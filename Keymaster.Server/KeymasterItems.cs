using Keymaster.Shared;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace Keymaster.Server;

[Injectable(TypePriority = KeymasterLoadOrder.Items)]
public class KeymasterItems(ISptLogger<KeymasterItems> logger, CustomItemService customItems) : IOnLoad
{
  const string HandbookContainers = "5b5f6fa186f77409407a7eb7";
  const string SimpleContainerClass = "5795f317245977243854e041";
  const string GridPrototype = "55d329c24bdc2d892f8b4567";

  public Task OnLoadAsync(CancellationToken cancellationToken)
  {
    var ok = Create(KeymasterIds.CaseTpl, KeymasterIds.SiccTpl, "keymaster_case", "Keymaster's Case", "KM Case",
                 "Carries the keys for the map you deploy to, plus your raid money. Goes in the secure container.",
                 2, 1, CaseGrids())
          & Create(KeymasterIds.VaultTpl, KeymasterIds.KeyToolTpl, "keymaster_vault", "Keymaster's Vault", "KM Vault",
                 "Stores your whole key collection. Stays in the stash.",
                 1, 1, new List<Grid> { KeyGrid(KeymasterIds.VaultTpl, "keys") });
    if (!ok) logger.Error("Keymaster: item templates missing, the case and vault will not work");
    return Task.CompletedTask;
  }

  bool Create(string id, string donor, string internalName, string name, string shortName, string description, int w, int h, List<Grid> grids)
  {
    var result = customItems.CreateItemFromClone(new NewItemFromCloneDetails
    {
      ItemTplToClone = new MongoId(donor),
      NewId = new MongoId(id),
      ParentId = new MongoId(SimpleContainerClass),
      NewItemName = internalName,
      HandbookParentId = HandbookContainers,
      HandbookPriceRoubles = 2000000,
      FleaPriceRoubles = 2000000,
      AddToHandbook = true,
      AddToFleaPriceDb = true,
      AddToWeaponShelf = false,
      OverrideProperties = new TemplateItemProperties
      {
        Name = name, ShortName = shortName, Description = description,
        Width = w, Height = h, BackgroundColor = "yellow",
        CanSellOnRagfair = false,
        Grids = grids,
      },
      Locales = new Dictionary<string, LocaleDetails> { ["en"] = new LocaleDetails { Name = name, ShortName = shortName, Description = description } },
    });
    if (result.Success != true)
      logger.Error($"Keymaster: {shortName} not created: {string.Join("; ", result.Errors ?? [])}");
    return result.Success == true;
  }

  static List<Grid> CaseGrids()
  {
    var grids = new List<Grid> { KeyGrid(KeymasterIds.CaseTpl, "keys") };
    for (var i = 0; i < KeymasterIds.Currencies.Length; i++)
      grids.Add(GridOf(KeymasterIds.CaseTpl, "money" + i, 1, 1, new HashSet<MongoId> { new(KeymasterIds.Currencies[i]) }));
    return grids;
  }

  static Grid KeyGrid(string parent, string name) =>
    GridOf(parent, name, KeymasterIds.KeyGridWidth, KeymasterIds.KeyGridHeight, new HashSet<MongoId> { new(KeymasterIds.KeyCategory) });

  static Grid GridOf(string parent, string name, int w, int h, HashSet<MongoId> filter) => new Grid
  {
    Name = name,
    Id = new MongoId().ToString(),
    Parent = parent,
    Prototype = GridPrototype,
    Properties = new GridProperties
    {
      CellsH = w, CellsV = h, MinCount = 0, MaxCount = 0, MaxWeight = 0, IsSortingTable = false,
      Filters = new List<GridFilter> { new GridFilter { Filter = filter, ExcludedFilter = new HashSet<MongoId>() } },
    },
  };
}
