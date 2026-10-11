using Keymaster.Shared;
using Xunit;

public class CaseLayoutTests
{
  static KeyCopy C(string id, string tpl) => new KeyCopy { ItemId = id, Tpl = tpl, UsesLeft = 1, Where = KeyHome.Case };

  [Fact]
  public void Split_WantedKeysInKeys_OthersInPickups()
  {
    var s = CaseLayout.Split(new[] { C("1", "map"), C("2", "found"), C("3", "always"), C("4", "map") }, new[] { "map", "always" });
    Assert.Equal(new[] { "1", "3", "4" }, s.Keys.Select(c => c.ItemId));
    Assert.Equal(new[] { "2" }, s.Pickups.Select(c => c.ItemId));
  }

  [Fact]
  public void PickupSlots_CountTemplatesNotCopies()
  {
    var s = CaseLayout.Split(new[] { C("1", "a"), C("2", "a"), C("3", "b") }, new string[0]);
    Assert.Equal(2, s.PickupSlotsUsed);
  }

  static CaseSections Full() =>
    CaseLayout.Split(Enumerable.Range(0, CaseLayout.PickupSlots).Select(i => C("p" + i, "t" + i)).ToList(), new[] { "map" });

  [Fact]
  public void Accepts_RefusesANewPickupWhenAllTenSlotsAreTaken()
  {
    Assert.Equal(10, CaseLayout.PickupSlots);
    Assert.False(CaseLayout.Accepts(Full(), "new", new[] { "map" }));
  }

  [Fact]
  public void Accepts_MapKeyAndExistingPickupTemplate_EvenWhenFull()
  {
    Assert.True(CaseLayout.Accepts(Full(), "map", new[] { "map" }));
    Assert.True(CaseLayout.Accepts(Full(), "t3", new[] { "map" }));
  }

  [Fact]
  public void Accepts_NewPickupWithAFreeSlot()
  {
    var s = CaseLayout.Split(new[] { C("1", "a") }, new string[0]);
    Assert.True(CaseLayout.Accepts(s, "b", new string[0]));
  }
}

public class LoadSourceTests
{
  static ContainerHop Slot(string owner) => new ContainerHop { OwnerTpl = owner, IsSlot = true };
  static ContainerHop Grid(string owner) => new ContainerHop { OwnerTpl = owner, IsSlot = false };

  [Fact]
  public void KeyInASlotOfAnotherModsKeychain_IsNotASource()
  {
    Assert.False(LoadSource.IsLoadSource(new[] { Slot("keychain") }));
    Assert.False(LoadSource.IsLoadSource(new[] { Slot("keychain"), Grid("docs") }));
  }

  [Fact]
  public void KeyInAGridOfAnOrdinaryContainer_OrLooseInTheStash_IsASource()
  {
    Assert.True(LoadSource.IsLoadSource(new[] { Grid("docs") }));
    Assert.True(LoadSource.IsLoadSource(new ContainerHop[0]));
  }

  [Fact]
  public void KeymasterItems_AreAlwaysSources()
  {
    Assert.True(LoadSource.IsLoadSource(new[] { Slot(KeymasterIds.VaultTpl) }));
    Assert.True(LoadSource.IsLoadSource(new[] { Slot(KeymasterIds.CaseTpl) }));
  }
}
