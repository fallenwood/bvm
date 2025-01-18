namespace Bvm.Models;

using System;
using System.Text.Json.Serialization;

public sealed class ReleasesResponse {
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  [JsonPropertyName("tag_name")]
  public string TagName { get; set; } = "";

  [JsonPropertyName("prerelease")]
  public bool Prerelease { get; set; }

  [JsonPropertyName("draft")]
  public bool Draft { get; set; }

  [JsonPropertyName("assets")]
  public Asset[] Assets { get; set; } = [];

  [JsonPropertyName("created_at")]
  public DateTime CreatedAt { get; set; }

  [JsonPropertyName("published_at")]
  public DateTime PublishedAt { get; set; }
}
