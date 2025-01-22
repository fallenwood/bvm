using System.Runtime.InteropServices;

namespace Bvm;

public enum Platform {
  Unknown,
  WindowsAmd64,
  LinuxAmd64,
  LinuxAarch64,
  MacAmd64,
}

public static class PlatformDetector {
  public static Platform Detect() {
    var platform = Platform.Unknown;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
      platform = Platform.LinuxAmd64;

      if (RuntimeInformation.OSArchitecture == Architecture.Arm64) {
        platform = Platform.LinuxAarch64;
      }
    } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
      platform = Platform.WindowsAmd64;
    } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
      platform = Platform.MacAmd64;
    }

    return platform;
  }
}
