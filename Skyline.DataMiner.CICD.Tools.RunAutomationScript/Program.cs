using System.CommandLine.Builder;
using System.CommandLine.Hosting;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Events;

using Skyline.DataMiner.CICD.Tools.RunAutomationScript.Commands;

namespace Skyline.DataMiner.CICD.Tools.RunAutomationScript
{
    /// <summary>
    /// This .NET Tools allows you to start an automation script..
    /// </summary>
    public static class Program
    {
        /*
         * Design guidelines for command line tools: https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#design-guidance
         */

        /// <summary>
        /// Code that will be called when running the tool.
        /// </summary>
        /// <param name="args">Extra arguments.</param>
        /// <returns>0 if successful.</returns>
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("This .NET Tools allows you to start an automation script.")
            {
                new Local()
            };

            var isDebug = new Option<bool>(
            name: "--debug",
            description: "Indicates the tool should write out debug logging.")
            {
                IsHidden = true
            };

            var logLevel = new Option<LogEventLevel>(
                name: "--minimum-log-level",
                description: "Indicates what the minimum log level should be. Default is Information",
                getDefaultValue: () => LogEventLevel.Information);

            rootCommand.AddGlobalOption(isDebug);
            rootCommand.AddGlobalOption(logLevel);

            ParseResult parseResult = rootCommand.Parse(args);
            LogEventLevel level = parseResult.GetValueForOption(isDebug)
                ? LogEventLevel.Debug
                : parseResult.GetValueForOption(logLevel);

            var builder = new CommandLineBuilder(rootCommand).UseDefaults().UseHost(host =>
            {
                host.ConfigureServices(services =>
                    {
                        services.AddLogging(loggingBuilder =>
                                {
                                    loggingBuilder.AddSerilog(
                                        new LoggerConfiguration()
                                            .MinimumLevel.Is(level)
                                            .WriteTo.Console()
                                            .CreateLogger());
                                });
                    })
                    .ConfigureHostConfiguration(configurationBuilder =>
                    {
                        configurationBuilder.AddUserSecrets<Local>() // For easy testing
                                            .AddEnvironmentVariables();
                    })
                    .UseCommandHandler<Local, LocalCommandHandler>();
            });

            return await builder.Build().InvokeAsync(args);
        }
    }
}