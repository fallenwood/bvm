namespace Bvm;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Bvm.Models;

[JsonSerializable(typeof(ReleasesResponse[]))]
[JsonSerializable(typeof(List<ReleasesResponse>))]
public partial class AppJsonContext : JsonSerializerContext {
}
