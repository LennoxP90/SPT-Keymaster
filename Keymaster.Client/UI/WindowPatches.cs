using System.Reflection;
using EFT.InventoryLogic;
using EFT.Settings.Game;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Keymaster.Client.UI
{
  /// <summary>The case and the vault open in the game's own container window, with the Keymaster panel as its content.</summary>
  public class WindowShowPatch : ModulePatch
  {
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GridWindow), nameof(GridWindow.Show),
      new[] { typeof(CompoundItem), typeof(ItemContext), typeof(ItemController), typeof(ItemUiContext), typeof(GameSettingsGroup.EPriorityWindowMode) });

    [PatchPostfix]
    static void Postfix(GridWindow __instance, CompoundItem compoundItem, ItemContext itemContext, ItemController itemController, ItemUiContext itemUiContext)
    {
      if (KeymasterPanel.Hosts(compoundItem)) KeymasterPanel.Attach(__instance, compoundItem, itemContext, itemController, itemUiContext);
    }
  }

  /// <summary>The window never builds its stock grid views for the case or the vault: 2000-cell hidden grids the panel replaces.</summary>
  public class WindowGridsPatch : ModulePatch
  {
    static readonly AccessTools.FieldRef<GridWindow, CompoundItem> Item = AccessTools.FieldRefAccess<GridWindow, CompoundItem>("_item");

    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GridWindow), nameof(GridWindow.ShowGrids));

    [PatchPrefix]
    static bool Prefix(GridWindow __instance) => !KeymasterPanel.Hosts(Item(__instance));
  }

  /// <summary>The case and the vault are never the priority (quick-transfer) window: their hidden grids are not quick-move targets.</summary>
  public class PriorityWindowPatch : ModulePatch
  {
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GridWindowContext), nameof(GridWindowContext.CanSelectGridWindow));

    [PatchPostfix]
    static void Postfix(ItemContext itemContext, ref bool __result)
    {
      if (__result && KeymasterPanel.Hosts(itemContext.Item)) __result = false;
    }
  }
}
