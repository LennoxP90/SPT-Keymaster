using System;
using System.Linq;
using Diz.LanguageExtensions;
using EFT;
using EFT.Communications;
using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using Keymaster.Shared;
using SPT.Reflection.Patching;

namespace Keymaster.Client.UI
{
  /// <summary>Ctrl-click of a money stack with the KM Case as the priority window: moves only what fits under the
  /// currency cell's cap (MoneyPlanner.QuickTransferAmount), splitting off the rest; a full cell gives the no-room warning.</summary>
  public class QuickMoneyPatch : ModulePatch
  {
    static readonly AccessTools.FieldRef<ItemUiContext, CompoundItem> Priority = AccessTools.FieldRefAccess<ItemUiContext, CompoundItem>("_currentPriorityCompoundItem");

    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.QuickFindAppropriatePlace));

    [PatchPrefix]
    static bool Prefix(ItemUiContext __instance, ItemContext itemContext, ItemController controller, bool forcePutInStash, bool displayWarnings, bool simulate, ref OperationResult __result)
    {
      var item = itemContext.Item;
      var target = Priority(__instance);
      var cur = Array.IndexOf(KeymasterIds.Currencies, item.StringTemplateId);
      if (forcePutInStash || cur < 0 || target == null || target.StringTemplateId != KeymasterIds.CaseTpl || item.IsChildOf(target)) return true;
      __result = Plan(item, target.Grids[cur + 1], controller, simulate);
      if (__result.Failed && displayWarnings) { var e = __result.Error; NotificationManager.DisplayWarningNotification((e is InventoryError ie ? ie.GetLocalizedDescription() : e.ToString()).Localized()); }
      return false;
    }

    /// <summary>The same currency already in the equipment outside this cell, not counting the stack being moved: the game's
    /// lobby limit counts all of it, since the case sits in the equipment.</summary>
    static int CarriedElsewhere(Item item, Grid grid, ItemController ic) =>
      ic is InventoryController inv
        ? inv.Inventory.GetPlayerItems(EPlayerItems.Equipment)
            .Where(i => i.StringTemplateId == item.StringTemplateId && i != item && i.Parent?.Container != grid).Sum(i => i.StackObjectsCount)
        : 0;

    internal static OperationResult Plan(Item item, Grid grid, ItemController ic, bool simulate)
    {
      var onto = grid.Items.FirstOrDefault();
      var count = MoneyPlanner.QuickTransferAmount(onto?.StackObjectsCount ?? 0, item.StackObjectsCount, KeymasterIds.MoneyCap(item.StringTemplateId), CarriedElsewhere(item, grid, ic));
      if (count <= 0) return new NoFreeSpaceError(item);
      var whole = count >= item.StackObjectsCount;
      if (onto != null) return whole ? (OperationResult)ItemManipulator.Merge(item, onto, ic, simulate) : ItemManipulator.TransferExact(item, count, onto, ic, simulate);
      var to = grid.FindLocationForItem(item);
      if (to == null) return new NoFreeSpaceError(item);
      return whole ? (OperationResult)ItemManipulator.Move(item, to, ic, simulate) : ItemManipulator.SplitExact(item, count, to, ic, ic, simulate);
    }
  }

  /// <summary>Money dropped on the KM Case's own icon: the stock drop-onto-item path would move the whole stack and hit the
  /// lobby limit, so it takes the same capped plan as the panel.</summary>
  public class CaseIconMoneyPatch : ModulePatch
  {
    protected override MethodBase GetTargetMethod() =>
      AccessTools.Method(typeof(ItemController), nameof(ItemController.ExecutePossibleAction), new[] { typeof(ItemContext), typeof(Item), typeof(bool), typeof(bool) });

    [PatchPrefix]
    static bool Prefix(ItemController __instance, ItemContext itemContext, Item targetItem, bool simulate, ref OperationResult __result)
    {
      var item = itemContext.Item;
      var cur = Array.IndexOf(KeymasterIds.Currencies, item.StringTemplateId);
      if (cur < 0 || !(targetItem is CompoundItem target) || target.StringTemplateId != KeymasterIds.CaseTpl || item.IsChildOf(target)) return true;
      __result = QuickMoneyPatch.Plan(item, target.Grids[cur + 1], __instance, simulate);
      return false;
    }
  }
}
