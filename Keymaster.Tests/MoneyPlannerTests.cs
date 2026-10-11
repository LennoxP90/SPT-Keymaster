using Keymaster.Shared;
using Xunit;

public class MoneyPlannerTests
{
  static StackRef S(string id, int n) => new StackRef { ItemId = id, Count = n };

  [Fact]
  public void TopUp_TakesSmallestStacksFirst()
  {
    var p = MoneyPlanner.Plan(KeymasterIds.Rub, 8000, 1000, new[] { S("big", 50000), S("small", 2000) });
    Assert.Equal(new[] { ("small", 2000), ("big", 5000) }, p.TopUp.Select(m => (m.ItemId, m.Count)));
    Assert.Equal(0, p.Excess);
  }

  [Fact]
  public void TopUp_PartialWhenStashShort()
  {
    var p = MoneyPlanner.Plan(KeymasterIds.Rub, 8000, 0, new[] { S("a", 3000) });
    Assert.Equal(3000, p.TopUp.Sum(m => m.Count));
    Assert.Equal(5000, p.Shortfall);
  }

  [Fact]
  public void Excess_AboveTarget()
  {
    var p = MoneyPlanner.Plan(KeymasterIds.Eur, 1000, 4200, new StackRef[0]);
    Assert.Empty(p.TopUp);
    Assert.Equal(3200, p.Excess);
  }

  [Fact]
  public void TargetClampedToOneStack()
  {
    var p = MoneyPlanner.Plan(KeymasterIds.Gp, 500, 0, new[] { S("g", 400) });
    Assert.Equal(100, p.TopUp.Sum(m => m.Count));
  }

  [Fact]
  public void ZeroTarget_EmptiesCell()
  {
    Assert.Equal(700, MoneyPlanner.Plan(KeymasterIds.Usd, 0, 700, new StackRef[0]).Excess);
  }
}

public class MoneyTargetTests
{
  [Fact]
  public void ClampTarget_KeepsWithinOneStack()
  {
    Assert.Equal(1000000, MoneyPlanner.ClampTarget(KeymasterIds.Rub, 5000000));
    Assert.Equal(100, MoneyPlanner.ClampTarget(KeymasterIds.Gp, 101));
    Assert.Equal(0, MoneyPlanner.ClampTarget(KeymasterIds.Usd, -5));
    Assert.Equal(8000, MoneyPlanner.ClampTarget(KeymasterIds.Rub, 8000));
  }
}
