namespace Bvm;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bvm.Models;

public interface IDownloadManager {
  public Task<List<Release>> RetriveBunReleasesAsync();
  public Task<List<Release>> RetriveDenoReleasesAsync();
  public Task<List<Release>> RetrieveNodejsReleasesAsync(string? registry);
  public Task<string> DownloadReleaseAsync(string distribution, string uri, IFileSystemManager fileSystemManager);
}

public sealed partial class DownloadManager(
  HttpClient httpClient,
  Platform platform)
  : IDownloadManager {
  private const string GithubReleaseUri = "https://api.github.com/repos/{0}/{1}/releases?page={2}&per_page={3}";
  private const string NodeDistUri = "{0}/dist";
  private const string NodeExeUri = "{0}/dist/{1}/{2}";
  private const string NodeWindowsAmd64Exe = "node-{0}-win-x64.zip";
  private const string NodeLinuxAmd64Exe = "node-{0}-linux-x64.tar.gz";
  private const string NodeLinuxAarch64Exe = "node-{0}-linux-arm64.tar.gz";

  private const string BunWindowsAmd64 = "bun-windows-x64.zip";
  private const string BunLinuxAmd64 = "bun-linux-x64.zip";
  private const string BunLinuxAarch64 = "bun-linux-aarch64.zip";
  private const string DenoWindowsAmd64 = "deno-x86_64-pc-windows-msvc.zip";
  private const string DenoLinuxAmd64 = "deno-x86_64-unknown-linux-gnu.zip";
  private const string DenoLinuxAarch64 = "deno-aarch64-unknown-linux-gnu.zip";

  [GeneratedRegex(@"<a href=""(v\d+\.\d+\.\d+)/"">(v\d+\.\d+\.\d+)/</a>\s+([\w|-]+\s+\d+:\d+)\s+-")]
  private static partial Regex NodeJsVersionRegex();

  public async Task<List<Release>> RetriveBunReleasesAsync() {
    var page = 1;
    var pageSize = 100;

    var releases = await this.RetriveBunReleasesAsync(page, pageSize);
    var tmpReleases = releases;

    while (tmpReleases.Count >= pageSize) {
      page++;
      tmpReleases = await this.RetriveBunReleasesAsync(page, pageSize);
      releases.AddRange(tmpReleases);
    }

    return releases;
  }


  public async Task<List<Release>> RetriveDenoReleasesAsync() {
    var page = 1;
    var pageSize = 100;

    var releases = await this.RetriveDenoReleasesAsync(page, pageSize);
    var tmpReleases = releases;

    while (tmpReleases.Count >= pageSize) {
      page++;
      tmpReleases = await this.RetriveDenoReleasesAsync(page, pageSize);
      releases.AddRange(tmpReleases);
    }

    return releases;
  }

  public async Task<string> DownloadReleaseAsync(string distribution, string uri, IFileSystemManager fileSystemManager) {
    var response = await httpClient.GetAsync(uri);
    response.EnsureSuccessStatusCode();

    var fileName = uri.Split('/').Last();
    var filePath = Path.Combine(fileSystemManager.TmpPath, fileName);

    await using var fileStream = File.Create(filePath);
    await response.Content.CopyToAsync(fileStream);

    if (distribution == Distribution.Deno) {
      // TODO: sha256sum
    }

    return filePath;
  }

  private async Task<List<Release>> RetriveBunReleasesAsync(int page, int pageSize) {
    var releases = await this.RetrieveGithubReleasesAsync(Distribution.Bun, "oven-sh", "bun", page, pageSize);
    return releases;
  }

  private async Task<List<Release>> RetriveDenoReleasesAsync(int page, int pageSize) {
    var releases = await this.RetrieveGithubReleasesAsync(Distribution.Deno, "denoland", "deno", page, pageSize);
    return releases;
  }

  public async Task<List<Release>> RetrieveNodejsReleasesAsync(
    string? registry) {
    if (string.IsNullOrWhiteSpace(registry)) {
      registry = "https://nodejs.org/";
    }

    var uri = string.Format(NodeDistUri, registry);

    var httpResponse = await httpClient.GetAsync(uri);
    httpResponse.EnsureSuccessStatusCode();

    var responseStream = await httpResponse.Content.ReadAsStreamAsync();
    using var streamReader = new StreamReader(responseStream);

    var releases = new List<Release>();

    while (true) {
      var line = await streamReader.ReadLineAsync();
      if (line == null) {
        break;
      }

      var match = NodeJsVersionRegex().Match(line);
      if (match.Success) {
        var tag = match.Groups[1].Value;
        var name = match.Groups[2].Value;

        var nodeExe = platform switch {
          Platform.WindowsAmd64 => string.Format(NodeWindowsAmd64Exe, tag),
          Platform.LinuxAmd64 => string.Format(NodeLinuxAmd64Exe, tag),
          Platform.LinuxAarch64 => string.Format(NodeLinuxAmd64Exe, tag),
          _ => throw new InvalidPlatformException(platform),
        };

        var downloadUri = string.Format(NodeExeUri, registry, tag, nodeExe);

        var timestamp = DateTime.Parse(match.Groups[3].Value);

        releases.Add(new Release(
          Name: name,
          TagName: tag,
          downloadUri,
          timestamp,
          timestamp));
      }
    }

    return releases;
  }

  private async Task<List<Release>> RetrieveGithubReleasesAsync(
    string distribution,
    string owner,
    string repo,
    int page,
    int pageSize) {
    var uri = string.Format(GithubReleaseUri, owner, repo, page, pageSize);

    var httpResponse = await httpClient.GetAsync(uri);
    httpResponse.EnsureSuccessStatusCode();

    var responseStream = await httpResponse.Content.ReadAsStreamAsync();

    var response = await JsonSerializer.DeserializeAsync(
      responseStream,
      AppJsonContext.Default.ReleasesResponseArray);

    var releases = this.ExtractReleaseFromResponse(distribution, response!);

    return releases;
  }

  internal List<Release> ExtractReleaseFromResponse(string distribution, ReleasesResponse[] response) {
    var releases = new List<Release>(response.Length);

    foreach (var release in response) {
      var asset = release.Assets.Where(a => this.IsPlatformMatch(name: a.Name, distribution: distribution)).ToList();
      if (asset.Count == 0) {
        continue;
      }

      Debug.Assert(asset.Count == 1);

      releases.Add(new Release(
        release.Name,
        release.TagName,
        asset[0].BrowserDownloadUrl,
        asset[0].CreatedAt,
        asset[0].UpdatedAt));
    }

    return releases;
  }

  internal bool IsPlatformMatch(string name, string distribution) {
    return distribution switch {
      Distribution.Bun => platform switch {
        Platform.WindowsAmd64 => string.Equals(name, BunWindowsAmd64, StringComparison.OrdinalIgnoreCase),
        Platform.LinuxAmd64 => string.Equals(name, BunLinuxAmd64, StringComparison.OrdinalIgnoreCase),
        Platform.LinuxAarch64 => string.Equals(name, BunLinuxAarch64, StringComparison.OrdinalIgnoreCase),
        _ => false,
      },
      Distribution.Deno => platform switch {
        Platform.WindowsAmd64 => string.Equals(name, DenoWindowsAmd64, StringComparison.OrdinalIgnoreCase),
        Platform.LinuxAmd64 => string.Equals(name, DenoLinuxAmd64, StringComparison.OrdinalIgnoreCase),
        Platform.LinuxAarch64 => string.Equals(name, DenoLinuxAarch64, StringComparison.OrdinalIgnoreCase),
        _ => false,
      },
      _ => throw new InvalidDistributionException(distribution!),
    };
  }
}
