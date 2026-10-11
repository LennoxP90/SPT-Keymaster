using System;
using EFT.UI;
using Keymaster.Shared;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Keymaster.Client.UI
{
  /// <summary>SET TARGET on a money cell: the game's split dialog with a 0 to one stack range, saved to the profile state.</summary>
  public static class MoneyTarget
  {
    public static void Ask(string currencyTpl)
    {
      var dialog = ItemUiContext.Instance?.SplitDialog;
      if (dialog == null) return;
      var cap = KeymasterIds.StackCap(currencyTpl);
      var current = KeymasterSettings.Target(currencyTpl);
      dialog.Show(Title(currencyTpl), 0, cap, 0, cap, current, Input.mousePosition, value =>
      {
        dialog.Hide();
        Save(currencyTpl, value);
      }, dialog.Hide, SplitDialog.ESplitDialogType.Step, allowZero: true);
      ShowAmount(dialog._stepSlider._valueInput, current, cap);
    }

    /// <summary>The step slider leaves the box's old text when the value matches its last one, and the box may be too short for a full rouble stack.</summary>
    static void ShowAmount(TMPro.TMP_InputField box, int current, int cap)
    {
      var digits = cap.ToString().Length;
      if (box.characterLimit > 0 && box.characterLimit < digits) box.characterLimit = digits;
      box.text = current.ToString();
    }

    static void Save(string currencyTpl, int value)
    {
      var target = MoneyPlanner.ClampTarget(currencyTpl, value);
      KeymasterApi.State.MoneyTargets[currencyTpl] = target;
      try { KeymasterApi.SaveState(); Plugin.Log($"{Swapper.Code(currencyTpl)} target {target}"); }
      catch (Exception ex) { Plugin.Warn("target not saved: " + ex.Message); }
    }

    static string Title(string tpl) =>
      tpl == KeymasterIds.Rub ? "Roubles target" : tpl == KeymasterIds.Usd ? "Dollars target" : tpl == KeymasterIds.Eur ? "Euros target" : "GP coins target";

    /// <summary>An empty currency cell has no item view and so no context menu; right-click opens the dialog directly.</summary>
    public sealed class Cell : MonoBehaviour, IPointerClickHandler
    {
      public string Tpl;

      public void OnPointerClick(PointerEventData e)
      {
        if (e.button == PointerEventData.InputButton.Right) Ask(Tpl);
      }
    }
  }
}
