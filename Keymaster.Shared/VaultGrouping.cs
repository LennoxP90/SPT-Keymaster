using System.Collections.Generic;
using System.Linq;

namespace Keymaster.Shared
{
  public sealed class VaultGroup
  {
    public string Title { get; set; } = "";
    public List<string> Tpls { get; set; } = new List<string>();
  }

  public static class VaultGrouping
  {
    public const string Other = "Other";

    /// <summary>Each template under the first table entry that lists it, in table order; within a group, table order.</summary>
    public static List<VaultGroup> Group(IEnumerable<string> tpls, IReadOnlyDictionary<string, List<string>> table)
    {
      var list = tpls.ToList();
      var remaining = new HashSet<string>(list);
      var groups = new List<VaultGroup>();
      foreach (var entry in table)
      {
        var mine = entry.Value.Where(remaining.Contains).ToList();
        foreach (var t in mine) remaining.Remove(t);
        if (mine.Count > 0) groups.Add(new VaultGroup { Title = entry.Key, Tpls = mine });
      }
      if (remaining.Count > 0) groups.Add(new VaultGroup { Title = Other, Tpls = list.Where(remaining.Contains).Distinct().ToList() });
      return groups;
    }
  }
}
