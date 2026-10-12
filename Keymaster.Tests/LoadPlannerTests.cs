using System.Linq;
using Keymaster.Shared;
using Xunit;

public class LoadPlannerTests
{
  static KeyCopy C(string id, string tpl, int uses, KeyHome where) => new KeyCopy { ItemId = id, Tpl = tpl, UsesLeft = uses, Where = where };

  [Fact]
  public void LoadsOneCopyPerMissingKey_VaultFirst()
  {
    var copies = new[] { C("s1", "a", 10, KeyHome.Stash), C("v1", "a", 3, KeyHome.Vault) };
    var plan = LoadPlanner.Plan(new[] { "a" }, copies, CopyChoice.Freshest, mapChanged: false);
    Assert.Equal(new[] { "v1" }, plan.ToCase);
  }

  [Fact]
  public void Freshest_PrefersUnlimited()
  {
    var copies = new[] { C("v1", "a", 9, KeyHome.Vault), C("v2", "a", -1, KeyHome.Vault) };
    Assert.Equal(new[] { "v2" }, LoadPlanner.Plan(new[] { "a" }, copies, CopyChoice.Freshest, false).ToCase);
  }

  [Fact]
  public void MostWorn_PicksFewestUses_UnlimitedLast()
  {
    var copies = new[] { C("v1", "a", -1, KeyHome.Vault), C("v2", "a", 2, KeyHome.Vault), C("v3", "a", 5, KeyHome.Vault) };
    Assert.Equal(new[] { "v2" }, LoadPlanner.Plan(new[] { "a" }, copies, CopyChoice.MostWorn, false).ToCase);
  }

  [Fact]
  public void KeyAlreadyInCase_IsNotLoadedAgain()
  {
    var copies = new[] { C("c1", "a", 1, KeyHome.Case), C("v1", "a", 10, KeyHome.Vault) };
    var plan = LoadPlanner.Plan(new[] { "a" }, copies, CopyChoice.Freshest, false);
    Assert.Empty(plan.ToCase);
    Assert.Equal(1, plan.Present);
  }

  [Fact]
  public void MapChanged_SendsUnwantedCaseKeysHome()
  {
    var copies = new[] { C("c1", "old", 5, KeyHome.Case), C("c2", "a", 5, KeyHome.Case) };
    var plan = LoadPlanner.Plan(new[] { "a" }, copies, CopyChoice.Freshest, mapChanged: true);
    Assert.Equal(new[] { "c1" }, plan.ToHome);
  }

  [Fact]
  public void Transit_CustomsToFactory_SendsCustomsKeysHome_LoadsFactoryKeysAndLabsCard()
  {
    var copies = new[]
    {
      C("cust", "customsKey", 5, KeyHome.Case),
      C("fac", "factoryKey", 5, KeyHome.Vault),
      C("labs", "labsCard", 1, KeyHome.Vault),
    };
    var plan = LoadPlanner.Plan(new[] { "factoryKey", "labsCard" }, copies, CopyChoice.Freshest, mapChanged: true);
    Assert.Equal(new[] { "cust" }, plan.ToHome);
    Assert.Equal(new[] { "fac", "labs" }, plan.ToCase.OrderBy(x => x).ToArray());
  }

  [Fact]
  public void SameMap_LeavesUnwantedCaseKeys()
  {
    var copies = new[] { C("c1", "old", 5, KeyHome.Case) };
    Assert.Empty(LoadPlanner.Plan(new[] { "a" }, copies, CopyChoice.Freshest, mapChanged: false).ToHome);
  }

  [Fact]
  public void MissingKeysCount()
  {
    var plan = LoadPlanner.Plan(new[] { "a", "b", "c" }, new[] { C("v1", "a", 1, KeyHome.Vault) }, CopyChoice.Freshest, false);
    Assert.Equal(3, plan.Wanted);
    Assert.Equal(1, plan.Present + plan.ToCase.Count);
  }
}
