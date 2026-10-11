using System;

namespace Keymaster.Shared
{
  /// <summary>The panel scrolls by whole rows: the bar maps first row 0..last to a 0..1 position.</summary>
  public static class ScrollBarMath
  {
    /// <summary>Thumb size: the share of the content the viewport shows, 1 when it all fits.</summary>
    public static float Size(float view, float content) => content <= view || content <= 0 ? 1f : view / content;

    public static bool Needed(float view, float content) => content > view + 0.5f;

    /// <summary>0 at the top row, 1 at the last first row.</summary>
    public static float Position(int first, int last) => last <= 0 ? 0f : Math.Min(1f, Math.Max(0f, (float)first / last));

    public static int FirstFor(float position, int last) =>
      last <= 0 ? 0 : Math.Min(last, Math.Max(0, (int)Math.Round(position * last)));
  }
}
