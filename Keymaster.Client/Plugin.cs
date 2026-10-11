using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Keymaster.Shared;

namespace Keymaster.Client
{
  [BepInPlugin("com.lennoxp90.keymaster", "Keymaster", "0.1.0")]
  public class Plugin : BaseUnityPlugin
  {
    static ManualLogSource _log;

    void Awake()
    {
      _log = Logger;
      KeymasterSettings.Bind(Config);
      if (!KeymasterApi.Refresh(initial: true)) { Log("server half not found; Keymaster is off"); return; }
      new LearnPatch().Enable();
      new ArmPatch().Enable();
      new FlushPatch().Enable();
      new GameEndPatch().Enable();
      new MenuBackPatch().Enable();
      new ContextMenuPatch().Enable();
      new AccessKeyPatch().Enable();
      new UI.WindowShowPatch().Enable();
      new UI.WindowGridsPatch().Enable();
      new UI.PriorityWindowPatch().Enable();
      new UI.PickupCap().Enable();
      new UI.MergedUsesPatch().Enable();
      new ShaderRebind().Enable();
      new IconRefresh().Enable();
    }

    public static void Log(string line) => _log.LogInfo("Keymaster: " + line);
    public static void Warn(string line) => _log.LogWarning("Keymaster: " + line);
    /// <summary>One summary line: a warning when something was left where it is.</summary>
    public static void Report((string Line, bool Warn) s) { if (s.Warn) Warn(s.Line); else Log(s.Line); }
    public static void Detail(string line) { if (KeymasterSettings.Verbose.Value) _log.LogInfo("Keymaster: " + line); }
  }

  public static class KeymasterSettings
  {
    public static ConfigEntry<CopyChoice> Copy;
    public static ConfigEntry<bool> Verbose;

    public static void Bind(ConfigFile c)
    {
      Copy = c.Bind("Keys", "Key copy", CopyChoice.Freshest, "Which copy goes into the case when you own several");
      Verbose = c.Bind("Debug", "Verbose logging", false, "One log line per moved item");
    }

    /// <summary>Money targets live in the profile state, set from a money cell's SET TARGET.</summary>
    public static int Target(string currencyTpl) =>
      KeymasterApi.State.MoneyTargets.TryGetValue(currencyTpl, out var t) ? MoneyPlanner.ClampTarget(currencyTpl, t) : 0;
  }
}
