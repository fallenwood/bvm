namespace Bvm.Models;

using System;
using System.Text.Json.Serialization;

public sealed class Asset {
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  [JsonPropertyName("browser_download_url")]
  public string BrowserDownloadUrl { get; set; } = "";

  [JsonPropertyName("created_at")]
  public DateTime CreatedAt { get; set; }

  [JsonPropertyName("updated_at")]
  public DateTime UpdatedAt { get; set; }

  [JsonPropertyName("size")]
  public ulong Size { get; set; }

  [JsonPropertyName("content_type")]
  public string ContentType { get; set; } = "";

  [JsonPropertyName("state")]
  public string State { get; set; } = "";

  [JsonIgnore]
  public Release Release { get; set; } = default!;
}
