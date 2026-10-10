using System.Linq;
using System.Reflection;
using EFT;
using EFT.Interactive;
using Keymaster.Client.Wire;
using SPT.Reflection.Patching;

namespace Keymaster.Client
{
  /// <summary>Once per raid: every door/container KeyId in the loaded scene goes to the server.</summary>
  public class LearnPatch : ModulePatch
  {
    protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

    [PatchPostfix]
    static void Postfix(GameWorld __instance)
    {
      try
      {
        Plugin.Detail("OnGameStarted on " + __instance.GetType().Name + " location=" + __instance.LocationId);
        if (string.IsNullOrEmpty(__instance.LocationId)) return;
        var keys = LocationScene.GetAllObjects<WorldInteractiveObject>()
          .Select(o => o.KeyId).Where(k => !string.IsNullOrEmpty(k)).Distinct().ToList();
        var req = new LearnRequestC { Location = __instance.LocationId, Variant = MapVariant(), EftBuild = UnityEngine.Application.version, Keys = keys };
        System.Threading.Tasks.Task.Run(() => Post(req));
        Plugin.Detail($"learned {req.Location}: {keys.Count} keys");
      }
      catch (System.Exception ex) { Plugin.Warn("learning failed: " + ex.Message); }
    }

    static void Post(LearnRequestC req)
    {
      try { KeymasterApi.Learn(req); }
      catch (System.Exception ex) { Plugin.Warn("learn post failed: " + ex.Message); }
    }

    /// <summary>MapVariants' current variant when installed and decided, else null. Soft: no compile-time reference.</summary>
    public static string MapVariant()
    {
      var api = System.Type.GetType("MapVariants.Client.Api.Maps, MapVariants.Client");
      var prop = api?.GetProperty("CurrentVariant", BindingFlags.Public | BindingFlags.Static);
      return prop?.GetValue(null) as string;
    }
  }
}
