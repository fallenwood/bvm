namespace Bvm;

using System;

public class InvalidDistributionException(string distribution)
  : Exception($"Invalid distribution: {distribution}") {
}
