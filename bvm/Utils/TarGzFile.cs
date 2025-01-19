namespace Bvm;

using System.Formats.Tar;
using System.IO.Compression;

public static class TarGzFile {
  public static void ExtractToDirectory(string filename, string outputDir) {
    if (!Directory.Exists(outputDir)) {
      Directory.CreateDirectory(outputDir);
    }

    using var fileStream = File.OpenRead(filename);
    using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
    TarFile.ExtractToDirectory(gzipStream, outputDir, true);
  }
}
