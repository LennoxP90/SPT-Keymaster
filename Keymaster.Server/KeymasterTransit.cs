using System.Reflection;
using Keymaster.Shared;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.InRaid;

namespace Keymaster.Server;

/// <summary>
/// The case swap for a map-to-map transit. The client has no stash between two maps, so the server moves the keys once it
/// has written the raid inventory, and the client reads the result when it reloads the profile for the next map. Keys only:
/// money stays as it is. Only existing items move, checked all-or-nothing by GridMoves before anything changes.
/// </summary>
[Injectable(InjectionType.Singleton)]
public class KeymasterTransit(ISptLogger<KeymasterTransit> logger, KeyTableService table, KeymasterStore store, ProfileHelper profiles,
  SaveServer saves, KmTemplateTable templates)
{
  static readonly MongoId Case = new(KeymasterIds.CaseTpl), Vault = new(KeymasterIds.VaultTpl);

  public async Task Swap(MongoId sessionId, string next)
  {
    var line = Plan(sessionId, next);
    if (line == null) return;
    store.UpdateState(sessionId, s => { s.CurrentMap = KeyTable.EntryKey(next, null); return true; });
    await saves.SaveProfileAsync(sessionId);
    logger.Info("Keymaster: " + line);
  }

  /// <summary>Applies the swap to the PMC profile in memory. Null when there is nothing to do or it was refused.</summary>
  string? Plan(MongoId sessionId, string next)
  {
    var pmc = profiles.GetPmcProfile(sessionId);
    var items = pmc?.Inventory?.Items;
    if (items == null || pmc!.Inventory!.Equipment is not { } equipment || pmc.Inventory.Stash is not { } stash) return null;
    var cases = items.Where(i => i.Template == Case && Under(i, equipment, items)).ToList();
    var vaults = items.Where(i => i.Template == Vault && Under(i, stash, items)).ToList();
    if (cases.Count != 1 || vaults.Count != 1) return null;

    var caseGrid = Grid(cases[0]); var vaultGrid = Grid(vaults[0]);
    if (caseGrid == null || vaultGrid == null) return Refuse(next, "no key grid on the case or vault template");
    bool In(Item i, GridRef g) => i.ParentId == g.ParentId && i.SlotId == g.SlotId;
    var inGrids = items.Where(i => In(i, caseGrid) || In(i, vaultGrid)).ToList();
    var placed = new List<GridItem>();
    foreach (var i in inGrids)
    {
      var at = i.GetParsedLocation();
      var props = templates.Items.GetValueOrDefault(i.Template)?.Properties;
      if (at?.X == null || at.Y == null || props?.Width != 1 || props.Height != 1) return Refuse(next, $"{i.Template} in a key grid is not a placed 1x1 item");
      placed.Add(new GridItem { Id = i.Id.ToString(), ParentId = i.ParentId!, SlotId = i.SlotId!, X = at.X.Value, Y = at.Y.Value });
    }

    var copies = inGrids.Select(i => new KeyCopy
    {
      ItemId = i.Id.ToString(), Tpl = i.Template.ToString(), UsesLeft = UsesLeft(i), Where = In(i, caseGrid) ? KeyHome.Case : KeyHome.Vault,
    }).ToList();
    // The table entry already holds the map's own access keys and its transit cards (KeyTable.Build).
    var wanted = KeyTable.Resolve(table.Table, next, null).Concat(store.Get(sessionId).AlwaysBring ?? []).Distinct().ToList();
    // Freshest copy always: the client's Copy setting is not sent to the server.
    var plan = LoadPlanner.Plan(wanted, copies, CopyChoice.Freshest, mapChanged: true);
    var moves = GridMoves.Place(placed, plan.ToHome.Select(id => (id, vaultGrid)).Concat(plan.ToCase.Select(id => (id, caseGrid))));
    var problem = GridMoves.Problem(placed, moves);
    if (problem != null) return Refuse(next, problem);

    var byId = inGrids.ToDictionary(i => i.Id.ToString());
    foreach (var m in moves)
    {
      var item = byId[m.Id];
      item.ParentId = m.To.ParentId;
      item.SlotId = m.To.SlotId;
      item.Location = (item.GetParsedLocation() ?? new ItemLocation()) with { X = m.X, Y = m.Y, R = ItemRotation.Horizontal };
    }
    var loaded = moves.Count(m => m.To == caseGrid);
    var skipped = plan.ToHome.Count + plan.ToCase.Count - moves.Count;
    return $"transit to {next}: {plan.Present + loaded}/{plan.Wanted} keys, {moves.Count - loaded} home" + (skipped > 0 ? $", {skipped} left (no room)" : "");
  }

  string? Refuse(string next, string why)
  {
    logger.Warning($"Keymaster: transit to {next}: case left as it is: {why}");
    return null;
  }

  GridRef? Grid(Item container)
  {
    var g = templates.Items.GetValueOrDefault(container.Template)?.Properties?.Grids?.FirstOrDefault();
    return g?.Name == null || g.Properties?.CellsH is not { } w || g.Properties.CellsV is not { } h ? null
      : new GridRef { ParentId = container.Id.ToString(), SlotId = g.Name, Width = w, Height = h };
  }

  int UsesLeft(Item i)
  {
    var max = templates.Items.GetValueOrDefault(i.Template)?.Properties?.MaximumNumberOfUsage ?? 0;
    return max <= 0 ? -1 : max - (i.Upd?.Key?.NumberOfUsages ?? 0);
  }

  static bool Under(Item item, MongoId root, List<Item> items)
  {
    var byId = items.ToDictionary(i => i.Id.ToString());
    for (var at = item.ParentId; at != null; at = byId.GetValueOrDefault(at)?.ParentId)
      if (at == root.ToString()) return true;
    return false;
  }
}

/// <summary>Runs the transit swap after SPT's raid end, for a player whose end actually ran (Fika skips spectators).</summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad)]
public class KeymasterTransitPatch : AbstractPatch, IOnLoad
{
  static KeymasterTransit transit = default!;
  public KeymasterTransitPatch(KeymasterTransit t) { transit = t; }

  public Task OnLoadAsync(CancellationToken cancellationToken)
  {
    Enable();
    return Task.CompletedTask;
  }

  protected override MethodBase GetTargetMethod() => typeof(LocationLifecycleService).GetMethod(nameof(LocationLifecycleService.EndLocalRaidAsync))!;

  [PatchPostfix]
  public static void Postfix(MongoId sessionId, EndLocalRaidRequestData request, ref Task __result, bool __runOriginal)
  {
    if (!__runOriginal || request.Results?.IsMapToMapTransfer() != true || request.LocationTransit?.Location is not { } next) return;
    if (request.ServerId?.Split('.').ElementAtOrDefault(1)?.Contains("pmc", StringComparison.OrdinalIgnoreCase) != true) return;
    __result = After(__result, sessionId, next);
  }

  static async Task After(Task ended, MongoId sessionId, string next)
  {
    await ended;
    try { await transit.Swap(sessionId, next); }
    catch (Exception ex) { Console.WriteLine("Keymaster: transit swap failed, case left as it is: " + ex.Message); }
  }
}
