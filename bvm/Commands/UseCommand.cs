namespace Bvm;

using System;
using System.CommandLine;
using System.Linq;
using Bvm.Models;

public partial class Commands {
  public Command UseCommand() {
    var useCommand = new Command(
      name: "use",
      description: "Use a specific version of bun");

    var tagArgument = new Argument<string?>(name: "tag");
    var allOption = new Option<bool>(
      aliases: ["--all", "-a"],
      description: "Copy or link all assets, only works for nodejs");

    useCommand.AddArgument(tagArgument);
    useCommand.AddOption(allOption);

    useCommand.SetHandler(
      async (tag, distribution, all) => {
        if (string.IsNullOrEmpty(tag)) {
          Console.WriteLine("Please provide a tag to use");
          return;
        }

        var config = await this.fileSystemManager.ReadConfigAsync();
        distribution = this.NormalizeDistribution(distribution);

        if (distribution == Distribution.Bun) {
          tag = this.NormalizeBunTag(tag);

          var installed = this.fileSystemManager.GetInstalledBunReleases();
          var release = installed.FirstOrDefault(r => string.Equals(r.TagName, tag));

          if (release is null) {
            Console.WriteLine($"Version {tag} not found");
            return;
          }

          config.BunVersion = tag;

          this.fileSystemManager.CopyOrLinkBun(tag);
        } else if (distribution == Distribution.Deno) {
          var directoryName = this.NormalizeDenoDirectoryName(tag);

          var installed = this.fileSystemManager.GetInstalledDenoReleases();

          var release = installed.FirstOrDefault(r => string.Equals(r.TagName, directoryName));

          if (release is null) {
            Console.WriteLine($"Version {directoryName} not found");
            return;
          }

          config.DenoVersion = directoryName;

          this.fileSystemManager.CopyOrLinkDeno(directoryName);
        } else if (distribution == Distribution.Node) {
          var directoryName = this.NormalizeNodeDirectoryName(tag);
          var installed = this.fileSystemManager.GetInstalledNodeReleases();
          var release = installed.FirstOrDefault(r => string.Equals(r.TagName, directoryName));
          if (release is null) {
            Console.WriteLine($"Version {directoryName} not found");
            return;
          }

          config.NodeVersion = directoryName;
          this.fileSystemManager.CopyOrLinkNode(directoryName, all);
        } else {
          throw new InvalidDistributionException(distribution!);
        }

        await this.fileSystemManager.WriteConfigAsync(config);
      },
      tagArgument,
      this.DistributionOption,
      allOption);

    return useCommand;
  }
}
