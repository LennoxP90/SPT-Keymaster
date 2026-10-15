using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using Keymaster.Shared;
using SPT.Reflection.Patching;
using UnityEngine;

namespace Keymaster.Client
{
  /// <summary>
  /// The model bundles carry a property-only stub of the game's item shader, which renders flat white. Every item view,
  /// icon and inspect model is popped from the pool here, so this one postfix points our materials at the game's shader
  /// and fills the cubemap the bundle could not reference. The materials are the bundle's shared ones, so each is
  /// rebound once and later pops pass straight through.
  /// </summary>
  public class ShaderRebind : ModulePatch
  {
    const string ItemShader = "p0/Reflective/Bumped Specular SMap";
    const string CubeProperty = "_Cube";
    /// <summary>The donors' own cubemap, from the game's `cubemaps` bundle (a dependency of our bundles).</summary>
    const string DonorCube = "patron_cubemap_metall";

    /// <summary>
    /// Lookups before giving up. Only the PopOrCreate postfix looks up and counts, since by then our prefab and its
    /// dependency bundles (shaders, cubemaps) are loaded; an icon request comes before that and only reads the cache.
    /// </summary>
    const int MaxLookups = 3;

    static Shader _gameShader;
    static Texture _gameCube;
    static int _lookups;

    protected override MethodBase GetTargetMethod() =>
      AccessTools.Method(typeof(ObjectsFactory), nameof(ObjectsFactory.PopOrCreate), new[] { typeof(ResourceKey), typeof(ObjectsFactory.PoolsCategory) });

    [PatchPostfix]
    static void Postfix(ResourceKey resourceKey, GameObject __result)
    {
      if (__result == null || !ModelBundles.IsOurs(resourceKey?.path)) return;
      if (!Ready()) { if (!Settled) Plugin.Detail("game shader " + ItemShader + " not loaded yet, " + resourceKey.path + " stays unbound"); return; }
      var n = 0;
      foreach (var renderer in __result.GetComponentsInChildren<Renderer>(true))
        foreach (var material in renderer.sharedMaterials)
          if (Rebind(material)) n++;
      if (n > 0) Plugin.Detail("rebound " + n + " material(s) of " + resourceKey.path);
    }

    static bool Rebind(Material material)
    {
      if (material == null || material.shader == null || material.shader.name != ItemShader) return false;
      var swapped = material.shader != _gameShader;
      if (swapped) material.shader = _gameShader;
      if (_gameCube != null && material.GetTexture(CubeProperty) == null) { material.SetTexture(CubeProperty, _gameCube); swapped = true; }
      return swapped;
    }

    /// <summary>
    /// Looks up what is still missing, then true once the game's shader is known. Called only from the postfix. Shader
    /// and cubemap are looked up among loaded objects, cached once found and retried while not, up to MaxLookups times;
    /// then one warning and no more scans (Settled). The shader is the copy of that name with more than one pass (our
    /// stubs have one), the cubemap the donors' one by name from the `cubemaps` bundle. A borrowed game material on that
    /// shader with a cubemap set is the fallback for either.
    /// </summary>
    static bool Ready()
    {
      Lookup();
      return Resolved;
    }

    /// <summary>The game's shader is known. Cached state only, no lookup.</summary>
    internal static bool Resolved => _gameShader != null;

    /// <summary>Nothing more to look up: everything found, or the lookups are spent.</summary>
    internal static bool Settled => _gameShader != null && _gameCube != null || _lookups >= MaxLookups;

    static void Lookup()
    {
      if (Settled) return;
      _lookups++;
      var hadShader = _gameShader != null;
      if (_gameShader == null) _gameShader = LoadedShader();
      if (_gameCube == null) _gameCube = LoadedCube();
      if ((_gameShader == null || _gameCube == null) && BorrowedMaterial() is Material borrowed)
      {
        if (_gameShader == null) _gameShader = borrowed.shader;
        if (_gameCube == null) _gameCube = borrowed.GetTexture(CubeProperty);
        Plugin.Detail("borrowed from material " + borrowed.name);
      }
      if (_gameShader != null && (!hadShader || _gameCube != null)) Plugin.Detail($"game shader {_gameShader.GetInstanceID()} ({_gameShader.passCount} passes), cube {(_gameCube != null ? _gameCube.name : "none")}");
      if (_lookups < MaxLookups) return;
      if (_gameShader == null) Plugin.Warn($"game shader {ItemShader} not found after {MaxLookups} lookups; the case and vault render with the bundle's stub (flat white)");
      else if (_gameCube == null) Plugin.Warn($"cubemap {DonorCube} not found after {MaxLookups} lookups; the case and vault render without reflections");
    }

    // another mod's full compiled copy of this shader would also pass the pass count; the lowest instance id
    // (loaded first, i.e. the game's own bundle at startup) wins. Match on the source bundle if that ever bites.
    static Shader LoadedShader() =>
      Resources.FindObjectsOfTypeAll<Shader>()
        .Where(s => s != null && s.name == ItemShader && s.passCount > 1)
        .OrderBy(s => s.GetInstanceID())
        .FirstOrDefault();

    static Texture LoadedCube() => Resources.FindObjectsOfTypeAll<Cubemap>().FirstOrDefault(c => c != null && c.name == DonorCube);

    static Material BorrowedMaterial() =>
      Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m =>
        m != null && m.shader != null && m.shader.name == ItemShader && m.shader.passCount > 1
        && m.HasProperty(CubeProperty) && m.GetTexture(CubeProperty) != null);
  }

  /// <summary>
  /// The icon cache is keyed by template and contents, not by model, so an icon drawn before a model change (or before
  /// the models existed) would stay forever. Each of our icons is rendered fresh once per session, then cached as usual.
  /// It reads ShaderRebind's cached state only: the forced render pops our prefab, whose postfix does the lookup. The hash
  /// counts as refreshed once the shader is resolved or the lookup has given up; a render before that is redone on the
  /// next request.
  /// </summary>
  public class IconRefresh : ModulePatch
  {
    static readonly HashSet<int> Refreshed = new HashSet<int>();

    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(ItemIconCreator), nameof(ItemIconCreator.GetItemIcon));

    [PatchPrefix]
    static void Prefix(Item item, ref bool forcedGeneration)
    {
      if (item == null || (item.StringTemplateId != KeymasterIds.CaseTpl && item.StringTemplateId != KeymasterIds.VaultTpl)) return;
      var hash = IconsHash.GetItemHash(item);
      if (Refreshed.Contains(hash)) return;
      forcedGeneration = true;
      if (ShaderRebind.Resolved || ShaderRebind.Settled) Refreshed.Add(hash);
    }
  }
}
