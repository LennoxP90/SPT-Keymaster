using Keymaster.Shared;
using Xunit;

public class DropPlanTests
{
  static readonly string[] Wanted = { "map", "always" };
  static string?[] Pickups(params string?[] t) => t;

  [Fact]
  public void Money_GoesToItsCurrencyCell()
  {
    var c = DropPlan.ForCase("usd", false, 1, new string[0], Pickups(), Wanted);
    Assert.Equal(DropSection.Money, c.Section);
    Assert.Equal(1, c.Index);
  }

  [Fact]
  public void NonKeyNonMoney_IsRefused() =>
    Assert.Equal(DropSection.None, DropPlan.ForCase("gun", false, -1, new string[0], Pickups(), Wanted).Section);

  [Fact]
  public void WantedKey_LandsOnTheTrailingKeysSlot()
  {
    var c = DropPlan.ForCase("map", true, -1, new[] { "always" }, Pickups(), Wanted);
    Assert.Equal(DropSection.Keys, c.Section);
    Assert.Equal(1, c.Index);
  }

  [Fact]
  public void WantedKey_AlreadyShown_LandsOnItsMergedView()
  {
    var c = DropPlan.ForCase("map", true, -1, new[] { "always", "map" }, Pickups(), Wanted);
    Assert.Equal(1, c.Index);
  }

  [Fact]
  public void OtherKey_LandsOnTheFirstFreePickupCell()
  {
    var c = DropPlan.ForCase("found", true, -1, new string[0], Pickups("a", null, null), Wanted);
    Assert.Equal(DropSection.Pickups, c.Section);
    Assert.Equal(1, c.Index);
  }

  [Fact]
  public void OtherKey_SameTemplate_LandsOnItsPickupCell()
  {
    var c = DropPlan.ForCase("b", true, -1, new string[0], Pickups("a", "b", null), Wanted);
    Assert.Equal(1, c.Index);
  }

  [Fact]
  public void OtherKey_PickupsFull_IsRefused()
  {
    var full = Enumerable.Range(0, CaseLayout.PickupSlots).Select(i => (string?)("t" + i)).ToArray();
    Assert.Equal(DropSection.None, DropPlan.ForCase("new", true, -1, new string[0], full, Wanted).Section);
    Assert.Equal(DropSection.Pickups, DropPlan.ForCase("t4", true, -1, new string[0], full, Wanted).Section);
  }

  [Fact]
  public void Vault_KeyGoesToItsGroup_OrTheEndSlot()
  {
    Assert.Equal(1, DropPlan.ForVault("Customs", new[] { "Factory", "Customs" }).Group);
    Assert.Equal(2, DropPlan.ForVault("Woods", new[] { "Factory", "Customs" }).Group);
    Assert.Equal(0, DropPlan.ForVault("Woods", new string[0]).Group);
  }

  [Fact]
  public void TrailingSlot_IsOneEmptyCellAfterTheItems()
  {
    Assert.Equal(new string?[] { null }, DropPlan.WithTrailingSlot(new string[0]));
    Assert.Equal(new string?[] { "a", "b", null }, DropPlan.WithTrailingSlot(new[] { "a", "b" }));
  }
}
