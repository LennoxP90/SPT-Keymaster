using Keymaster.Shared;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace Keymaster.Server;

/// <summary>Adds the case to the Therapist assort. Price and loyalty level come from config/config.json.</summary>
// TraderRegistration + 30, not PostLoad: PostLoad runs after SPT has built the flea offers.
[Injectable(InjectionType.Singleton, TypePriority = KeymasterLoadOrder.Trader)]
public class KeymasterTraderAssort(ISptLogger<KeymasterTraderAssort> logger, KeymasterServerConfig config, TradersTable tradersTable) : IOnLoad
{
  readonly MongoId therapist = new(KeymasterIds.TherapistId);
  MongoId? assortItemId;

  public Task OnLoadAsync(CancellationToken cancellationToken)
  {
    if (!tradersTable.TryGetValue(therapist, out var trader) || trader.Assort is null)
    {
      logger.Error("Keymaster: Therapist or her assort not found, the case is not on offer");
      return Task.CompletedTask;
    }

    var id = new MongoId();
    trader.Assort.Items.Add(new Item
    {
      Id = id,
      Template = new MongoId(KeymasterIds.CaseTpl),
      ParentId = "hideout",
      SlotId = "hideout",
      Upd = new Upd { StackObjectsCount = 9999999, UnlimitedCount = true },
    });
    assortItemId = id;
    WriteTerms(trader, id);
    return Task.CompletedTask;
  }

  /// <summary>Rewrites the live offer from the current config; the flea copy follows on the next ragfair update.</summary>
  public bool ApplyConfig()
  {
    if (assortItemId is not { } id || !tradersTable.TryGetValue(therapist, out var trader) || trader.Assort is null) return false;
    WriteTerms(trader, id);
    if (trader.Base is not null) trader.Base.RefreshTraderRagfairOffers = true;
    logger.Debug($"Keymaster: trader settings re-applied: LL{config.Trader.LoyaltyLevel}, {config.Trader.PriceRoubles} RUB");
    return true;
  }

  void WriteTerms(Trader trader, MongoId id)
  {
    trader.Assort!.BarterScheme[id] = new List<List<BarterScheme>>
    {
      new() { new BarterScheme { Count = config.Trader.PriceRoubles, Template = ItemTpl.MONEY_ROUBLES } },
    };
    trader.Assort.LoyalLevelItems[id] = config.Trader.LoyaltyLevel;
  }
}
