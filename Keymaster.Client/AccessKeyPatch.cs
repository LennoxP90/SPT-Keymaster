using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using Keymaster.Shared;

namespace Keymaster.Client
{
  /// <summary>Lets the map screen accept an access card that is in the stash or vault; the swap then carries it in.</summary>
  public class AccessKeyPatch : ModulePatch
  {
    protected override MethodBase GetTargetMethod() =>
      typeof(MainMenuShowOperation).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Single(m => m.ReturnType == typeof(bool) && m.GetParameters().Select(p => p.ParameterType)
          .SequenceEqual(new[] { typeof(IEnumerable<string>), typeof(EquipmentSlot?), typeof(string).MakeByRefType() }));

    /// <summary>Moves the copy the swap would load (same planner, same copy choice) into the case now. Only when it
    /// is in the case afterwards is the card accepted; otherwise the game shows its own missing-keycard screen.</summary>
    [PatchPostfix]
    static void Postfix(MainMenuShowOperation __instance, IEnumerable<string> accessKeys, EquipmentSlot? specialSlot, ref string keyId, ref bool __result)
    {
      if (__result || specialSlot.HasValue || !KeymasterApi.Available) return;
      var a = InventoryAdapter.For(__instance.InventoryController);
      if (a?.Case == null) return;
      var copies = a.Copies();
      var pick = accessKeys.Distinct().ToList()
        .Select(tpl => copies.FirstOrDefault(c => c.ItemId == LoadPlanner.Plan(new[] { tpl }, copies, KeymasterSettings.Copy.Value, false).ToCase.FirstOrDefault()))
        .FirstOrDefault(c => c != null);
      if (pick == null) return;
      _ = a.MoveToCase(pick.ItemId);   // applied locally at once; the Ready path's own flush confirms it with the server
      if (!a.Copies().Any(c => c.ItemId == pick.ItemId && c.Where == KeyHome.Case)) { Plugin.Log("access card could not be moved into the case"); return; }
      keyId = pick.ItemId;
      __result = true;
      Plugin.Log("access card moved into the case at the map screen");
    }
  }
}
