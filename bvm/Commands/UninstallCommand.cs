namespace Bvm;

using Microsoft.Extensions.Logging;

public static partial class Commands {
  /// <summary>
  /// Uninstall a specific version
  /// </summary>
  /// <param name="tag"></param>
  /// <param name="distribution">-d, bun/node/deno/tailwindcss</param>
  /// <param name="silent"></param>
  /// <returns></returns>
  public static void Uninstall(
    [ConsoleAppFramework.Argument] string tag,
    string distribution = "bun",
    bool silent = true) {
    if (silent) {
      Logger.Instance.Silent();
    }

    if (string.IsNullOrEmpty(tag)) {
      Logger.Instance.LogError("Please provide a tag to use");
      return;
    }

    var versionManagerHandler = Commands.GetVersionManagerHandler(distribution);
    tag = versionManagerHandler.NormalizeTag(tag);
    versionManagerHandler.Remove(fileSystemManager, tag);
  }
}
