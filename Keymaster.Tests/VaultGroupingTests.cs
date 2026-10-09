using Keymaster.Shared;
using Xunit;

public class VaultGroupingTests
{
  [Fact]
  public void Groups_ByFirstMap_InTableOrder_WithOtherLast()
  {
    var table = new Dictionary<string, List<string>>
    {
      ["bigmap"] = new() { "k1", "shared" },
      ["woods"] = new() { "k2", "shared" },
    };
    var groups = VaultGrouping.Group(new[] { "k2", "shared", "loose", "k1" }, table);
    Assert.Equal(new[] { "bigmap", "woods", "Other" }, groups.Select(g => g.Title));
    Assert.Equal(new[] { "k1", "shared" }, groups[0].Tpls);
    Assert.Equal(new[] { "k2" }, groups[1].Tpls);
    Assert.Equal(new[] { "loose" }, groups[2].Tpls);
  }

  [Fact]
  public void EmptyGroupsAreOmitted()
  {
    var groups = VaultGrouping.Group(new[] { "x" }, new Dictionary<string, List<string>> { ["woods"] = new() { "k" } });
    Assert.Equal(new[] { "Other" }, groups.Select(g => g.Title));
  }
}
