using System.Collections.Generic;

namespace Keymaster.Shared
{
  public static class VaultHolders
  {
    /// <summary>True when the profile may receive a KM Vault: the gift and the web resend both ask this.</summary>
    public static bool NeedsVault(IEnumerable<string> heldTpls) => OneOfEach.MayReceive(heldTpls, new[] { KeymasterIds.VaultTpl });
  }
}
