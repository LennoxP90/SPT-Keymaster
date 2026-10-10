using Keymaster.Shared;
using Xunit;

public class KeyTableTests
{
  static KeysFile File(params (string key, string build, string[] tpls)[] e)
  {
    var f = new KeysFile { Version = 1 };
    foreach (var (key, build, tpls) in e) f.Entries[key] = new KeysEntry { EftBuild = build, Keys = tpls.ToList() };
    return f;
  }

  static KeyTableInput Input(KeysFile? seed, KeysFile? learned, params string[] known) => new KeyTableInput
  {
    Seed = seed, Learned = learned, KnownTpls = new HashSet<string>(known),
  };

  [Fact]
  public void EntryKey_JoinsVariant()
  {
    Assert.Equal("factory4_day", KeyTable.EntryKey("factory4_day", null));
    Assert.Equal("factory4_day@backport", KeyTable.EntryKey("Factory4_Day", "Backport"));
  }

  [Fact]
  public void Build_LearnedReplacesSeedEntry()
  {
    var seed = File(("bigmap", "0.16.9.40087", new[] { "a", "b" }));
    var learned = File(("bigmap", "0.16.1.35000", new[] { "c" }));
    var t = KeyTable.Build(Input(seed, learned, "a", "b", "c"));
    Assert.Equal(new[] { "c" }, t["bigmap"]);
  }

  [Fact]
  public void Build_DropsUnknownTemplates()
  {
    var t = KeyTable.Build(Input(File(("bigmap", "x", new[] { "a", "gone" })), null, "a"));
    Assert.Equal(new[] { "a" }, t["bigmap"]);
  }

  [Fact]
  public void Build_AddsTransitTargetAccessKeys()
  {
    var input = Input(File(("tarkovstreets", "x", new[] { "s1" })), null, "s1", "labsCard");
    input.TransitTargets["tarkovstreets"] = new List<string> { "laboratory" };
    input.AccessKeys["laboratory"] = new List<string> { "labsCard" };
    var t = KeyTable.Build(input);
    Assert.Equal(new[] { "s1", "labsCard" }, t["tarkovstreets"]);
  }

  [Fact]
  public void Build_TransitTargetsAreLocationNamesInAnyCase()
  {
    // SPT's Transit.Location is a mixed-case name ("Sandbox_high"); its Target is a hex id and is not used.
    var input = Input(File(("tarkovstreets", "x", new[] { "s1" })), null, "s1", "labsCard");
    input.TransitTargets["tarkovstreets"] = new List<string> { "Laboratory" };
    input.AccessKeys["laboratory"] = new List<string> { "labsCard" };
    Assert.Equal(new[] { "s1", "labsCard" }, KeyTable.Build(input)["tarkovstreets"]);
  }

  [Fact]
  public void Build_AddsOwnAccessKeys()
  {
    var input = Input(File(("laboratory", "x", new[] { "door" })), null, "door", "labsCard");
    input.AccessKeys["laboratory"] = new List<string> { "labsCard" };
    Assert.Equal(new[] { "door", "labsCard" }, KeyTable.Build(input)["laboratory"]);
  }

  [Fact]
  public void Build_VariantEntryGetsBaseTransitCards()
  {
    var input = Input(File(("tarkovstreets@backport", "x", new[] { "s1" })), null, "s1", "labsCard");
    input.TransitTargets["tarkovstreets"] = new List<string> { "laboratory" };
    input.AccessKeys["laboratory"] = new List<string> { "labsCard" };
    Assert.Contains("labsCard", KeyTable.Build(input)["tarkovstreets@backport"]);
  }

  [Fact]
  public void Resolve_VariantFallsBackToBase()
  {
    var t = new Dictionary<string, List<string>> { ["factory4_day"] = new() { "a" } };
    Assert.Equal(new[] { "a" }, KeyTable.Resolve(t, "factory4_day", "backport"));
  }

  [Fact]
  public void Resolve_BaseIgnoresVariantEntries()
  {
    var t = new Dictionary<string, List<string>> { ["factory4_day@backport"] = new() { "a" } };
    Assert.Empty(KeyTable.Resolve(t, "factory4_day", null));
  }

  [Fact]
  public void Resolve_UnknownLocationIsEmpty()
  {
    Assert.Empty(KeyTable.Resolve(new Dictionary<string, List<string>>(), "woods", null));
  }

  static KeyTableInput Streets() => new KeyTableInput
  {
    Seed = File(("tarkovstreets", "x", new[] { "door" }), ("laboratory", "x", new string[0])),
    KnownTpls = new HashSet<string> { "door", "labs", "streetsCard" },
    TransitTargets = { ["tarkovstreets"] = new List<string> { "Laboratory" } },
    AccessKeys = { ["laboratory"] = new List<string> { "labs" }, ["tarkovstreets"] = new List<string> { "streetsCard" } },
  };

  [Fact]
  public void Homes_TransitCardHomesAtItsOwnMap()
  {
    var input = Streets();
    Assert.Contains("labs", KeyTable.Build(input)["tarkovstreets"]);
    var homes = KeyTable.Homes(input);
    Assert.Equal("laboratory", homes["labs"]);
    Assert.Equal("tarkovstreets", homes["streetsCard"]);
    Assert.Equal("tarkovstreets", homes["door"]);
  }

  [Fact]
  public void Homes_LearnedDoorKeyWinsOverAccessKey()
  {
    var input = Streets();
    input.Learned = File(("tarkovstreets", "y", new[] { "labs" }));
    Assert.Equal("tarkovstreets", KeyTable.Homes(input)["labs"]);
  }

  [Fact]
  public void Homes_NoHomeIsAbsent()
  {
    var input = Streets();
    input.AccessKeys.Remove("laboratory");
    Assert.False(KeyTable.Homes(input).ContainsKey("labs"));
  }

  [Fact]
  public void Homes_AccessKeyWithoutEntryHomesAtItsLocation()
  {
    var input = Streets();
    input.Seed!.Entries.Remove("laboratory");
    Assert.Equal("laboratory", KeyTable.Homes(input)["labs"]);
  }
}
