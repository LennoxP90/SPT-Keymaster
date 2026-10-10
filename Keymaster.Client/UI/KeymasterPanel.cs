using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diz.LanguageExtensions;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using Keymaster.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace Keymaster.Client.UI
{
  /// <summary>
  /// The case and vault window: only what the item holds, as EFT's own item views in virtualised rows.
  /// Case keys merge into one view per template; vault keys sit under a header per map.
  /// </summary>
  public sealed partial class KeymasterPanel : MonoBehaviour, IContainerView, IAddHandler, IRemoveHandler, IRefreshHandler
  {
    const int Cell = 63, Cols = 10;
    static KeymasterPanel _open;
    static readonly AccessTools.FieldRef<ItemUiContext, ItemController> UiController = AccessTools.FieldRefAccess<ItemUiContext, ItemController>("_itemController");

    sealed class Row
    {
      public float Y, H;
      public string Header;
      public List<Item> Items;
    }

    CompoundItem _item;
    ItemUiContext _ui;
    ItemController _ic;
    IItemOwner _owner;
    ItemContext _source;
    bool _dirty, _closing;
    Item[] _parents;
    List<Row> _rows = new List<Row>();
    readonly Dictionary<int, List<Component>> _shown = new Dictionary<int, List<Component>>();

    public bool IsCase => _item.StringTemplateId == KeymasterIds.CaseTpl;

    public List<Item> CopiesOf(string tpl) => _item.Grids[0].Items.Where(i => i.StringTemplateId == tpl).ToList();

    /// <summary>False when there is no inventory canvas to draw on; the stock window opens instead.</summary>
    public static bool Open(CompoundItem item, ItemContext ctx, ItemUiContext ui)
    {
      Close();
      var ic = UiController(ui);
      var screen = ScreenCanvas(ui, ic);
      if (screen == null) { Plugin.Warn("no inventory canvas for the panel; stock window used"); return false; }
      _open = new GameObject("KeymasterPanel", typeof(RectTransform), typeof(Image)).AddComponent<KeymasterPanel>();
      _open.Init(item, ctx, ui, ic, screen);
      return true;
    }

    public static void Close()
    {
      if (_open == null) return;
      _open.Release();
      Destroy(_open.gameObject);
      _open = null;
    }

    void Init(CompoundItem item, ItemContext ctx, ItemUiContext ui, ItemController ic, Canvas screen)
    {
      _item = item;
      _ui = ui;
      _ic = ic;
      _source = ctx.CreateChild(item);
      _source.OnCloseDependentWindow += Close;
      _parents = item.GetAllParentItemsAndSelf().ToArray();
      Build(screen);
      _owner = item.Owner;
      _owner?.RegisterView(this);
      Rebuild();
    }

    void LateUpdate()
    {
      if (_closing) Close();
      else if (_dirty) Rebuild();
    }

    void Rebuild()
    {
      _dirty = false;
      foreach (var i in _shown.Keys.ToList()) Hide(i);
      _rows = IsCase ? CaseRows() : VaultRows();
      float y = 0;
      foreach (var r in _rows)
      {
        r.Y = y;
        r.H = r.Header != null ? HeaderHeight : Cell;
        y += r.H;
      }
      Resize(y);
      ShowVisible();
    }

    /// <summary>Keys in the current map's table order, then the rest; one view per template, holding the copy a drag should take. Money last.</summary>
    List<Row> CaseRows()
    {
      var order = KeymasterApi.CurrentMapKeys().ToList();
      int Rank(string tpl) { var i = order.IndexOf(tpl); return i < 0 ? int.MaxValue : i; }
      var rows = Chunk(_item.Grids[0].Items.GroupBy(i => i.StringTemplateId).OrderBy(g => Rank(g.Key)).Select(Chosen));
      var money = _item.Grids.Skip(1).SelectMany(g => g.Items).ToList();
      if (money.Count > 0) rows.Add(new Row { Items = money });
      return rows;
    }

    static Item Chosen(IEnumerable<Item> copies)
    {
      var list = copies.ToList();
      var keyCopies = list.Select(i => new KeyCopy { ItemId = i.Id, Tpl = i.StringTemplateId, UsesLeft = InventoryAdapter.UsesLeft(i) });
      var id = LoadPlanner.Pick(keyCopies, KeymasterSettings.Copy.Value).ItemId;
      return list.First(i => i.Id == id);
    }

    List<Row> VaultRows()
    {
      var items = _item.Grids[0].Items.ToList();
      var byTpl = items.ToLookup(i => i.StringTemplateId);
      var rows = new List<Row>();
      foreach (var g in VaultGrouping.Group(items.Select(i => i.StringTemplateId), KeymasterApi.Table, KeymasterApi.Homes))
      {
        rows.Add(new Row { Header = g.Title });
        rows.AddRange(Chunk(g.Tpls.SelectMany(t => byTpl[t])));
      }
      return rows;
    }

    static List<Row> Chunk(IEnumerable<Item> items) =>
      items.Select((item, n) => (item, n)).GroupBy(x => x.n / Cols).Select(g => new Row { Items = g.Select(x => x.item).ToList() }).ToList();

    /// <summary>Only rows in the viewport plus one either side have views.</summary>
    void ShowVisible()
    {
      var top = _content.anchoredPosition.y;
      var bottom = top + _viewH;
      var first = Mathf.Max(0, _rows.FindIndex(r => r.Y + r.H > top) - 1);
      var last = Mathf.Min(_rows.Count - 1, _rows.FindLastIndex(r => r.Y < bottom) + 1);
      foreach (var i in _shown.Keys.Where(i => i < first || i > last).ToList()) Hide(i);
      for (var i = first; i <= last; i++)
        if (!_shown.ContainsKey(i)) Show(i);
    }

    void Show(int index)
    {
      var r = _rows[index];
      var shown = new List<Component>();
      if (r.Header != null) shown.Add(Header(r.Header, r.Y));
      else
        for (var c = 0; c < r.Items.Count; c++) shown.Add(View(r.Items[c], c, r.Y));
      _shown[index] = shown;
    }

    ItemView View(Item item, int col, float y)
    {
      var v = _ui.CreateItemView(item, _source, ItemRotation.Horizontal, _ic, null, null, this, false, true);
      PlaceView(v, col, y);
      return v;
    }

    /// <summary>ItemView.IsKilled is not reachable, so a view counts as live while it still has an item.</summary>
    void Hide(int index)
    {
      foreach (var c in _shown[index])
        if (c is ItemView v) { if (v != null && v.Item != null) v.Kill(); }
        else if (c != null) Destroy(c.gameObject);
      _shown.Remove(index);
    }

    void Release()
    {
      if (_source == null) return;
      _owner?.UnregisterView(this);
      _owner = null;
      foreach (var i in _shown.Keys.ToList()) Hide(i);
      _source.OnCloseDependentWindow -= Close;
      _source.Dispose();
      _source = null;
    }

    void OnDestroy()
    {
      Release();
      if (_open == this) _open = null;
    }

    void Touch(CommandStatus status, ItemAddress at)
    {
      if (status != CommandStatus.Begin && at?.Container?.ParentItem == _item) _dirty = true;
    }

    void IAddHandler.OnItemAdded(AddItemEventArgs e) => Touch(e.Status, e.To);
    /// <summary>As GridWindow does: the item or one of its parents moved away closes the panel, after the owner finishes delivering events.</summary>
    void IRemoveHandler.OnItemRemoved(RemoveItemEventArgs e)
    {
      if (e.Status == CommandStatus.Succeed && _parents.Contains(e.Item)) _closing = true;
      else Touch(e.Status, e.From);
    }
    void IRefreshHandler.OnRefreshItem(RefreshItemEventArgs e) { if (e.Container?.ParentItem == _item) _dirty = true; }

    public bool CanDrag(ItemContext itemContext) => itemContext.DragAvailable && itemContext.Item.CheckAction(null).Succeeded;

    /// <summary>
    /// A drop anywhere goes to the next free cell of the hidden grid; money in the case goes onto its currency cell's
    /// stack (the item-target overload, so it merges). Items already inside are refused.
    /// </summary>
    public bool CanAccept(DragItemContext itemContext, ItemContext targetItemContext, out OperationResult operation)
    {
      operation = default;
      var item = itemContext.Item;
      if (item.Parent?.Container?.ParentItem == _item) return false;
      var grid = GridFor(item);
      var onto = grid == _item.Grids[0] ? null : grid.Items.FirstOrDefault();
      var to = onto == null ? grid.FindLocationForItem(item) : null;
      if (onto == null && to == null) return false;
      operation = onto != null
        ? _ic.ExecutePossibleAction(itemContext, onto, false, true)
        : _ic.ExecutePossibleAction(itemContext, to, false, true);
      return operation.Succeeded;
    }

    Grid GridFor(Item item)
    {
      var cur = Array.IndexOf(KeymasterIds.Currencies, item.StringTemplateId);
      return IsCase && cur >= 0 ? _item.Grids[cur + 1] : _item.Grids[0];
    }

    public async Task AcceptItem(DragItemContext itemContext, ItemContext targetItemContext)
    {
      if (!CanAccept(itemContext, targetItemContext, out var op)) return;
      var result = await _ic.TryRunNetworkTransaction(op);
      if (!result.Succeed) Plugin.Detail("panel drop refused: " + result.Error);
      itemContext.DragCancelled();
    }

    public void HighlightItemViewPosition(DragItemContext itemContext, ItemContext targetItemContext, bool preview) { }
    public void DisableHighlight() { }
    public void DragStarted() { }
    public void DragCancelled() { }
  }
}
