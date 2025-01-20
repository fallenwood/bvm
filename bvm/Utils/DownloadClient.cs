using Microsoft.Extensions.Logging;

namespace Bvm;

public class Progress(long totalSize) {
  private const char StartMark = '[';
  private const char EndMark = ']';
  private const char ProgressMark = '#';

  private const int TotalProgress = 50;
  private long currentSize = 0;
  private int currentProgress = 0;

  public void Start() {
    Logger.Instance.Write(LogLevel.Information, StartMark);
  }

  public void End() {
    Logger.Instance.WriteLine(LogLevel.Information, EndMark);
  }

  public void OnUpdate(long size) {
    if (totalSize < 0) {
      Logger.Instance.Write(LogLevel.Information, ProgressMark);
      return;
    }

    this.currentSize += size;
    var newProgress = (int)(currentSize * TotalProgress / totalSize);

    if (currentProgress < newProgress) {
      for (var i = currentProgress; i < newProgress; i++) {
        Logger.Instance.Write(LogLevel.Information, ProgressMark);
      }
      this.currentProgress = newProgress;
    }
  }
}

public interface IDownloadClient {
  public Task<HttpResponseMessage> GetAsync(Uri uri);
  public Task<(HttpResponseMessage, Stream)> GetAsyncWithProgress(Uri uri);

  public async Task<HttpResponseMessage> GetAsync(string uri) => await GetAsync(new Uri(uri));
  public async Task<(HttpResponseMessage, Stream)> GetAsyncWithProgress(string uri) => await GetAsyncWithProgress(new Uri(uri));
}

public sealed class DownloadClient(HttpClient httpClient) : IDownloadClient {
  public async Task<HttpResponseMessage> GetAsync(Uri uri) {
    return await httpClient.GetAsync(uri);
  }

  public async Task<(HttpResponseMessage, Stream)> GetAsyncWithProgress(Uri uri) {
    var httpResponse = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
    httpResponse.EnsureSuccessStatusCode();

    var totalSize = httpResponse.Content.Headers.ContentLength ?? -1;

    var progress = new Progress(totalSize);
    progress.Start();

    var memoryStream = new MemoryStream();
    await using var responseStream = await httpResponse.Content.ReadAsStreamAsync();

    var buffer = new byte[10240];

    while (true) {
      var readSize = await responseStream.ReadAsync(buffer);
      if (readSize == 0) {
        break;
      }

      progress.OnUpdate(readSize);
      memoryStream.Write(buffer, 0, readSize);
    }

    progress.End();

    memoryStream.Seek(0, SeekOrigin.Begin);

    return (httpResponse, memoryStream);
  }
}
