namespace Bvm;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bvm.Models;

public interface IDownloadManager {
  public IDownloadClient Client { get; }
  public Task<List<Release>> RetrieveReleasesAsync(
    IVersionManagerHandler versionManagerHandler,
    string? registry);

  public Task<string> DownloadReleaseAsync(
    IVersionManagerHandler versionManagerHandler,
    string distribution,
    string uri,
    IFileSystemManager fileSystemManager);
}

public sealed partial class DownloadManager(
  IDownloadClient downloadClient,
  Platform platform)
  : IDownloadManager {
  public IDownloadClient Client => downloadClient;

  public async Task<List<Release>> RetrieveReleasesAsync(
    IVersionManagerHandler versionManagerHandler,
    string? registry) {
    return await versionManagerHandler.RetrieveReleasesAsync(downloadClient, platform, registry);
  }

  public async Task<string> DownloadReleaseAsync(
    IVersionManagerHandler versionManagerHandler,
    string distribution,
    string uri,
    IFileSystemManager fileSystemManager) {
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
}
