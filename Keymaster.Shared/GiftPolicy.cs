namespace Keymaster.Shared
{
  public static class GiftPolicy
  {
    public const long StorageSeconds = 30L * 24 * 3600;

    public static bool ShouldSend(bool hasVault, long? sentAtUnix, long nowUnix, long storageSeconds, bool resend)
    {
      if (hasVault) return false;
      if (sentAtUnix == null) return true;
      return resend && nowUnix > sentAtUnix.Value + storageSeconds;
    }
  }
}
