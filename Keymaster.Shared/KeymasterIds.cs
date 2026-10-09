namespace Keymaster.Shared
{
  /// <summary>Every id and fixed size the two halves must agree on.</summary>
  public static class KeymasterIds
  {
    public const string CaseTpl = "6707ed1c4b4d4b6d00000001";
    public const string VaultTpl = "6707ed1c4b4d4b6d00000002";
    public const string SiccTpl = "5d235bb686f77443f4331278";
    public const string KeyToolTpl = "59fafd4b86f7745ca07e1232";
    public const string TherapistId = "54cb57776803fa99248b456e";
    public const string KeyCategory = "543be5e94bdc2df1348b4568";
    public const string StashClass = "566abbb64bdc2d144c8b457d";

    public const int KeyGridWidth = 10;
    public const int KeyGridHeight = 200;

    public const string Rub = "5449016a4bdc2d6f028b456f";
    public const string Usd = "5696686a4bdc2da3298b456a";
    public const string Eur = "569668774bdc2da2298b4568";
    public const string Gp = "5d235b4d86f7742e017bc88a";

    /// <summary>Case grid order after the key grid. Index i is grid i + 1.</summary>
    public static readonly string[] Currencies = { Rub, Usd, Eur, Gp };

    public static int StackCap(string currencyTpl)
    {
      switch (currencyTpl)
      {
        case Rub: return 1000000;
        case Usd: return 50000;
        case Eur: return 50000;
        case Gp: return 100;
        default: return 0;
      }
    }
  }
}
