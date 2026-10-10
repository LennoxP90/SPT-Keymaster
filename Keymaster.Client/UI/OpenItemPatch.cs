using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using Keymaster.Shared;
using SPT.Reflection.Patching;

namespace Keymaster.Client.UI
{
  /// <summary>Opening the case or the vault shows the Keymaster panel instead of the stock grid window.</summary>
  public class OpenItemPatch : ModulePatch
  {
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.OpenItem));

    [PatchPrefix]
    static bool Prefix(ItemUiContext __instance, CompoundItem item, ItemContext itemContext)
    {
      if (item.StringTemplateId != KeymasterIds.CaseTpl && item.StringTemplateId != KeymasterIds.VaultTpl) return true;
      return !KeymasterPanel.Open(item, itemContext, __instance);
    }
  }

  /// <summary>The panel closes with the stock windows (inventory screen closed, screen change).</summary>
  public class CloseWindowsPatch : ModulePatch
  {
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.CloseAllWindows));

    [PatchPostfix]
    static void Postfix() => KeymasterPanel.Close();
  }
}
