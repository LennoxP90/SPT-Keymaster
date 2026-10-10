using Keymaster.Shared;
using Xunit;

public class VaultGroupingTests
{
  static Dictionary<string, string> Homes(Dictionary<string, List<string>> table)
  {
    var homes = new Dictionary<string, string>();
    foreach (var e in table) foreach (var t in e.Value) homes.TryAdd(t, e.Key);
    return homes;
  }

  [Fact]
  public void Groups_ByFirstMap_InTableOrder_WithOtherLast()
  {
    var table = new Dictionary<string, List<string>>
    {
      ["bigmap"] = new() { "k1", "shared" },
      ["woods"] = new() { "k2", "shared" },
    };
    var groups = VaultGrouping.Group(new[] { "k2", "shared", "loose", "k1" }, table, Homes(table));
    Assert.Equal(new[] { "bigmap", "woods", "Other" }, groups.Select(g => g.Title));
    Assert.Equal(new[] { "k1", "shared" }, groups[0].Tpls);
    Assert.Equal(new[] { "k2" }, groups[1].Tpls);
    Assert.Equal(new[] { "loose" }, groups[2].Tpls);
  }

  [Fact]
  public void EmptyGroupsAreOmitted()
  {
    var groups = VaultGrouping.Group(new[] { "x" }, new Dictionary<string, List<string>> { ["woods"] = new() { "k" } }, new Dictionary<string, string>());
    Assert.Equal(new[] { "Other" }, groups.Select(g => g.Title));
  }

  [Fact]
  public void ATemplateListedTwiceInOneEntryAppearsOnce()
  {
    var groups = VaultGrouping.Group(new[] { "k" }, new Dictionary<string, List<string>> { ["woods"] = new() { "k", "k" } }, new Dictionary<string, string> { ["k"] = "woods" });
    Assert.Equal(new[] { "k" }, groups.Single().Tpls);
  }

  [Fact]
  public void Group_UsesHomes_LabsCardUnderLaboratory()
  {
    var table = new Dictionary<string, List<string>> { ["tarkovstreets"] = new() { "door", "labs" }, ["laboratory"] = new() { "labs" } };
    var homes = new Dictionary<string, string> { ["door"] = "tarkovstreets", ["labs"] = "laboratory" };
    var groups = VaultGrouping.Group(new[] { "labs", "door", "stray" }, table, homes);
    Assert.Equal(new[] { "tarkovstreets", "laboratory", "Other" }, groups.Select(g => g.Title));
    Assert.Equal(new[] { "door" }, groups[0].Tpls);
    Assert.Equal(new[] { "labs" }, groups[1].Tpls);
  }

  [Fact]
  public void Group_DuplicateTplInTableRendersOnce()
  {
    var table = new Dictionary<string, List<string>> { ["a"] = new() { "k" }, ["b"] = new() { "k" } };
    var groups = VaultGrouping.Group(new[] { "k", "k" }, table, new Dictionary<string, string> { ["k"] = "a" });
    Assert.Equal(1, groups.Sum(g => g.Tpls.Count));
  }

  [Fact]
  public void Group_HomeWithoutTableEntryGetsItsOwnGroupBeforeOther()
  {
    var table = new Dictionary<string, List<string>> { ["tarkovstreets"] = new() { "door", "labs" } };
    var homes = new Dictionary<string, string> { ["door"] = "tarkovstreets", ["labs"] = "laboratory" };
    var groups = VaultGrouping.Group(new[] { "labs", "stray", "door" }, table, homes);
    Assert.Equal(new[] { "tarkovstreets", "laboratory", "Other" }, groups.Select(g => g.Title));
    Assert.Equal(new[] { "labs" }, groups[1].Tpls);
    Assert.Equal(3, groups.Sum(g => g.Tpls.Count));
  }
}
