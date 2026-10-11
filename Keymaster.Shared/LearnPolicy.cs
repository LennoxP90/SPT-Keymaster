using System.Collections.Generic;
using System.Linq;

namespace Keymaster.Shared
{
  public static class LearnPolicy
  {
    /// <summary>Whether a learn replaces a map's stored list: never for an empty scan (a scene that read nothing must
    /// not wipe a map), and not when the keys are the same set.</summary>
    public static bool Replaces(IEnumerable<string>? before, IReadOnlyCollection<string>? incoming)
    {
      if (incoming == null || incoming.Count == 0) return false;
      return before == null || !new HashSet<string>(before).SetEquals(incoming);
    }
  }
}
