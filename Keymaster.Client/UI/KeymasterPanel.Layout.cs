using Diz.LanguageExtensions;
using System;
using System.Linq;
using EFT;
using EFT.UI;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Keymaster.Shared;

namespace Keymaster.Client.UI
{
  /// <summary>Everything the panel draws besides EFT's item views, in the host GridWindow's own styles. Logic lives in KeymasterPanel.cs.</summary>
  public sealed partial class KeymasterPanel
  {
    const int HeaderHeight = 28, MaxView = 8 * Cell;
    static readonly AccessTools.FieldRef<GridWindow, ContainedGridsView> WindowGrids = AccessTools.FieldRefAccess<GridWindow, ContainedGridsView>("_containedGrids");
    static readonly Color Dim = new Color(1f, 1f, 1f, 0.35f);

    RectTransform _root, _content;
    TextMeshProUGUI _style;
    Image _cellStyle, _highlight;
    GridView _gridTemplate;
    Scrollbar _bar;
    float _viewH, _barW;

    /// <summary>Takes the place of the window's (never shown) grid view; caption, close button, frame and dragging stay the window's.</summary>
    void Build(GridWindow window)
    {
      var grids = WindowGrids(window);
      _style = window.Caption;
      _gridTemplate = (grids as GeneratedGridsView)?._gridViewTemplate;
      _cellStyle = _gridTemplate?.transform.Find("Background Tile")?.GetComponent<Image>();
      if (window._sortPanel != null) window._sortPanel.gameObject.SetActive(false);

      _root = (RectTransform)transform;
      var slot = (RectTransform)grids.transform;
      _root.SetParent(slot.parent, false);
      _root.SetSiblingIndex(slot.GetSiblingIndex() + 1);
      _root.anchorMin = slot.anchorMin;
      _root.anchorMax = slot.anchorMax;
      _root.pivot = slot.pivot;
      _root.anchoredPosition = slot.anchoredPosition;
      grids.gameObject.SetActive(false);
      GetComponent<Image>().color = Color.clear;
      gameObject.AddComponent<LayoutElement>();

      _content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
      _content.SetParent(_root, false);
      _content.anchorMin = _content.anchorMax = _content.pivot = new Vector2(0, 1);
      _bar = Bar(window);
    }

    static bool Vertical(Scrollbar s) => s.direction == Scrollbar.Direction.BottomToTop || s.direction == Scrollbar.Direction.TopToBottom;

    /// <summary>A copy of the game's own vertical scrollbar (the window's, else any loaded one), driven by the panel's rows.</summary>
    Scrollbar Bar(GridWindow window)
    {
      var template = window.GetComponentsInChildren<Scrollbar>(true).FirstOrDefault(Vertical)
        ?? Resources.FindObjectsOfTypeAll<Scrollbar>().Where(Vertical).OrderByDescending(s => s.gameObject.scene.IsValid()).FirstOrDefault();
      if (template == null) { Plugin.Detail("panel: no stock scrollbar found, wheel only"); return null; }
      var bar = Instantiate(template.gameObject, _root, false).GetComponent<Scrollbar>();
      bar.name = "Scrollbar";
      // The window's own bar is hidden while its grid fits; the copy carries that state, so show every part of it.
      foreach (var part in bar.GetComponentsInChildren<Transform>(true)) if (part != bar.transform) part.gameObject.SetActive(true);
      foreach (var g in bar.GetComponentsInChildren<Graphic>(true)) { g.enabled = true; g.canvasRenderer.SetAlpha(1f); }
      foreach (var cg in bar.GetComponentsInChildren<CanvasGroup>(true)) { cg.alpha = 1f; cg.interactable = cg.blocksRaycasts = true; }
      Plugin.Detail("panel: scroll bar copied from " + template.transform.parent?.name + "/" + template.name);
      bar.onValueChanged = new Scrollbar.ScrollEvent();
      bar.onValueChanged.AddListener(v => OnBar(FromTop(bar, v)));
      bar.numberOfSteps = 0;
      var w = ((RectTransform)template.transform).rect.width;
      _barW = w >= 4 && w <= 24 ? w : 12;
      return bar;
    }

    /// <summary>Bar value to 0-at-top position, and back: a BottomToTop bar is 1 at the top.</summary>
    static float FromTop(Scrollbar bar, float v) => bar.direction == Scrollbar.Direction.BottomToTop ? 1 - v : v;

    void SyncBar(float position)
    {
      if (_bar != null && _bar.gameObject.activeSelf) _bar.SetValueWithoutNotify(FromTop(_bar, position));
    }

    /// <summary>The name the map screen shows for a location id (LocationSettings.Location.LocalizedName), in the game's language.</summary>
    static string MapName(string id)
    {
      if (!TarkovApplication.Exist(out var app)) return null;
      return app.Session?.LocationSettings?.locations?.Values
        .FirstOrDefault(l => string.Equals(l.Id, id, StringComparison.OrdinalIgnoreCase))?.LocalizedName;
    }

