namespace Bvm;

using System.CommandLine;
using Microsoft.Extensions.Logging;

public partial class Commands {
  private const string Default = "default";

  public Command UninstallCommand() {
    var command = new Command(
      name: "uninstall");

    var tagArgument = new Argument<string?>(name: "tag");

    command.AddArgument(tagArgument);

    command.SetHandler(
      (tag, distribution, silent) => {
        if (silent) {
          Logger.Instance.Silent();
        }

        if (string.IsNullOrEmpty(tag)) {
          Logger.Instance.LogError("Please provide a tag to use");
          return;
        }

        if (string.Equals(Default, tag)) {
          Logger.Instance.LogError("TODO");
          return;
        }

        var versionManagerHandler = this.GetVersionManagerHandler(distribution);

        tag = versionManagerHandler.NormalizeTag(tag);
        versionManagerHandler.Remove(fileSystemManager, tag);
      },
      tagArgument,
      this.DistributionOption,
      this.SilentOption);

    return command;
  }
}
