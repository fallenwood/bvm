namespace Bvm.Models;

using System;

public sealed record Release(
  string Name,
  string TagName,
  string DownloadUrl,
  DateTime CreatedAt,
  DateTime UpdatedAt) {
  public Release(string TagName)
    : this(TagName, TagName, "", DateTime.MinValue, DateTime.MinValue) { }
}
