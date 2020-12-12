﻿using CommandLine;
using PrDash.Configuration;
using PrDash.DataSource;
using PrDash.View;

namespace PrDash
{
    /// <summary>
    /// The class entry point for the program.
    /// </summary>
    public static class EntryPoint
    {
        /// <summary>
        /// The program entry point for pr-dash.
        /// </summary>
        /// <param name="args">The raw command line arguments.</param>
        /// <returns>The process exit code.</returns>
        public static int Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<CommandLineOptions>(args);

            return options.MapResult(
                options => RunAndReturnExitCode(options),
                _ => 1);
        }

        /// <summary>
        /// The post command line option parsing entry point.
        /// </summary>
        /// <param name="options">The parsed command line options.</param>
        /// <returns>The process exit code.</returns>
        private static int RunAndReturnExitCode(CommandLineOptions options)
        {
            Config.ValidateConfigExists();
            Config config = Config.FromConfigFile(options);

            IPullRequestSource source;
            if (config.DemoModeEnabled)
            {
                source = new DemoPullRequestSource();
            }
            else
            {
                source = new AzureDevOpsPullRequestSource(config);
            }

            Display.RunUiLoop(config, source);

            return 0;
        }
    }
}
