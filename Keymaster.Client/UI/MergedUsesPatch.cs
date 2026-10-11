using System.Linq;
using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using Keymaster.Shared;
using SPT.Reflection.Patching;

namespace Keymaster.Client.UI
{
  /// <summary>In the case panel, a key's view shows the summed uses of every copy of that key in the case, in the stock key format (left/max).</summary>
  public class MergedUsesPatch : ModulePatch
  {
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GridItemView), nameof(GridItemView.UpdateInfo));

    [PatchPostfix]
    static void Postfix(GridItemView __instance)
    {
      if (!(__instance.Container is KeymasterPanel panel) || !panel.IsCase) return;
      var item = __instance.Item;
      if (item?.GetItemComponent<KeyComponent>() == null) return;
      var copies = panel.CopiesOf(item.StringTemplateId);
      if (copies.Count < 2) return;
      var sum = MergedUses.Sum(copies.Select(InventoryAdapter.UsesLeft));
      if (!sum.HasValue) { __instance.UpdateItemValue(string.Empty); return; }
      var max = copies.Sum(c => c.GetItemComponent<KeyComponent>().Template.MaximumNumberOfUsage);
      __instance.SetItemValue(GridItemView.EItemValueFormat.TwoValues, __instance.Examined, "#ff5335", sum.Value, max);
    }
  }
}
