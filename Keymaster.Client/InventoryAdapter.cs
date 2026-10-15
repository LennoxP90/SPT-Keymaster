using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diz.LanguageExtensions;
using EFT.InventoryLogic;
using Keymaster.Shared;

namespace Keymaster.Client
{
  /// <summary>Every item move Keymaster makes, on one inventory controller (the menu's). A refused move returns false, never throws.</summary>
  public sealed class InventoryAdapter
  {
    readonly InventoryController ic;
    InventoryAdapter(InventoryController ic) { this.ic = ic; }

    /// <summary>Null when there is no controller, or when the game's item reads cannot be trusted this session.</summary>
    public static InventoryAdapter For(InventoryController ic) => ic != null && Trusted ? new InventoryAdapter(ic) : null;

    static bool? trusted;
    /// <summary>Checked on first use, not at Awake: the game sets JsonConvert.DefaultSettings only at BeforeSceneLoad.</summary>
    static bool Trusted
    {
      get
      {
        if (trusted == null)
        {
          var warn = InventoryTrust.StandDown(Newtonsoft.Json.JsonConvert.DefaultSettings != null);
          trusted = warn == null;
          if (warn != null) Plugin.Warn(warn);
        }
        return trusted.Value;
      }
    }

    /// <summary>The case anywhere on the PMC, as the server's transit finds it.</summary>
    public CompoundItem Case => ic.Inventory.GetPlayerItems(EPlayerItems.Equipment).OfType<CompoundItem>()
      .FirstOrDefault(i => i.StringTemplateId == KeymasterIds.CaseTpl);

    /// <summary>On the PMC but not in the secure container.</summary>
    public static bool AtRisk(Inventory inv, Item item)
    {
      var sc = inv.Equipment.GetSlot(EquipmentSlot.SecuredContainer).ContainedItem;
      return item.IsChildOf(inv.Equipment) && (sc == null || !item.IsChildOf(sc));
    }

    public CompoundItem Vault => ic.Inventory.Stash?.GetAllItems().OfType<CompoundItem>().FirstOrDefault(i => i.StringTemplateId == KeymasterIds.VaultTpl);

    /// <summary>False while the menu inventory has no stash yet (right after a raid).</summary>
    public bool HasStash => ic.Inventory.Stash != null;

    Grid StashGrid => ic.Inventory.Stash?.Grids[0];

    /// <summary>Whether any of these templates is anywhere in the equipment (the case and secure container included).</summary>
    public bool Wears(IEnumerable<string> tpls)
    {
      var set = new HashSet<string>(tpls);
      return ic.Inventory.GetPlayerItems(EPlayerItems.Equipment).Any(i => set.Contains(i.StringTemplateId));
    }

    public static int UsesLeft(Item item)
    {
      var k = item.GetItemComponent<KeyComponent>();
      if (k == null || k.Template.MaximumNumberOfUsage <= 0) return -1;
      return k.Template.MaximumNumberOfUsage - k.NumberOfUsages;
    }

    /// <summary>By id in the case and the stash, the only places Keymaster moves from.</summary>
    Item Find(string itemId)
    {
      var item = (Case?.GetAllItems() ?? Enumerable.Empty<Item>()).Concat(ic.Inventory.Stash?.GetAllItems() ?? Enumerable.Empty<Item>())
        .FirstOrDefault(i => i.Id == itemId);
      if (item == null) Plugin.Detail("item " + itemId + " not found");
      return item;
    }

    static bool InACase(Item i) => i.Parent?.Container?.ParentItem?.StringTemplateId == KeymasterIds.CaseTpl;

    /// <summary>The containers from this item up to the stash, for LoadSource. Stops at the owner root as GetAllParentItems does.</summary>
    static IEnumerable<ContainerHop> Chain(Item i, Item stash)
    {
      for (var at = i.Parent; at != null && !(at is OwnerItself) && at.Container.ParentItem != stash; at = at.Container.ParentItem.CurrentAddress)
        yield return new ContainerHop { OwnerTpl = at.Container.ParentItem.StringTemplateId, IsSlot = at.Container is Slot };
    }

    /// <summary>
    /// Every key in the case, the vault and the rest of the stash, except keys in another mod's keychain slots.
    /// Equipped gear other than the case is never a source.
    /// </summary>
    public List<KeyCopy> Copies()
    {
      var list = new List<KeyCopy>();
      void Add(IEnumerable<Item> items, KeyHome where)
      {
        foreach (var i in items.ToList())
          if (i.GetItemComponent<KeyComponent>() != null)
            list.Add(new KeyCopy { ItemId = i.Id, Tpl = i.StringTemplateId, UsesLeft = UsesLeft(i), Where = where });
      }
      var c = Case; var v = Vault;
      if (c != null) Add(c.Grids[0].Items, KeyHome.Case);
      if (v != null) Add(v.Grids[0].Items, KeyHome.Vault);
      var skip = new HashSet<string>(list.Select(x => x.ItemId));
      if (ic.Inventory.Stash != null)
        Add(ic.Inventory.Stash.GetAllItems().Where(i => !skip.Contains(i.Id) && i.GetItemComponent<KeyComponent>() != null && !InACase(i)
          && LoadSource.IsLoadSource(Chain(i, ic.Inventory.Stash))), KeyHome.Stash);
      return list;
    }

