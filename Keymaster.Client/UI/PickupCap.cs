using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT.InventoryLogic;
using HarmonyLib;
using Keymaster.Shared;
using SPT.Reflection.Patching;

namespace Keymaster.Client.UI
{
  /// <summary>
  /// The 10-slot PICKUPS cap on the case's key grid, at the grid's own compatibility check, so every way in respects it:
  /// a panel drop, a drop on the case's icon, a quick move, looting in a raid.
  /// </summary>
  public class PickupCap : ModulePatch
  {
    /// <summary>Items Keymaster itself is loading (the swap, access cards): always let in.</summary>
    internal static readonly HashSet<string> Loading = new HashSet<string>();

    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(Grid), nameof(Grid.CheckCompatibility));

    [PatchPostfix]
    static void Postfix(Grid __instance, Item item, ref bool __result)
    {
      if (!__result || __instance.ParentItem?.StringTemplateId != KeymasterIds.CaseTpl || item.GetItemComponent<KeyComponent>() == null
          || Loading.Contains(item.Id)) return;
      var wanted = KeymasterApi.WantedKeys();
      var inCase = __instance.Items.Select(i => new KeyCopy { ItemId = i.Id, Tpl = i.StringTemplateId }).ToList();
      __result = CaseLayout.Accepts(CaseLayout.Split(inCase, wanted), item.StringTemplateId, wanted);
    }
  }
}
