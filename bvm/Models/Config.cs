namespace Bvm.Models;

public static class Distribution {
  public const string Bun = "bun";
  public const string Deno = "deno";
  public const string Node = "node";
}

public class Config {
  public const string ProxyKey = "proxy";
  public const string BunVersionKey = "bun_version";
  public const string DenoVersionKey = "deno_version";
  public const string NodeRegistryKey = "node_registry";
  public const string NpmRegistryKey = "npm_registry";
  public const string NodeVersionKey = "node_version";

  public string Proxy { get; set; } = "";

  public string BunVersion { get; set; } = "";

  public string DenoVersion { get; set; } = "";

  public string NodeRegistry { get; set; } = "";

  public string NpmRegistry { get; set; } = "";

  public string NodeVersion { get; set; } = "";
}
