namespace Keymaster.Shared
{
  public enum KeyHome { Case, Vault, Stash }

  public enum CopyChoice { Freshest, MostWorn }

  public sealed class KeyCopy
  {
    public string ItemId { get; set; } = "";
    public string Tpl { get; set; } = "";
    /// <summary>Uses left; -1 means unlimited (MaximumNumberOfUsage 0).</summary>
    public int UsesLeft { get; set; }
    public KeyHome Where { get; set; }
  }
}
