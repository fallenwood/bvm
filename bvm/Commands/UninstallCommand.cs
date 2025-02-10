namespace Bvm;

using System.CommandLine;
using Bvm.Models;
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

        if (distribution == Distribution.Bun) {
          tag = this.NormalizeBunTag(tag);
          this.fileSystemManager.RemoveBun(tag);
        } else if (distribution == Distribution.Deno) {
          tag = this.NormalizeDenoTag(tag);
          this.fileSystemManager.RemoveDeno(tag);
        } else if (distribution == Distribution.Node) {
          tag = this.NormalizeNodeTag(tag);
          this.fileSystemManager.RemoveNode(tag);
        } else {
          throw new InvalidDistributionException(distribution!);
        }
      },
      tagArgument,
      this.DistributionOption,
      this.SilentOption);

    return command;
  }
}
