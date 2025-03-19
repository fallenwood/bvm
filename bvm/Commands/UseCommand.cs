namespace Bvm;

using System.CommandLine;
using System.Linq;
using Microsoft.Extensions.Logging;
using Bvm.Models;

public partial class Commands {
  public Command UseCommand() {
    var useCommand = new Command(
      name: "use",
      description: "Use a specific version of bun");

    var tagArgument = new Argument<string?>(name: "tag");
    var allOption = new Option<bool>(
      aliases: ["--all", "-a"],
      description: "Copy or link all assets, only works for nodejs on windows");

    useCommand.AddArgument(tagArgument);
    useCommand.AddOption(allOption);

    useCommand.SetHandler(
      async (tag, distribution, all, silent) => {
        if (silent) {
          Logger.Instance.Silent();
        }

        if (string.IsNullOrEmpty(tag)) {
          Logger.Instance.LogError("Please provide a tag to use");
          return;
        }

        var config = await this.fileSystemManager.ReadConfigAsync();
        distribution = this.NormalizeDistribution(distribution);

        var versionManagerHandler = this.GetVersionManagerHandler(distribution);
        var directoryName = versionManagerHandler.NormalizeDirectoryName(tag);
        var installed = versionManagerHandler.GetInstalledReleases(this.fileSystemManager);
        var release = installed.FirstOrDefault(r => string.Equals(r.TagName, directoryName));

        if (release is null) {
          Logger.Instance.LogError("Version {version} not found", directoryName);
          return;
        }

        if (distribution == Distribution.Bun) {
          config.BunVersion = directoryName;
        } else if (distribution == Distribution.Deno) {
          config.DenoVersion = directoryName;
        } else if (distribution == Distribution.Node) {
          config.NodeVersion = directoryName;
        } else if (distribution == Distribution.Tailwind) {
          config.TailwindVersion = directoryName;
        } else {
          throw new InvalidDistributionException(distribution!);
        }

        versionManagerHandler.CopyOrLink(this.fileSystemManager, platform, directoryName, all);

        await this.fileSystemManager.WriteConfigAsync(config);
      },
      tagArgument,
      this.DistributionOption,
      allOption,
      this.SilentOption);

    return useCommand;
  }
}
