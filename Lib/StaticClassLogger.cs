using Microsoft.Extensions.Logging;

namespace VttVideoPreviews;

public class StaticClassLogger
{
    public StaticClassLogger()
    {
        Logger = LoggerFactory.CreateLogger(GetType().Name);
    }
    private ILoggerFactory LoggerFactory { get; set; }= new LoggerFactory();
    ILogger CreateLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);
    public ILogger Logger { get; init; }
}