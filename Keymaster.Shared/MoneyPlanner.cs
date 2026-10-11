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
    /// <summary>A target is 0 to the game's lobby limit.</summary>
    public static int ClampTarget(string currencyTpl, int target) => Math.Max(0, Math.Min(target, KeymasterIds.MoneyCap(currencyTpl)));

    /// <summary>Ctrl-click of a money stack into the case: the whole stack, or only the room left under the cap; 0 when full.</summary>
    public static int QuickTransferAmount(int cellCount, int stackCount, int cap, int carriedElsewhere = 0) =>
      Math.Max(0, Math.Min(stackCount, cap - cellCount - carriedElsewhere));

    /// <summary>carriedElsewhere: the currency already in the equipment outside the cell; the top-up never takes the
    /// total past the lobby limit.</summary>
    public static MoneyPlan Plan(string currencyTpl, int target, int inCell, IReadOnlyList<StackRef> stashStacks, int carriedElsewhere = 0)
    {
      var goal = Math.Min(ClampTarget(currencyTpl, target), Math.Max(0, KeymasterIds.MoneyCap(currencyTpl) - carriedElsewhere));
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
