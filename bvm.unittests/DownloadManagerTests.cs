namespace Bvm.Unittests;

using System;
using System.Collections.Generic;
using Bvm.Models;
using Xunit;

public class DownloadManagerTests {

  [Theory]
  [InlineData(Platform.WindowsAmd64, "bun-windows-x86.zip", Distribution.Bun, false)]
  [InlineData(Platform.WindowsAmd64, "bun-windows-x64.zip", Distribution.Bun, true)]
  [InlineData(Platform.WindowsAmd64, "bun-windows-x64.zip", Distribution.Deno, false)]
  [InlineData(Platform.WindowsAmd64, "deno-x86_64-pc-windows-msvc.zip", Distribution.Deno, true)]
  [InlineData(Platform.LinuxAmd64, "bun-linux-x86.zip", Distribution.Bun, false)]
  [InlineData(Platform.LinuxAmd64, "bun-linux-x64.zip", Distribution.Bun, true)]
  [InlineData(Platform.LinuxAmd64, "bun-linux-x64.zip", Distribution.Deno, false)]
  [InlineData(Platform.LinuxAmd64, "deno-x86_64-unknown-linux-gnu.zip", Distribution.Deno, true)]
  [InlineData(Platform.LinuxAarch64, "bun-linux-x86.zip", Distribution.Bun, false)]
  [InlineData(Platform.LinuxAarch64, "bun-linux-aarch64.zip", Distribution.Bun, true)]
  [InlineData(Platform.LinuxAarch64, "bun-linux-x64.zip", Distribution.Deno, false)]
  [InlineData(Platform.LinuxAarch64, "deno-aarch64-unknown-linux-gnu.zip", Distribution.Deno, true)]
  public void TestIsPlatformMatch(
    Platform platform,
    string tagName,
    string distribution,
    bool expected) {
    IVersionManagerHandler versionManagerHandler = distribution switch {
      Distribution.Bun => new BunVersionManager(),
      Distribution.Deno => new DenoVersionManager(),
      Distribution.Node => new NodeJSVersionManager(),
      _ => throw new InvalidDistributionException(distribution!),
    };

    var actual = versionManagerHandler.IsPlatformMatch(platform, tagName, distribution);

    Assert.Equal(expected, actual);
  }

  [Theory]
  [MemberData(nameof(this.ExtractReleaseFromResponseData))]
  public void TestExtractReleaseFromResponse(
    Platform platform,
    string distribution,
    ReleasesResponse[] responses,
    List<Release> expected) {
    IVersionManagerHandler versionManagerHandler = distribution switch {
      Distribution.Bun => new BunVersionManager(),
      Distribution.Deno => new DenoVersionManager(),
      Distribution.Node => new NodeJSVersionManager(),
      _ => throw new InvalidDistributionException(distribution!),
    };

    var actual = versionManagerHandler.ExtractReleaseFromResponse(platform, distribution, responses);

    Assert.Equal(expected.Count, actual.Count);

    for (var i = 0; i < expected.Count; i++) {
      Assert.Equal(expected[i].Name, actual[i].Name);
      Assert.Equal(expected[i].TagName, actual[i].TagName);
      Assert.Equal(expected[i].DownloadUrl, actual[i].DownloadUrl);
      Assert.Equal(expected[i].UpdatedAt, actual[i].UpdatedAt);
      Assert.Equal(expected[i].CreatedAt, actual[i].CreatedAt);
    }
  }

  public static List<object[]> ExtractReleaseFromResponseData = [
    [
      Platform.WindowsAmd64,
      Distribution.Bun,
      new ReleasesResponse[]{
        new ReleasesResponse {
          Name = "bun v1.0.0",
          TagName = "bun-v1.0.0",
          Assets = [
            new Asset {
              Name = "bun-windows-x86.zip",
            },
            new Asset {
              Name = "bun-windows-x64.zip",
              BrowserDownloadUrl = "http://test",
              UpdatedAt = DateTime.Parse("2025-01-17T15:35:19Z"),
              CreatedAt = DateTime.Parse("2025-01-17T15:35:19Z"),
            },
          ],
        },
      },
      new List<Release>{
        new(Name: "bun v1.0.0", TagName: "bun-v1.0.0", DownloadUrl: "http://test", DateTime.Parse("2025-01-17T15:35:19Z"), DateTime.Parse("2025-01-17T15:35:19Z")),
      },
    ]
  ];
}
