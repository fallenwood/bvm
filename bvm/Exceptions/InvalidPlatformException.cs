namespace Bvm;

using System;

public class InvalidPlatformException(Platform platform)
  : Exception($"Unspported Platform: {platform}") {
}
