using System.Collections.Generic;
using System.Linq;

namespace Keymaster.Shared
{
  public static class ReturnPlanner
  {
    /// <summary>Item ids leaving the case: keys not wanted, and every copy of a wanted key except the chosen one.</summary>
    public static List<string> Plan(IReadOnlyCollection<string> wantedTpls, IReadOnlyList<KeyCopy> caseCopies, CopyChoice choice)
    {
      var wanted = new HashSet<string>(wantedTpls);
      var home = new List<string>();
      foreach (var group in caseCopies.GroupBy(c => c.Tpl))
      {
        if (!wanted.Contains(group.Key)) { home.AddRange(group.Select(c => c.ItemId)); continue; }
        var keep = LoadPlanner.Pick(group, choice);
        home.AddRange(group.Where(c => c.ItemId != keep.ItemId).Select(c => c.ItemId));
      }
      return home;
    }
  }
}
