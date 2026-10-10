using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using Keymaster.Shared;
using SPT.Reflection.Patching;

namespace Keymaster.Client
{
  public static class Returner
  {
    public static async Task<string> Run(InventoryAdapter a)
    {
      if (a.Case == null) return null;
      var wanted = KeymasterApi.CurrentMapKeys().Concat(KeymasterApi.State.AlwaysBring).Distinct().ToList();
      var home = ReturnPlanner.Plan(wanted, a.Copies().Where(x => x.Where == KeyHome.Case).ToList(), KeymasterSettings.Copy.Value);
      var moved = 0; var stuck = 0;
      foreach (var id in home) if (await a.MoveHome(id)) moved++; else stuck++;

      var money = "";
      for (var i = 0; i < KeymasterIds.Currencies.Length; i++)
      {
        var cur = KeymasterIds.Currencies[i];
        var mp = MoneyPlanner.Plan(cur, KeymasterSettings.Target(cur), a.InCell(i + 1), new StackRef[0]);
        if (mp.Excess > 0 && await a.ReturnExcess(cur, mp.Excess, i + 1)) money += $", {Swapper.Code(cur)} -{mp.Excess}";
      }
      if (moved == 0 && stuck == 0 && money.Length == 0) return null;
      return $"returned {moved} keys" + (stuck > 0 ? $", {stuck} left in the case (no room)" : "") + money;
    }

    /// <summary>Runs the return on the menu inventory, logs its line and sends the moves to the server.</summary>
    internal static async Task RunOnMenu(string why)
    {
      try
      {
        if (!TarkovApplication.Exist(out var app)) return;
        var a = Swapper.MenuInventory(app);
        for (var i = 0; i < 50 && a?.HasStash != true; i++) { await Task.Delay(200); a = Swapper.MenuInventory(app); }
        if (a?.HasStash != true) { Plugin.Warn("no menu stash to return to"); return; }
        var line = await Run(a);
        if (line == null) return;
        Plugin.Log(line + " (" + why + ")");
        var sent = await app.Session.FlushOperationQueue();
        if (!sent.Succeed) Plugin.Warn("the server did not confirm the return: " + sent.Error);
      }
      catch (System.Exception ex) { Plugin.Warn("return failed: " + ex.Message); }
    }
  }

  /// <summary>Records the location a local game ended on; the hideout is not a raid.</summary>
  public class GameEndPatch : ModulePatch
  {
    internal static string EndedOn;
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TarkovApplication), nameof(TarkovApplication.OnGameEnd));

    [PatchPrefix]
    static void Prefix(JsonType.LocationSettings.Location location)
    {
      var id = location?.Id;
      EndedOn = string.IsNullOrEmpty(id) || id == "hideout" ? null : id;
    }
  }

  /// <summary>Back in the menu after a raid: spare keys and excess money go home.</summary>
  public class MenuBackPatch : ModulePatch
  {
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(TarkovApplication), nameof(TarkovApplication.ComebackToMainMenu));

    [PatchPostfix]
    static void Postfix(Task __result)
    {
      var endedOn = GameEndPatch.EndedOn;
      GameEndPatch.EndedOn = null;
      if (endedOn == null) return;
      __result.ContinueWith(t => { if (!t.IsFaulted && !t.IsCanceled) _ = Returner.RunOnMenu("after " + endedOn); },
        TaskScheduler.FromCurrentSynchronizationContext());
    }
  }

  /// <summary>Keymaster entries on item context menus: Dump excess on the equipped case, always-bring on keys.</summary>
  public class ContextMenuPatch : ModulePatch
  {
    const string Dump = "Keymaster: dump excess", Bring = "Keymaster: bring on every map", Stop = "Keymaster: stop bringing";

    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.GetItemContextInteractions));

    [PatchPostfix]
    static void Postfix(ItemContext itemContext, ContextInteractions<EItemInfoButton> __result)
    {
      var item = itemContext?.Item;
      if (__result == null || item == null) return;
      foreach (var key in new[] { Dump, Bring, Stop }) __result.RemoveDynamicInteraction(key);

      if (item.StringTemplateId == KeymasterIds.CaseTpl && IsMenuCase(item))
        __result.method_2("keymaster.dump", Dump, () => _ = Returner.RunOnMenu("dump excess"));
      else if (item.GetItemComponent<KeyComponent>() != null)
      {
        var tpl = item.StringTemplateId;
        var always = KeymasterApi.State.AlwaysBring;
        if (always.Contains(tpl)) __result.method_2("keymaster.stop", Stop, () => { always.Remove(tpl); KeymasterApi.SaveState(); });
        else __result.method_2("keymaster.bring", Bring, () => { always.Add(tpl); KeymasterApi.SaveState(); });
      }
    }

    /// <summary>The case the menu inventory has equipped (menu or hideout, never a raid), not a spare in the stash.</summary>
    static bool IsMenuCase(Item item) =>
      (!Singleton<GameWorld>.Instantiated || Singleton<GameWorld>.Instance.LocationId == "hideout")
      && TarkovApplication.Exist(out var app) && Swapper.MenuInventory(app)?.Case?.Id == item.Id;
  }
}
