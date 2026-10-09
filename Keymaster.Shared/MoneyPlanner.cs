using System;
using System.Collections.Generic;
using System.Linq;

namespace Keymaster.Shared
{
  public sealed class StackRef
  {
    public string ItemId { get; set; } = "";
    public int Count { get; set; }
  }

  public sealed class MoneyMove
  {
    public string ItemId { get; set; } = "";
    public int Count { get; set; }
  }

  public sealed class MoneyPlan
  {
    public List<MoneyMove> TopUp { get; } = new List<MoneyMove>();
    /// <summary>Amount to move from the cell back to the stash.</summary>
    public int Excess { get; set; }
    /// <summary>How far short of the target the stash left the cell.</summary>
    public int Shortfall { get; set; }
  }

  public static class MoneyPlanner
  {
    public static MoneyPlan Plan(string currencyTpl, int target, int inCell, IReadOnlyList<StackRef> stashStacks)
    {
      var goal = Math.Max(0, Math.Min(target, KeymasterIds.StackCap(currencyTpl)));
      var plan = new MoneyPlan();
      if (inCell > goal) { plan.Excess = inCell - goal; return plan; }

      var need = goal - inCell;
      foreach (var s in stashStacks.OrderBy(s => s.Count))
      {
        if (need == 0) break;
        var take = Math.Min(need, s.Count);
        plan.TopUp.Add(new MoneyMove { ItemId = s.ItemId, Count = take });
        need -= take;
      }
      plan.Shortfall = need;
      return plan;
    }
  }
}
