namespace Bvm;

using System.IO.Compression;
using System.Text;

// https://gist.github.com/ForeverZer0/a2cd292bd2f3b5e114956c00bb6e872b
public static class TarGzFile {
  public static void ExtractToDirectory(string filename, string outputDir) {
    using var stream = File.OpenRead(filename);
    ExtractTarGz(stream, outputDir);
  }

  private static void ExtractTarGz(Stream stream, string outputDir) {
    using var gzip = new GZipStream(stream, CompressionMode.Decompress);
    const int chunk = 4096;
    using var memoryStream = new MemoryStream();
    int read;
    var buffer = new byte[chunk];
    do {
      read = gzip.Read(buffer, 0, chunk);
      memoryStream.Write(buffer, 0, read);
    } while (read == chunk);

    memoryStream.Seek(0, SeekOrigin.Begin);
    ExtractTar(memoryStream, outputDir);
  }

  public static void ExtractTar(Stream stream, string outputDir) {
    const int chunk = 4096;
    var buffer = new byte[chunk];
    while (true) {
      stream.ReadExactly(buffer, 0, chunk);
      var name = Encoding.ASCII.GetString(buffer).Trim('\0');
      if (string.IsNullOrWhiteSpace(name)) {
        break;
      }

      stream.Seek(24, SeekOrigin.Current);
      stream.ReadExactly(buffer, 0, 12);
      var size = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);

      stream.Seek(376L, SeekOrigin.Current);

      var output = Path.Combine(outputDir, name);
      if (!Directory.Exists(Path.GetDirectoryName(output))) {
        Directory.CreateDirectory(path: Path.GetDirectoryName(output)!);
      }

      if (!string.Equals(name, "./", StringComparison.InvariantCulture)) {
        using var file = File.Open(output, FileMode.OpenOrCreate, FileAccess.Write);
        var buf = new byte[size];
        stream.ReadExactly(buf);
        file.Write(buf, 0, buf.Length);
      }

      var pos = stream.Position;

      var offset = 512 - (pos % 512);
      if (offset == 512) {
        offset = 0;
      }

      stream.Seek(offset, SeekOrigin.Current);
    }
  }
}
