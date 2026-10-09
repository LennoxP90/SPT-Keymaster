using System.Collections.Generic;

namespace Keymaster.Shared
{
  /// <summary>Shape of db/keys-seed.json and data/keys-learned.json. Entry key: KeyTable.EntryKey.</summary>
  public sealed class KeysFile
  {
    public int Version { get; set; } = 1;
    public Dictionary<string, KeysEntry> Entries { get; set; } = new Dictionary<string, KeysEntry>();
  }

  public sealed class KeysEntry
  {
    public string EftBuild { get; set; } = "";
    public List<string> Keys { get; set; } = new List<string>();
  }
}
