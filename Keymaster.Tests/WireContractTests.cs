using Xunit;

public class WireContractTests
{
  [Fact]
  public void State_RoundTripsServerToClient()
  {
    var s = new Keymaster.Server.Wire.StateDto { CurrentMap = "bigmap", AlwaysBring = new() { "mk" } };
    var json = System.Text.Json.JsonSerializer.Serialize(s);
    var c = Newtonsoft.Json.JsonConvert.DeserializeObject<Keymaster.Client.Wire.StateDtoC>(json)!;
    Assert.Equal("bigmap", c.CurrentMap);
    Assert.Equal(new[] { "mk" }, c.AlwaysBring);
  }

  [Fact]
  public void Learn_RoundTripsClientToServer()
  {
    var c = new Keymaster.Client.Wire.LearnRequestC { Location = "woods", Variant = null, EftBuild = "0.16.9.40087", Keys = new() { "k" } };
    var json = Newtonsoft.Json.JsonConvert.SerializeObject(c);
    var s = System.Text.Json.JsonSerializer.Deserialize<Keymaster.Server.Wire.LearnRequest>(json)!;
    Assert.Equal("woods", s.Location);
    Assert.Equal("0.16.9.40087", s.EftBuild);
    Assert.Equal(new[] { "k" }, s.Keys);
  }

  [Fact]
  public void Config_RoundTripsServerToClient()
  {
    var s = new Keymaster.Server.Wire.ConfigResponse { Table = new() { ["woods"] = new() { "k" } }, Homes = new() { ["k"] = "woods" } };
    var c = Newtonsoft.Json.JsonConvert.DeserializeObject<Keymaster.Client.Wire.ConfigResponseC>(System.Text.Json.JsonSerializer.Serialize(s))!;
    Assert.Equal(new[] { "k" }, c.Table["woods"]);
    Assert.Equal("woods", c.Homes["k"]);
  }
}
