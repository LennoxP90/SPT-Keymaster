using Keymaster.Shared;
using Xunit;

public class ReturnPlannerTests
{
  static KeyCopy C(string id, string tpl, int uses) => new KeyCopy { ItemId = id, Tpl = tpl, UsesLeft = uses, Where = KeyHome.Case };

  [Fact]
  public void UnwantedKeysGoHome()
  {
    Assert.Equal(new[] { "c2" }, ReturnPlanner.Plan(new[] { "a" }, new[] { C("c1", "a", 3), C("c2", "x", 3) }, CopyChoice.Freshest));
  }

  [Fact]
  public void DuplicatesBeyondOneGoHome_KeepingTheChosenCopy()
  {
    var home = ReturnPlanner.Plan(new[] { "a" }, new[] { C("c1", "a", 2), C("c2", "a", 8), C("c3", "a", 5) }, CopyChoice.Freshest);
    Assert.Equal(new[] { "c1", "c3" }, home.OrderBy(x => x));
  }

  [Fact]
  public void NothingToReturn()
  {
    Assert.Empty(ReturnPlanner.Plan(new[] { "a" }, new[] { C("c1", "a", 2) }, CopyChoice.Freshest));
  }
}
