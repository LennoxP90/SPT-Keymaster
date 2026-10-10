using System.Linq;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Keymaster.Client.UI
{
  /// <summary>Everything the panel draws besides EFT's item views: window, title, close button, viewport, headers and positions. Logic lives in KeymasterPanel.cs.</summary>
  public sealed partial class KeymasterPanel
  {
    const int HeaderHeight = 28, TitleHeight = 30, Pad = 10, MaxRows = 8;
    RectTransform _root, _content;
    TMP_FontAsset _font;
    float _viewH;

    /// <summary>The stash grid's canvas, as the spike found raycasts need; any shown grid's in a raid.</summary>
    static Canvas ScreenCanvas(ItemUiContext ui, ItemController ic)
    {
      var stash = (ic as InventoryController)?.Inventory.Stash;
      var grids = FindObjectsOfType<GridView>().Where(g => g.isActiveAndEnabled).ToList();
      var grid = grids.FirstOrDefault(g => stash != null && g.Grid?.ParentItem == stash) ?? grids.FirstOrDefault();
      return grid != null ? grid.GetComponentInParent<Canvas>() : ui.GetComponentInParent<Canvas>();
    }

    void Build(Canvas screen)
    {
      _font = screen.GetComponentInChildren<TextMeshProUGUI>(true)?.font;
      _root = (RectTransform)transform;
      _root.SetParent(screen.rootCanvas.transform, false);
      _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(0.5f, 0.5f);
      _root.SetAsLastSibling();
      GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.95f);
      var canvas = gameObject.AddComponent<Canvas>();
      canvas.overrideSorting = true;
      canvas.sortingLayerID = screen.sortingLayerID;
      canvas.sortingOrder = screen.sortingOrder + 1;
      gameObject.AddComponent<GraphicRaycaster>();

      var title = Text(_root, _item.ShortName.Localized(), 18);
      Place(title.rectTransform, Pad, 0, Cols * Cell - TitleHeight, TitleHeight);
      CloseButton();

      var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
      var vp = (RectTransform)viewport.transform;
      vp.SetParent(_root, false);
      vp.anchorMin = Vector2.zero;
      vp.anchorMax = Vector2.one;
      vp.offsetMin = new Vector2(Pad, Pad);
      vp.offsetMax = new Vector2(-Pad, -TitleHeight);
      viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);

      _content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
      _content.SetParent(vp, false);
      _content.anchorMin = _content.anchorMax = _content.pivot = new Vector2(0, 1);

      var scroll = gameObject.AddComponent<ScrollRect>();
      scroll.viewport = vp;
      scroll.content = _content;
      scroll.horizontal = false;
      scroll.movementType = ScrollRect.MovementType.Clamped;
      scroll.scrollSensitivity = Cell;
      scroll.onValueChanged.AddListener(_ => ShowVisible());
    }

    void CloseButton()
    {
      var go = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
      var rt = (RectTransform)go.transform;
      rt.SetParent(_root, false);
      rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one;
      rt.anchoredPosition = new Vector2(-4, -4);
      rt.sizeDelta = new Vector2(TitleHeight - 8, TitleHeight - 8);
      go.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
      go.GetComponent<Button>().onClick.AddListener(Close);
      var x = Text(rt, "X", 16);
      x.alignment = TextAlignmentOptions.Center;
      x.rectTransform.anchorMin = Vector2.zero;
      x.rectTransform.anchorMax = Vector2.one;
      x.rectTransform.offsetMin = x.rectTransform.offsetMax = Vector2.zero;
    }

    TextMeshProUGUI Text(Transform parent, string text, float size)
    {
      var t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
      t.transform.SetParent(parent, false);
      if (_font != null) t.font = _font;
      t.fontSize = size;
      t.text = text;
      t.raycastTarget = false;
      t.alignment = TextAlignmentOptions.MidlineLeft;
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
      var t = Text(_content, title, 16);
      t.color = new Color(0.85f, 0.8f, 0.6f);
      Place(t.rectTransform, 4, y, Cols * Cell - 4, HeaderHeight);
      return t;
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

    void Resize(float contentHeight)
    {
      _content.sizeDelta = new Vector2(Cols * Cell, contentHeight);
      _viewH = Mathf.Clamp(contentHeight, Cell, MaxRows * Cell);
      _root.sizeDelta = new Vector2(Cols * Cell + 2 * Pad, TitleHeight + _viewH + Pad);
    }
  }
}