    TextMeshProUGUI Text(Transform parent, string text, float size, TextAlignmentOptions align)
    {
      var t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
      t.transform.SetParent(parent, false);
      if (_style != null)
      {
        t.font = _style.font;
        t.fontSharedMaterial = _style.fontSharedMaterial;
        t.color = _style.color;
        t.fontStyle = _style.fontStyle;
      }
      t.fontSize = size;
      t.text = text;
      t.raycastTarget = false;
      t.alignment = align;
      return t;
    }

    static void Place(RectTransform rt, float x, float y, float w, float h)
    {
      rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
      rt.anchoredPosition = new Vector2(x, -y);
      rt.sizeDelta = new Vector2(w, h);
    }

    Component Header(string title, float y)
    {
      var t = Text(_content, title, _style != null ? _style.fontSize - 2 : 14, TextAlignmentOptions.BottomLeft);
      Place(t.rectTransform, 2, y, Cols * Cell, HeaderHeight - 4);
      return t;
    }

    /// <summary>The stock grid's cell background under a row of n cells.</summary>
    Component Cells(int n, float y)
    {
      var img = new GameObject("Cells", typeof(RectTransform)).AddComponent<Image>();
      img.transform.SetParent(_content, false);
      if (_cellStyle != null)
      {
        img.sprite = _cellStyle.sprite;
        img.type = _cellStyle.type;
        img.pixelsPerUnitMultiplier = _cellStyle.pixelsPerUnitMultiplier;
        img.material = _cellStyle.material;
        img.color = _cellStyle.color;
      }
      else img.color = new Color(1f, 1f, 1f, 0.04f);
      Place(img.rectTransform, 0, y, n * Cell, Cell);
      img.transform.SetAsFirstSibling();
      return img;
    }

    /// <summary>An empty currency cell: its code and 0, dimmed. Right-click sets its target.</summary>
    Component EmptyMoney(string tpl, string code, int col, float y)
    {
      var cell = new GameObject("Empty " + code, typeof(RectTransform)).AddComponent<Image>();
      cell.transform.SetParent(_content, false);
      cell.color = Color.clear;
      Place(cell.rectTransform, col * Cell, y, Cell, Cell);
      cell.gameObject.AddComponent<MoneyTarget.Cell>().Tpl = tpl;
      var name = Text(cell.transform, code, 13, TextAlignmentOptions.TopRight);
      var amount = Text(cell.transform, "0", 13, TextAlignmentOptions.BottomRight);
      foreach (var t in new[] { name, amount })
      {
        t.color = t.color * Dim;
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = new Vector2(3, 2);
        t.rectTransform.offsetMax = new Vector2(-3, -2);
      }
      return cell;
    }

    /// <summary>A stock grid's drop highlight: its panel sprite and GridView.GetHighlightColor for the operation.</summary>
    void ShowHighlight(int col, float y, int cells, OperationResult operation, DragItemContext itemContext, ItemContext target)
    {
      if (_highlight == null)
      {
        _highlight = new GameObject("Highlight", typeof(RectTransform)).AddComponent<Image>();
        _highlight.transform.SetParent(_content, false);
        _highlight.raycastTarget = false;
        var style = _gridTemplate?._highlightPanel;
        if (style != null)
        {
          _highlight.sprite = style.sprite;
          _highlight.type = style.type;
          _highlight.material = style.material;
        }
      }
      _highlight.color = _gridTemplate != null ? _gridTemplate.GetHighlightColor(itemContext, operation, target)
        : operation.Succeeded ? new Color(0.06f, 0.38f, 0.06f, 0.57f) : new Color(0.68f, 0f, 0f, 0.57f);
      Place(_highlight.rectTransform, col * Cell, y, cells * Cell, Cell);
      _highlight.transform.SetAsLastSibling();
      _highlight.gameObject.SetActive(true);
    }

    public void DisableHighlight()
    {
      if (_highlight != null) _highlight.gameObject.SetActive(false);
    }

    void PlaceView(ItemView v, int col, float y)
    {
      var rt = (RectTransform)v.transform;
      rt.SetParent(_content, false);
      rt.localScale = Vector3.one;
      rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
      rt.anchoredPosition = new Vector2(col * Cell, -y);
      v.gameObject.SetActive(true);
    }

    void ScrollTo(float top) => _content.anchoredPosition = new Vector2(0, top);

    void Resize(float contentHeight)
    {
      _content.sizeDelta = new Vector2(Cols * Cell, contentHeight);
      _viewH = Mathf.Clamp(contentHeight, Cell, MaxView);
      var bar = _bar != null && ScrollBarMath.Needed(_viewH, contentHeight);
      var size = new Vector2(Cols * Cell + (bar ? _barW : 0), _viewH);
      if (_bar != null)
      {
        _bar.gameObject.SetActive(bar);
        Place((RectTransform)_bar.transform, Cols * Cell, 0, _barW, _viewH);
        _bar.size = ScrollBarMath.Size(_viewH, contentHeight);
      }
      _root.sizeDelta = size;
      var layout = GetComponent<LayoutElement>();
      layout.preferredWidth = layout.minWidth = size.x;
      layout.preferredHeight = layout.minHeight = size.y;
    }
  }
}
