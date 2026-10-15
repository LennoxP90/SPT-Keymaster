using System.Reflection;
using Keymaster.Shared;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Traders;
using SPTarkov.Server.Core.Helpers.Commerce;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Request;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Inventory;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Ragfair;
using SPTarkov.Server.Core.Models.Eft.Trade;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services.Ragfair;
using SPTarkov.Server.Core.Utils;

namespace Keymaster.Server;

/// <summary>Holds every profile to one KM Case and one KM Vault. Trader and flea buys pass TradeHelper.BuyItem; mail claims
/// (gift, insurance, Fika item sends) and scav-to-PMC transfers pass InventoryController.MoveItem. Both are refused per profile.
/// Flea listings of either item pass RagfairController.AddPlayerOffer and are always refused.</summary>
[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostLoad)]
public class KeymasterOneOfEach(ISptLogger<KeymasterOneOfEach> logger, HttpResponseUtil http, ProfileHelper profiles, TraderHelper traders, RagfairOfferService ragfair, EventOutputHolder outputs) : IOnLoad
{
  internal static KeymasterOneOfEach? Instance;

  public Task OnLoadAsync(CancellationToken cancellationToken)
  {
    Instance = this;
    new BuyGuard().Enable();
    new MoveGuard().Enable();
    new ListGuard().Enable();
    return Task.CompletedTask;
  }

  static IEnumerable<string> Tpls(IEnumerable<Item>? items) => (items ?? []).Select(i => i.Template.ToString());

  internal bool RefuseBuy(PmcData pmc, ProcessBuyTradeRequestData request, ItemEventRouterResponse output)
  {
    var tpl = BoughtTemplate(request);
    if (tpl is null || OneOfEach.MayBuy(Tpls(pmc.Inventory?.Items), tpl, request.Count ?? 1)) return false;
    Refuse(pmc, output, tpl, "purchase");
    return true;
  }

  internal bool RefuseMove(PmcData pmc, InventoryMoveRequestData request, MongoId sessionId, ItemEventRouterResponse output)
  {
    if (request.Item is not { } itemId || request.FromOwner is null) return false;
    var scavId = profiles.GetScavProfile(sessionId)?.Id;
    if (request.ToOwner?.Id is { } to && to == scavId) return false;
    var moved = WithChildren(SourceItems(request.FromOwner, sessionId, scavId), itemId).ToList();
    if (OneOfEach.MayReceive(Tpls(pmc.Inventory?.Items), moved)) return false;
    Refuse(pmc, output, moved.First(OneOfEach.IsLimited), "transfer");
    return true;
  }

  /// <summary>Read-only: InventoryHelper.GetOwnerInventoryItems marks a mail collected before the move is decided.</summary>
  IEnumerable<Item>? SourceItems(OwnerInfo from, MongoId sessionId, MongoId? scavId)
  {
    if (from.Id == scavId) return profiles.GetScavProfile(sessionId)?.Inventory?.Items;
    if (!string.Equals(from.Type, "mail", StringComparison.OrdinalIgnoreCase)) return null;
    return (profiles.GetFullProfile(sessionId)?.DialogueRecords?.Values.AsEnumerable() ?? [])
      .SelectMany(d => d.Messages ?? []).FirstOrDefault(m => m.Id == from.Id)?.Items?.Data;
  }

  internal bool RefuseListing(PmcData pmc, AddOfferRequestData request, MongoId sessionId, out ItemEventRouterResponse output)
  {
    output = null!;
    var listed = (request.Items ?? []).SelectMany(id => WithChildren(pmc.Inventory?.Items, id));
    if (OneOfEach.MayList(listed)) return false;
    output = http.AppendErrorToOutput(outputs.GetOutput(sessionId), "Keymaster items cannot be listed on the flea.");
    logger.Info($"Keymaster: flea listing of a Keymaster item refused for {pmc.Info?.Nickname}");
    return true;
  }

  string? BoughtTemplate(ProcessBuyTradeRequestData request)
  {
    var item = OneOfEach.UsesOfferLookup(request.Type)
      ? ragfair.GetOfferByOfferId(request.ItemId)?.Items?.FirstOrDefault()
      : traders.GetTraderAssortsByTraderId(request.TransactionId)?.Items?.FirstOrDefault(i => i.Id == request.ItemId);
    var tpl = item?.Template.ToString();
    return tpl is not null && OneOfEach.IsLimited(tpl) ? tpl : null;
  }

  static IEnumerable<string> WithChildren(IEnumerable<Item>? items, MongoId root)
  {
    var list = items?.ToList() ?? [];
    var ids = new HashSet<MongoId> { root };
    // rescans until no new child, fine for a mail or a scav backpack.
    while (true)
    {
      var added = list.Where(i => i.ParentId is { } p && ids.Contains(new MongoId(p)) && ids.Add(i.Id)).Count();
      if (added == 0) break;
    }
    return list.Where(i => ids.Contains(i.Id)).Select(i => i.Template.ToString());
  }

  void Refuse(PmcData pmc, ItemEventRouterResponse output, string tpl, string what)
  {
    var name = tpl == KeymasterIds.CaseTpl ? "KM Case" : "KM Vault";
    http.AppendErrorToOutput(output, $"You can only hold one {name}.");
    logger.Info($"Keymaster: {what} of a second {name} refused for {pmc.Info?.Nickname}");
  }
}

public class BuyGuard : AbstractPatch
{
  protected override MethodBase GetTargetMethod() => typeof(TradeHelper).GetMethod(nameof(TradeHelper.BuyItem))!;

  [PatchPrefix]
  public static bool Prefix(PmcData pmcData, ProcessBuyTradeRequestData buyRequestData, ItemEventRouterResponse output) =>
    KeymasterOneOfEach.Instance?.RefuseBuy(pmcData, buyRequestData, output) != true;
}

public class MoveGuard : AbstractPatch
{
  protected override MethodBase GetTargetMethod() => typeof(InventoryController).GetMethod(nameof(InventoryController.MoveItem))!;

  [PatchPrefix]
  public static bool Prefix(PmcData pmcData, InventoryMoveRequestData moveRequest, MongoId sessionId, ItemEventRouterResponse output) =>
    KeymasterOneOfEach.Instance?.RefuseMove(pmcData, moveRequest, sessionId, output) != true;
}

public class ListGuard : AbstractPatch
{
  protected override MethodBase GetTargetMethod() => typeof(RagfairController).GetMethod(nameof(RagfairController.AddPlayerOffer))!;

  [PatchPrefix]
  public static bool Prefix(PmcData pmcData, AddOfferRequestData offerRequest, MongoId sessionID, ref ItemEventRouterResponse __result) =>
    KeymasterOneOfEach.Instance?.RefuseListing(pmcData, offerRequest, sessionID, out __result) != true;
}
