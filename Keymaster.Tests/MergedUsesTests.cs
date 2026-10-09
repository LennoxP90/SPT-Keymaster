using Keymaster.Shared;
using Xunit;

public class MergedUsesTests
{
  [Fact] public void Sums() => Assert.Equal(10, MergedUses.Sum(new[] { 1, 4, 5 }));
  [Fact] public void Merged_UnlimitedHidesCount() => Assert.Null(MergedUses.Sum(new[] { 3, -1 }));
}
