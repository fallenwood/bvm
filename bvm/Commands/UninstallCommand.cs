namespace Bvm;

using System;
using System.CommandLine;
using System.Linq;
using Bvm.Models;

public partial class Commands {
  private const string Default = "default";

  public Command UninstallCommand() {
    var command = new Command(
      name: "uninstall");

    var tagArgument = new Argument<string?>(name: "tag");

    command.AddArgument(tagArgument);

    command.SetHandler(
      (tag, distribution) => {
        if (string.IsNullOrEmpty(tag)) {
          Console.WriteLine("Please provide a tag to use");
          return;
        }

        if (string.Equals(Default, tag)) {
          Console.WriteLine("TODO");
          return;
        }

        if (distribution == Distribution.Bun) {
          tag = this.NormalizeBunTag(tag);
          this.fileSystemManager.RemoveBun(tag);
        } else if (distribution == Distribution.Deno) {
          tag = this.NormalizeDenoTag(tag);
          this.fileSystemManager.RemoveDeno(tag);
        } else {
          throw new InvalidDistributionException(distribution!);
        }
      },
      tagArgument,
      this.DistributionOption);

    return command;
  }
}
