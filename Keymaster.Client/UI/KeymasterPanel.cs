using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diz.LanguageExtensions;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using Keymaster.Shared;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Keymaster.Client.UI
{
  /// <summary>
  /// The case and vault content inside the game's container window: EFT's own item views in virtualised rows.
  /// Case: KEYS (current map and always-bring), PICKUPS (10 slots), MONEY (one cell per currency); keys merge into one
  /// view per template. Vault: a header per home map.
  /// </summary>
  public sealed partial class KeymasterPanel : MonoBehaviour, IContainerView, IAddHandler, IRemoveHandler, IRefreshHandler, IScrollHandler
  {
    const int Cell = 63, Cols = 10;
    static readonly string[] CurrencyCodes = { "RUB", "USD", "EUR", "GP" };

    sealed class Row
    {
      public float Y, H;
      public string Header;
      /// <summary>One per cell; null is an empty cell.</summary>
      public List<Item> Items;
      /// <summary>The MONEY row: cell c is KeymasterIds.Currencies[c].</summary>
      public bool Money;
    }

    CompoundItem _item;
    ItemUiContext _ui;
    ItemController _ic;
    IItemOwner _owner;
    ItemContext _source;
    bool _dirty;
    int _first;
    List<Row> _rows = new List<Row>();
    readonly Dictionary<int, List<Component>> _shown = new Dictionary<int, List<Component>>();

    public bool IsCase => _item.StringTemplateId == KeymasterIds.CaseTpl;

    public static bool Hosts(Item item) => item != null && (item.StringTemplateId == KeymasterIds.CaseTpl || item.StringTemplateId == KeymasterIds.VaultTpl);

    public List<Item> CopiesOf(string tpl) => _item.Grids[0].Items.Where(i => i.StringTemplateId == tpl).ToList();

    public static void Attach(GridWindow window, CompoundItem item, ItemContext ctx, ItemController ic, ItemUiContext ui)
    {
      var panel = new GameObject("KeymasterPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image)).AddComponent<KeymasterPanel>();
      try { panel.Init(window, item, ctx, ui, ic); }
      catch (Exception ex)
      {
        Plugin.Warn("panel failed to build: " + ex.Message);
        Destroy(panel.gameObject);
        window.Close();
      }
    }

    void Init(GridWindow window, CompoundItem item, ItemContext ctx, ItemUiContext ui, ItemController ic)
    {
      _item = item;
      _ui = ui;
      _ic = ic;
      _source = ctx.CreateChild(item);
      Build(window);
      _owner = item.Owner;
      _owner?.RegisterView(this);
      Rebuild();
    }

    void LateUpdate()
    {
      if (_dirty) Rebuild();
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
      _first = Mathf.Min(_first, LastFirst());
      ShowVisible();
    }

    static KeyCopy Copy(Item i) => new KeyCopy { ItemId = i.Id, Tpl = i.StringTemplateId, UsesLeft = InventoryAdapter.UsesLeft(i) };

    List<Row> CaseRows()
    {
      var wanted = KeymasterApi.WantedKeys();
      var keys = _item.Grids[0].Items.ToList();
      var sections = CaseLayout.Split(keys.Select(Copy).ToList(), wanted);
      int Rank(Item i) { var n = wanted.IndexOf(i.StringTemplateId); return n < 0 ? int.MaxValue : n; }

      var rows = new List<Row> { new Row { Header = "Keys" } };
      rows.AddRange(Chunk(Merged(sections.Keys, keys).OrderBy(Rank).ToList()));
      rows.Add(new Row { Header = "Pickups" });
      var pickups = Merged(sections.Pickups, keys).ToList();
      while (pickups.Count < CaseLayout.PickupSlots || pickups.Count % Cols != 0) pickups.Add(null);
      rows.AddRange(Chunk(pickups));
      rows.Add(new Row { Header = "Money" });
      rows.Add(new Row { Money = true, Items = Enumerable.Range(1, KeymasterIds.Currencies.Length).Select(g => _item.Grids[g].Items.FirstOrDefault()).ToList() });
      return rows;
    }

    /// <summary>One item per template, the copy a drag should take, in first-seen order.</summary>
    static IEnumerable<Item> Merged(IEnumerable<KeyCopy> copies, List<Item> items) =>
      copies.GroupBy(c => c.Tpl).Select(g => items.First(i => i.Id == LoadPlanner.Pick(g, KeymasterSettings.Copy.Value).ItemId));

    List<Row> VaultRows()
    {
      var items = _item.Grids[0].Items.ToList();
      var byTpl = items.ToLookup(i => i.StringTemplateId);
      var rows = new List<Row>();
      foreach (var g in MapTitle.Named(VaultGrouping.Group(items.Select(i => i.StringTemplateId), KeymasterApi.Table, KeymasterApi.Homes), MapName))
      {
        rows.Add(new Row { Header = g.Title });
        rows.AddRange(Chunk(g.Tpls.SelectMany(t => byTpl[t]).ToList()));
      }
      return rows;
    }

    static List<Row> Chunk(List<Item> items) =>
      Enumerable.Range(0, (items.Count + Cols - 1) / Cols).Select(n => new Row { Items = items.Skip(n * Cols).Take(Cols).ToList() }).ToList();

    /// <summary>The highest first row that still fills the viewport.</summary>
    int LastFirst()
    {
      var total = _rows.Count == 0 ? 0 : _rows[_rows.Count - 1].Y + _rows[_rows.Count - 1].H;
      var i = 0;
      while (i < _rows.Count && total - _rows[i].Y > _viewH) i++;
      return i;
    }

    /// <summary>Scrolls a whole row per notch, so only whole rows are ever drawn and nothing needs clipping.</summary>
    public void OnScroll(PointerEventData e)
    {
      var next = Mathf.Clamp(_first - Math.Sign(e.scrollDelta.y), 0, LastFirst());
      if (next == _first) return;
      _first = next;
      ShowVisible();
    }

    void ShowVisible()
    {
      var top = _rows.Count == 0 ? 0 : _rows[_first].Y;
      ScrollTo(top);
      foreach (var i in _shown.Keys.Where(i => !Visible(_rows[i], top)).ToList()) Hide(i);
      for (var i = _first; i < _rows.Count && Visible(_rows[i], top); i++)
        if (!_shown.ContainsKey(i)) Show(i);
    }

    bool Visible(Row r, float top) => r.Y >= top && r.Y + r.H - top <= _viewH + 0.5f;

    void Show(int index)
    {
      var r = _rows[index];
      var shown = new List<Component>();
      if (r.Header != null) shown.Add(Header(r.Header, r.Y));
      else
      {
        shown.Add(Cells(r.Items.Count, r.Y));
        for (var c = 0; c < r.Items.Count; c++)
          if (r.Items[c] != null) shown.Add(View(r.Items[c], c, r.Y));
          else if (r.Money) shown.Add(EmptyMoney(KeymasterIds.Currencies[c], CurrencyCodes[c], c, r.Y));
      }
      _shown[index] = shown;
    }

    ItemView View(Item item, int col, float y)
    {
      var v = _ui.CreateItemView(item, _source, ItemRotation.Horizontal, _ic, null, null, this, false, true);
      PlaceView(v, col, y);
      return v;
    }

    /// <summary>A pooled view may already belong to another container; only views this panel still owns are killed.</summary>
    void Hide(int index)
    {
      foreach (var c in _shown[index])
        if (c is ItemView v) { if (v != null && ReferenceEquals(v.Container, this)) v.Kill(); }
        else if (c != null) Destroy(c.gameObject);
      _shown.Remove(index);
    }

    void OnDestroy()
    {
      if (_source == null) return;
      _owner?.UnregisterView(this);
      _owner = null;
      foreach (var i in _shown.Keys.ToList()) Hide(i);
      _source.Dispose();
      _source = null;
    }

    void Touch(CommandStatus status, ItemAddress at)
    {
      if (status != CommandStatus.Begin && at?.Container?.ParentItem == _item) _dirty = true;
    }

    void IAddHandler.OnItemAdded(AddItemEventArgs e) => Touch(e.Status, e.To);
    void IRemoveHandler.OnItemRemoved(RemoveItemEventArgs e) => Touch(e.Status, e.From);
    void IRefreshHandler.OnRefreshItem(RefreshItemEventArgs e) { if (e.Container?.ParentItem == _item) _dirty = true; }

    public bool CanDrag(ItemContext itemContext) => itemContext.DragAvailable && itemContext.Item.CheckAction(null).Succeeded;

    /// <summary>
    /// A drop anywhere goes to the next free cell of the hidden grid. Money in the case goes onto its currency cell's
    /// stack, merging or topping it up, never swapping a full stack out. A new pickup is refused once PICKUPS is full.
    /// Items already inside are refused.
    /// </summary>
    public bool CanAccept(DragItemContext itemContext, ItemContext targetItemContext, out OperationResult operation)
    {
      operation = default;
      var item = itemContext.Item;
      if (item.Parent?.Container?.ParentItem == _item) return false;
      var grid = GridFor(item);
      if (grid == _item.Grids[0])
      {
        var to = grid.FindLocationForItem(item);
        if (to == null) return false;
        operation = _ic.ExecutePossibleAction(itemContext, to, false, true);
        return operation.Succeeded;
      }
      var onto = grid.Items.FirstOrDefault();
      if (onto == null)
      {
        var to = grid.FindLocationForItem(item);
        if (to == null) return false;
        operation = _ic.ExecutePossibleAction(itemContext, to, false, true);
        return operation.Succeeded;
      }
      var room = onto.StackMaxSize - onto.StackObjectsCount;
      if (room <= 0) return false;
      operation = item.StackObjectsCount <= room
        ? (OperationResult)ItemManipulator.Merge(item, onto, _ic, true)
        : ItemManipulator.TransferExact(item, room, onto, _ic, true);
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
