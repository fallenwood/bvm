namespace Bvm;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
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
  IDownloadClient downloadClient,
  Platform platform)
  : IDownloadManager {
  private const string GithubReleaseUri = "https://api.github.com/repos/{0}/{1}/releases?page={2}&per_page={3}";
  private const string NodeDistUri = "{0}/dist";
  private const string NodeExeUri = "{0}/dist/{1}/{2}";
  private const string NodeJsOrg = "https://nodejs.org/";

  private const string NodeWindowsAmd64Archive = "node-{0}-win-x64.zip";
  private const string NodeLinuxAmd64Archive = "node-{0}-linux-x64.tar.gz";
  private const string NodeLinuxAarch64Archive = "node-{0}-linux-arm64.tar.gz";
  private const string NodeMacAmd64Archive = "node-{0}-darwin-x64.tar.gz ";

  private const string BunWindowsAmd64Archive = "bun-windows-x64.zip";
  private const string BunLinuxAmd64Archive = "bun-linux-x64.zip";
  private const string BunLinuxAarch64Archive = "bun-linux-aarch64.zip";
  private const string BunMacAmd64Archive = "bun-darwin-x64.zip";

  private const string DenoWindowsAmd64Archive = "deno-x86_64-pc-windows-msvc.zip";
  private const string DenoLinuxAmd64Archive = "deno-x86_64-unknown-linux-gnu.zip";
  private const string DenoLinuxAarch64Archive = "deno-aarch64-unknown-linux-gnu.zip";
  private const string DenoOsXAmd64Archive = "deno-x86_64-apple-darwin.zip";

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
    var (response, responseStream) = await downloadClient.GetAsyncWithProgress(uri);
    response.EnsureSuccessStatusCode();

    var fileName = uri.Split('/').Last();
    var filePath = Path.Combine(fileSystemManager.TmpPath, fileName);

    await using var fileStream = File.Create(filePath);
    await responseStream.CopyToAsync(fileStream);

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
      registry = NodeJsOrg;
    }

    var uri = string.Format(NodeDistUri, registry);

    var httpResponse = await downloadClient.GetAsync(uri);
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
          Platform.WindowsAmd64 => string.Format(NodeWindowsAmd64Archive, tag),
          Platform.LinuxAmd64 => string.Format(NodeLinuxAmd64Archive, tag),
          Platform.LinuxAarch64 => string.Format(NodeLinuxAarch64Archive, tag),
          Platform.MacAmd64 => string.Format(NodeMacAmd64Archive, tag),
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

    var httpResponse = await downloadClient.GetAsync(uri);
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
        Platform.WindowsAmd64 => string.Equals(name, BunWindowsAmd64Archive, StringComparison.OrdinalIgnoreCase),
        Platform.LinuxAmd64 => string.Equals(name, BunLinuxAmd64Archive, StringComparison.OrdinalIgnoreCase),
        Platform.LinuxAarch64 => string.Equals(name, BunLinuxAarch64Archive, StringComparison.OrdinalIgnoreCase),
        Platform.MacAmd64 => string.Equals(name, BunMacAmd64Archive, StringComparison.OrdinalIgnoreCase),
        _ => false,
      },
      Distribution.Deno => platform switch {
        Platform.WindowsAmd64 => string.Equals(name, DenoWindowsAmd64Archive, StringComparison.OrdinalIgnoreCase),
        Platform.LinuxAmd64 => string.Equals(name, DenoLinuxAmd64Archive, StringComparison.OrdinalIgnoreCase),
        Platform.LinuxAarch64 => string.Equals(name, DenoLinuxAarch64Archive, StringComparison.OrdinalIgnoreCase),
        Platform.MacAmd64 => string.Equals(name, DenoOsXAmd64Archive, StringComparison.OrdinalIgnoreCase),
        _ => false,
      },
      _ => throw new InvalidDistributionException(distribution!),
    };
  }
}
