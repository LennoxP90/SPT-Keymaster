using Keymaster.Shared;
using Xunit;

public class ScrollBarMathTests
{
  [Fact] public void Fits_FullThumbNoBar() { Assert.Equal(1f, ScrollBarMath.Size(504, 300)); Assert.False(ScrollBarMath.Needed(504, 504)); }
  [Fact] public void Taller_ThumbIsViewShare() { Assert.Equal(0.25f, ScrollBarMath.Size(100, 400)); Assert.True(ScrollBarMath.Needed(100, 400)); }
  [Fact] public void Position_EndsAndMiddle()
  {
    Assert.Equal(0f, ScrollBarMath.Position(0, 4));
    Assert.Equal(0.5f, ScrollBarMath.Position(2, 4));
    Assert.Equal(1f, ScrollBarMath.Position(4, 4));
    Assert.Equal(0f, ScrollBarMath.Position(0, 0));
  }
  [Fact] public void FirstFor_RoundsAndClamps()
  {
    Assert.Equal(2, ScrollBarMath.FirstFor(0.45f, 4));
    Assert.Equal(4, ScrollBarMath.FirstFor(1.2f, 4));
    Assert.Equal(0, ScrollBarMath.FirstFor(-0.1f, 4));
    Assert.Equal(0, ScrollBarMath.FirstFor(0.7f, 0));
  }
  [Fact] public void RoundTrip() { for (var f = 0; f <= 7; f++) Assert.Equal(f, ScrollBarMath.FirstFor(ScrollBarMath.Position(f, 7), 7)); }
}
