namespace Bvm;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bvm.Models;

public interface IVersionManagerHandler {
  private const string GithubReleaseUri = "https://api.github.com/repos/{0}/{1}/releases?page={2}&per_page={3}";

  public Task<List<Release>> RetrieveReleasesAsync(
    IDownloadClient downloadClient,
    Platform platform,
    string? registry);

  public bool IsPlatformMatch(Platform platform, string name, string distribution);

  public string NormalizeTag(string tag);

  public string NormalizeDirectoryName(string tag);

  public List<Release> GetInstalledReleases(IFileSystemManager fileSystemManager);

  public void CopyOrLink(IFileSystemManager fileSystemManager, Platform platform, string tag, bool all = false);

  public void Remove(IFileSystemManager fileSystemManager, string tag);

  public async Task<List<Release>> RetrieveGithubReleasesAsync(
    IDownloadClient downloadClient,
    Platform platform,
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

    var releases = this.ExtractReleaseFromResponse(platform, distribution, response!);

    return releases;
  }

  internal List<Release> ExtractReleaseFromResponse(Platform platform, string distribution, ReleasesResponse[] response) {
    var releases = new List<Release>(response.Length);

    foreach (var release in response) {
      var asset = release.Assets.Where(a => this.IsPlatformMatch(platform: platform, name: a.Name, distribution: distribution)).ToList();
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
}