    /// <summary>Keymaster's own loads (the swap, access cards) are never held back by the pickups cap.</summary>
    public async Task<bool> MoveToCase(string itemId)
    {
      UI.PickupCap.Loading.Add(itemId);
      try { return await MoveInto(itemId, Case?.Grids[0]); }
      finally { UI.PickupCap.Loading.Remove(itemId); }
    }

    /// <summary>Vault, then the main stash grid. False means it stays where it is.</summary>
    public async Task<bool> MoveHome(string itemId)
    {
      var v = Vault;
      if (v != null && await MoveInto(itemId, v.Grids[0])) return true;
      return await MoveInto(itemId, StashGrid);
    }

    Task<bool> MoveInto(string itemId, Grid grid)
    {
      var item = Find(itemId);
      if (grid == null) Plugin.Detail("no grid to move " + itemId + " into (stash " + (ic.Inventory.Stash == null ? "missing" : "present") + ")");
      if (grid == null || item == null) return Task.FromResult(false);
      var to = grid.FindLocationForItem(item);
      if (to == null) { Plugin.Detail($"no room for {item.TemplateId} in {grid.ParentItem.TemplateId}"); return Task.FromResult(false); }
      return Apply(ItemManipulator.Move(item, to, ic, true), item);
    }

    public List<StackRef> StashStacks(string currencyTpl) =>
      ic.Inventory.Stash.GetAllItems().Where(i => i.StringTemplateId == currencyTpl && !InACase(i))
        .Select(i => new StackRef { ItemId = i.Id, Count = i.StackObjectsCount }).ToList();

    public int InCell(int gridIndex) => Case?.Grids[gridIndex].Items.Sum(i => i.StackObjectsCount) ?? 0;

    /// <summary>The currency anywhere in the equipment (secure container included) outside the case's cell.</summary>
    public int CarriedOutsideCell(string currencyTpl, int gridIndex) =>
      ic.Inventory.GetPlayerItems(EPlayerItems.Equipment).Where(i => i.StringTemplateId == currencyTpl).Sum(i => i.StackObjectsCount) - InCell(gridIndex);

    /// <summary>Moves (or splits off) each planned amount into the currency cell. Returns the amount moved.</summary>
    public async Task<int> TopUp(MoneyPlan plan, int gridIndex)
    {
      var moved = 0;
      foreach (var m in plan.TopUp)
      {
        var cell = Case?.Grids[gridIndex];
        var src = Find(m.ItemId);
        if (m.Count <= 0 || cell == null || src == null) continue;
        if (await Put(src, m.Count, cell.Items.FirstOrDefault(), cell)) moved += m.Count;
      }
      return moved;
    }

    /// <summary>Moves the excess off the currency cell to the main stash grid, merging into a stack with room when there is one.</summary>
    public Task<bool> ReturnExcess(string currencyTpl, int amount, int gridIndex)
    {
      var stack = Case?.Grids[gridIndex].Items.FirstOrDefault();
      if (stack == null || amount <= 0 || StashGrid == null) return Task.FromResult(false);
      var onto = StashGrid.Items.FirstOrDefault(i => i.StringTemplateId == currencyTpl && i.StackObjectsCount + amount <= i.StackMaxSize);
      return Put(stack, amount, onto, StashGrid);
    }

    /// <summary>count of src onto an existing stack, or into free space in grid. Whole stacks merge or move; part stacks transfer or split.</summary>
    Task<bool> Put(Item src, int count, Item onto, Grid grid)
    {
      var whole = count >= src.StackObjectsCount;
      if (onto != null)
        return Apply(whole ? (OperationResult)ItemManipulator.Merge(src, onto, ic, true) : ItemManipulator.TransferExact(src, count, onto, ic, true), src);
      var to = grid.FindLocationForItem(src);
      if (to == null) { Plugin.Detail($"no room for {src.TemplateId} in {grid.ParentItem.TemplateId}"); return Task.FromResult(false); }
      return Apply(whole ? (OperationResult)ItemManipulator.Move(src, to, ic, true) : ItemManipulator.SplitExact(src, count, to, ic, ic, true), src);
    }

    async Task<bool> Apply(OperationResult op, Item item)
    {
      if (op.Failed) { Plugin.Detail($"move {item.TemplateId} refused: {op.Error}"); return false; }
      try
      {
        var done = await ic.TryRunNetworkTransaction(op);
        Plugin.Detail(done.Succeed ? $"moved {item.TemplateId}" : $"move {item.TemplateId} failed: {done.Error}");
        return done.Succeed;
      }
      catch (System.Exception ex) { Plugin.Detail($"move {item.TemplateId} threw: {ex.Message}"); return false; }
    }
  }
}
