using System.Collections.Generic;
using System.Linq;

namespace Keymaster.Shared
{
  public sealed class LoadPlan
  {
    public List<string> ToCase { get; } = new List<string>();
    public List<string> ToHome { get; } = new List<string>();
    public int Wanted { get; set; }
    /// <summary>Wanted keys already in the case before the plan runs.</summary>
    public int Present { get; set; }
  }

  public static class LoadPlanner
  {
    public static LoadPlan Plan(IReadOnlyCollection<string> wantedTpls, IReadOnlyList<KeyCopy> copies, CopyChoice choice, bool mapChanged)
    {
      var wanted = new HashSet<string>(wantedTpls);
      var plan = new LoadPlan { Wanted = wanted.Count };
      var inCase = copies.Where(c => c.Where == KeyHome.Case).ToList();

      if (mapChanged)
        plan.ToHome.AddRange(inCase.Where(c => !wanted.Contains(c.Tpl)).Select(c => c.ItemId));

      var caseTpls = new HashSet<string>(inCase.Select(c => c.Tpl));
      foreach (var tpl in wanted)
      {
        if (caseTpls.Contains(tpl)) { plan.Present++; continue; }
        var source = copies.Where(c => c.Tpl == tpl && c.Where == KeyHome.Vault).ToList();
        if (source.Count == 0) source = copies.Where(c => c.Tpl == tpl && c.Where == KeyHome.Stash).ToList();
        if (source.Count > 0) plan.ToCase.Add(Pick(source, choice).ItemId);
      }
      return plan;
    }

    /// <summary>Unlimited (-1) counts as the most uses left.</summary>
    public static KeyCopy Pick(IEnumerable<KeyCopy> copies, CopyChoice choice)
    {
      int Rank(KeyCopy c) => c.UsesLeft < 0 ? int.MaxValue : c.UsesLeft;
      return choice == CopyChoice.Freshest
        ? copies.OrderByDescending(Rank).First()
        : copies.OrderBy(Rank).First();
    }
  }
}
