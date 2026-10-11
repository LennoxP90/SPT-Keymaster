using System;
using System.Linq;
using EFT;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    Image _cellStyle;
    float _viewH;

    /// <summary>Takes the place of the window's (never shown) grid view; caption, close button, frame and dragging stay the window's.</summary>
    void Build(GridWindow window)
    {
      var grids = WindowGrids(window);
      _style = window.Caption;
      _cellStyle = (grids as GeneratedGridsView)?._gridViewTemplate.transform.Find("Background Tile")?.GetComponent<Image>();
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
      var size = new Vector2(Cols * Cell, _viewH);
      _root.sizeDelta = size;
      var layout = GetComponent<LayoutElement>();
      layout.preferredWidth = layout.minWidth = size.x;
      layout.preferredHeight = layout.minHeight = size.y;
    }
  }
}
