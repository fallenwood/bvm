namespace Bvm;

using System;
using Microsoft.Extensions.Logging;

public class Logger() : ILogger {
  public static Logger Instance { get; } = new Logger();

  public LogLevel MinLogLevel { get; private set; } = LogLevel.Information;

  public void Silent() {
    this.MinLogLevel = LogLevel.None;
  }

  public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
    throw new NotImplementedException();
  }

  public bool IsEnabled(LogLevel logLevel) {
    return logLevel >= this.MinLogLevel;
  }

  public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
    if (this.IsEnabled(logLevel) == false) {
      return;
    }

    var message = formatter(state, exception);
    Console.WriteLine(message);
  }

  public void Write(LogLevel logLevel, char message) {
    if (this.IsEnabled(logLevel) == false) {
      return;
    }

    Console.Write(message);
  }

  public void WriteLine(LogLevel logLevel, char message) {
    if (this.IsEnabled(logLevel) == false) {
      return;
    }

    Console.WriteLine(message);
  }
}
