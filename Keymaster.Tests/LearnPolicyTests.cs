using Keymaster.Shared;
using Xunit;

public class LearnPolicyTests
{
  [Fact] public void NullKeys_Ignored() => Assert.False(LearnPolicy.Replaces(new[] { "a" }, null));
  [Fact] public void EmptyKeys_Ignored() => Assert.False(LearnPolicy.Replaces(new[] { "a" }, new string[0]));
  [Fact] public void EmptyKeys_IgnoredEvenWithNoEntry() => Assert.False(LearnPolicy.Replaces(null, new string[0]));
  [Fact] public void SameKeysAnyOrder_NoChange() => Assert.False(LearnPolicy.Replaces(new[] { "a", "b" }, new[] { "b", "a" }));
  [Fact] public void NewEntry_Replaces() => Assert.True(LearnPolicy.Replaces(null, new[] { "a" }));
  [Fact] public void DifferentKeys_Replaces() => Assert.True(LearnPolicy.Replaces(new[] { "a" }, new[] { "a", "b" }));
}
