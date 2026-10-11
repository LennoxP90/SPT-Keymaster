using Keymaster.Shared;
using Xunit;

public class InventoryTrustTests
{
  [Fact]
  public void Trusted_WhenJsonDefaultsSet() => Assert.Null(InventoryTrust.StandDown(jsonDefaultsSet: true));

  [Fact]
  public void StandsDown_WhenJsonDefaultsMissing_AndNamesWhereToLook()
  {
    var warn = InventoryTrust.StandDown(jsonDefaultsSet: false);
    Assert.Contains("DefaultSettings", warn);
    Assert.Contains("Player.log", warn);
  }
}
