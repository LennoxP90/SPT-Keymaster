using Keymaster.Shared;
using Xunit;

public class AccessGateTests
{
  [Fact] public void UngatedMap_NeverBlocks() => Assert.False(AccessGate.MustBlock(0, 30, null, false));
  [Fact] public void GatedMap_NoCard_Blocks() => Assert.True(AccessGate.MustBlock(1, 30, null, false));
  [Fact] public void GatedMap_CardWorn_DoesNotBlock() => Assert.False(AccessGate.MustBlock(1, 30, null, true));
  [Fact] public void BelowMinLevel_NoCardNeeded() => Assert.False(AccessGate.MustBlock(1, 10, 20, false));
  [Fact] public void AtMinLevel_CardNeeded() => Assert.True(AccessGate.MustBlock(1, 20, 20, false));
}
