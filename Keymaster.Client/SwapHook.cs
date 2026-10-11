using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using HarmonyLib;
using Keymaster.Shared;
using SPT.Reflection.Patching;

namespace Keymaster.Client
{
  public static class Swapper
  {
    /// <summary>accessKeys: the location's own access cards, always wanted whatever the key table holds.</summary>
    public static async Task<(string Line, bool Warn)> Run(InventoryAdapter a, string location, string variant, IEnumerable<string> accessKeys)
    {
      var entry = KeyTable.EntryKey(location, variant);
      var keys = KeyTable.Resolve(KeymasterApi.Table, location, variant);
      var wanted = keys.Concat(accessKeys).Concat(KeymasterApi.State.AlwaysBring).Distinct().ToList();
      var mapChanged = KeymasterApi.State.CurrentMap != entry;
      var plan = LoadPlanner.Plan(wanted, a.Copies(), KeymasterSettings.Copy.Value, mapChanged);

      var failed = 0;
      foreach (var id in plan.ToHome) if (!await a.MoveHome(id)) failed++;
      var loaded = 0;
      foreach (var id in plan.ToCase) if (await a.MoveToCase(id)) loaded++; else failed++;

      var money = "";
      for (var i = 0; i < KeymasterIds.Currencies.Length; i++)
      {
        var cur = KeymasterIds.Currencies[i];
        var target = KeymasterSettings.Target(cur);
        if (target <= 0) continue;
        var mp = MoneyPlanner.Plan(cur, target, a.InCell(i + 1), a.StashStacks(cur), a.CarriedOutsideCell(cur, i + 1));
        var added = await a.TopUp(mp, i + 1);
        if (added > 0) money += $", {Code(cur)} +{added}";
        if (mp.Shortfall > 0) money += $" ({mp.Shortfall} short)";
      }

      KeymasterApi.State.CurrentMap = entry;
      KeymasterApi.SaveState();
      return ($"{entry} {plan.Present + loaded}/{plan.Wanted} keys" + (failed > 0 ? $", {failed} moves skipped" : "") + money, failed > 0);
    }

    internal static string Code(string tpl) => tpl == KeymasterIds.Rub ? "RUB" : tpl == KeymasterIds.Usd ? "USD" : tpl == KeymasterIds.Eur ? "EUR" : "GP";

    internal static InventoryAdapter MenuInventory(TarkovApplication app)
    {
      var menu = AccessTools.Field(typeof(TarkovApplication), "_menuOperation").GetValue(app) as MainMenuShowOperation;
      return InventoryAdapter.For(menu?.InventoryController);
    }
  }

  /// <summary>Arms the swap when a PMC raid's matching begins (not on a transit).</summary>
  public class ArmPatch : ModulePatch
  {
    internal static bool Armed;
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TarkovApplication), nameof(TarkovApplication.LocalGameMatching));

    [PatchPrefix]
    static void Prefix(TarkovApplication __instance, bool inTransition)
    {
      var rs = __instance.CurrentRaidSettings;
      Armed = !inTransition && rs != null && rs.IsPmc && KeymasterApi.Available;
    }
  }

  /// <summary>Runs the swap at LocalGameMatching's FlushOperationQueue, then lets the real flush confirm it.</summary>
  public class FlushPatch : ModulePatch
  {
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ClientBackendSession), nameof(ClientBackendSession.FlushOperationQueue));

    [PatchPrefix]
    static bool Prefix(ClientBackendSession __instance, ref Task<IResult> __result)
    {
      if (!ArmPatch.Armed) return true;
      ArmPatch.Armed = false;
      __result = SwapThenFlush(__instance);
      return false;
    }

    static async Task<IResult> SwapThenFlush(ClientBackendSession session)
    {
      await Task.Yield();
      try { await Swap(); }
      catch (System.Exception ex) { Plugin.Warn("swap failed, raid continues with the case as it is: " + ex.Message); }
      var flushed = await session.FlushOperationQueue();
      if (!flushed.Succeed) throw new System.Exception("Keymaster: the server did not confirm the swap: " + flushed.Error);
      RequireAccessCard();
      return flushed;
    }

    /// <summary>A gated map never starts without its card worn, at the level the map screen asks for one: the throw takes the game's HandleError path back to the menu.</summary>
    static void RequireAccessCard()
    {
      if (!TarkovApplication.Exist(out var app)) return;
      var location = app.CurrentRaidSettings.SelectedLocation;
      var cards = AccessKeys(location);
      var worn = Swapper.MenuInventory(app)?.Wears(cards) != false;
      if (!AccessGate.MustBlock(cards.Length, app.Session.Profile.Info.Level, location.MinPlayerLvlAccessKeys, worn)) return;
      Plugin.Warn($"{location.Id}: its access card could not be loaded; back to the menu");
      throw new System.Exception("Keymaster: no access card for " + location.Id);
    }

    static string[] AccessKeys(JsonType.LocationSettings.Location location) => location?.AccessKeys ?? new string[0];

    static async Task Swap()
    {
      if (!TarkovApplication.Exist(out var app)) return;
      var a = Swapper.MenuInventory(app);
      if (a?.Case == null) return;
      KeymasterApi.Refresh();
      Plugin.Report(await Swapper.Run(a, app.CurrentRaidSettings.SelectedLocation.Id, LearnPatch.MapVariant(), AccessKeys(app.CurrentRaidSettings.SelectedLocation)));
    }
  }
}
