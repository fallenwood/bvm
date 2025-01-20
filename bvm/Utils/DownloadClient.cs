namespace Bvm;

public class Progress(long totalSize) {
  private const char StartMark = '[';
  private const char EndMark = ']';
  private const char ProgressMark = '#';

  private const int TotalProgress = 50;
  private long currentSize = 0;
  private int currentProgress = 0;

  public void Start() {
    Console.Write(StartMark);
  }

  public void End() {
    Console.WriteLine(EndMark);
  }

  public void OnUpdate(long size) {
    if (totalSize < 0) {
      Console.Write(ProgressMark);
      return;
    }

    this.currentSize += size;
    var newProgress = (int)(currentSize * TotalProgress / totalSize);
    for (int i = currentProgress + 1; i <= newProgress; i++) {
      Console.Write(ProgressMark);
    }
    this.currentProgress = newProgress + 1;
  }
}

public interface IDownloadClient {
  public Task<HttpResponseMessage> GetAsync(Uri uri, bool showProgress);
  public Task<HttpResponseMessage> GetAsync(string uri, bool showProgress);
}

public sealed class DownloadClient(HttpClient httpClient) : IDownloadClient {
  public async Task<HttpResponseMessage> GetAsync(string uri, bool showProgress) {
    return await GetAsync(new Uri(uri), showProgress);
  }

  public async Task<HttpResponseMessage> GetAsync(Uri uri, bool showProgress) {
    if (!showProgress) {
      return await httpClient.GetAsync(uri);
    }

    var httpResponse = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
    httpResponse.EnsureSuccessStatusCode();

    var totalSize = httpResponse.Content.Headers.ContentLength ?? -1;

    var progress = new Progress(totalSize);
    progress.Start();

    await using var responseStream = await httpResponse.Content.ReadAsStreamAsync();

    var buffer = new byte[4096];

    while (true) {
      var readSize = await responseStream.ReadAsync(buffer, 0, buffer.Length);
      if (readSize == 0) {
        break;
      }

      progress.OnUpdate(readSize);
    }

    progress.End();

    return httpResponse;
  }
}
