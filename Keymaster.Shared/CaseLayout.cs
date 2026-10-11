using System.Collections.Generic;
using System.Linq;

namespace Keymaster.Shared
{
  public sealed class CaseSections
  {
    /// <summary>Keys the current map or always-bring wants.</summary>
    public List<KeyCopy> Keys { get; } = new List<KeyCopy>();
    /// <summary>Every other key: found in a raid or dropped in by hand.</summary>
    public List<KeyCopy> Pickups { get; } = new List<KeyCopy>();
    /// <summary>A slot holds one template, however many copies.</summary>
    public int PickupSlotsUsed => Pickups.Select(c => c.Tpl).Distinct().Count();
  }

  /// <summary>The case window's KEYS and PICKUPS sections.</summary>
  public static class CaseLayout
  {
    public const int PickupSlots = 10;

    public static CaseSections Split(IReadOnlyList<KeyCopy> caseCopies, IReadOnlyCollection<string> wantedTpls)
    {
      var s = new CaseSections();
      foreach (var c in caseCopies) (wantedTpls.Contains(c.Tpl) ? s.Keys : s.Pickups).Add(c);
      return s;
    }

    /// <summary>Wanted keys always fit; another key needs a free pickup slot or one already holding its template.</summary>
    public static bool Accepts(CaseSections s, string tpl, IReadOnlyCollection<string> wantedTpls) =>
      wantedTpls.Contains(tpl) || s.Pickups.Any(c => c.Tpl == tpl) || s.PickupSlotsUsed < PickupSlots;
  }

  /// <summary>One container on the way from a key up to the stash.</summary>
  public struct ContainerHop
  {
    public string OwnerTpl;
    public bool IsSlot;
  }

  public static class LoadSource
  {
    /// <summary>
    /// A key in a slot of another mod's key container (one slot per key, as Gilded Key Storage keychains have) stays there;
    /// keys in grids, nested or not, are sources. chain runs from the key's container up to, not including, the stash.
    /// </summary>
    public static bool IsLoadSource(IEnumerable<ContainerHop> chain) =>
      !chain.Any(h => h.IsSlot && h.OwnerTpl != KeymasterIds.CaseTpl && h.OwnerTpl != KeymasterIds.VaultTpl);
  }
}
