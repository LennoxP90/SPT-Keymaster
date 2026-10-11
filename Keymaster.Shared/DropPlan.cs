using System.Collections.Generic;
using System.Linq;

namespace Keymaster.Shared
{
  public enum DropSection { None, Keys, Pickups, Money, Vault }

  /// <summary>The panel cell a dragged or quick-moved item lands on. Vault: Group is the map group, or the group count for the end slot.</summary>
  public struct DropCell
  {
    public DropSection Section;
    public int Group;
    /// <summary>Cell position within the section (Money: the currency column).</summary>
    public int Index;
  }

  public static class DropPlan
  {
    /// <param name="currency">Index into KeymasterIds.Currencies, or -1 when the item is not money.</param>
    /// <param name="keysShown">The template of each KEYS cell, one per template, in display order.</param>
    /// <param name="pickupsShown">The template of each PICKUPS cell, null for an empty cell.</param>
    public static DropCell ForCase(string tpl, bool isKey, int currency, IReadOnlyList<string> keysShown,
      IReadOnlyList<string?> pickupsShown, IReadOnlyCollection<string> wanted)
    {
      if (currency >= 0) return new DropCell { Section = DropSection.Money, Index = currency };
      if (!isKey) return default;
      if (wanted.Contains(tpl))
      {
        var at = IndexOf(keysShown, tpl);
        return new DropCell { Section = DropSection.Keys, Index = at < 0 ? keysShown.Count : at };
      }
      var same = IndexOf(pickupsShown, tpl);
      if (same >= 0) return new DropCell { Section = DropSection.Pickups, Index = same };
      if (pickupsShown.Count(t => t != null) >= CaseLayout.PickupSlots) return default;
      var free = IndexOf(pickupsShown, null);
      return free < 0 ? default : new DropCell { Section = DropSection.Pickups, Index = free };
    }

    /// <summary>The trailing empty slot of the key's map group, or the end slot when its map has no group yet.</summary>
    public static DropCell ForVault(string title, IReadOnlyList<string> shownTitles)
    {
      var at = IndexOf(shownTitles, title);
      return new DropCell { Section = DropSection.Vault, Group = at < 0 ? shownTitles.Count : at };
    }

    /// <summary>A section's cells: its items, then one empty slot.</summary>
    public static List<T?> WithTrailingSlot<T>(IEnumerable<T> items) where T : class => items.Cast<T?>().Append(null).ToList();

    static int IndexOf<T>(IReadOnlyList<T> list, T value)
    {
      for (var i = 0; i < list.Count; i++) if (Equals(list[i], value)) return i;
      return -1;
    }
  }
}
