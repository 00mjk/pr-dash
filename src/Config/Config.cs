using System;
using System.IO;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using PrDash.Handlers;

namespace PrDash.Configuration
{
    /// <summary>
    /// Represents the complete configuration for the dashboard.
    /// </summary>
    public sealed class Config
    {
        /// <summary>
        /// The default configuration file name.
        /// </summary>
        private static string ConfigName = "pr-dash.yml";

        /// <summary>
        /// The fully qualified configuration file path.
        /// </summary>
        public static string ConfigPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ConfigName);
            }
        }

        /// <summary>
        /// Individual configured accounts.
        /// </summary>
        private readonly List<AccountConfig> m_accounts = new List<AccountConfig>();

        /// <summary>
        /// Gets the individual configured accounts.
        /// </summary>
        public IList<AccountConfig> Accounts
        {
            get { return m_accounts; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Config"/> class.
        /// </summary>
        public Config()
        {
        }

        /// <summary>
        /// Validates that the required configuration file exists, terminates otherwise.
        /// </summary>
        public static void ValidateConfigExists()
        {
            if (!File.Exists(ConfigPath))
            {
                Console.WriteLine("Configuration does not exist: {0}", ConfigPath);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Factory function to initializes a new instance of the
        /// <see cref="Config"/> from a Yaml configuration file.
        /// </summary>
        public static Config FromConfigFile()
        {
            string configFile = ConfigPath;

            Console.WriteLine("Loading configuration from: {0}", configFile);

            return FromFile(configFile);
        }

        /// <summary>
        /// Factory function to initializes a new instance of the <see cref="Config"/>
        /// from a Yaml configuration file.
        /// </summary>
        /// <param name="filePath">The file system path to the configuration file.</param>
        public static Config FromFile(string filePath)
        {
            Config configuration = new Config();

            using (StreamReader reader = new StreamReader(filePath))
            {
                configuration.LoadYaml(reader);
            }

            return configuration;
        }

        /// <summary>
        /// Factory function to initializes a new instance of the <see cref="Config"/>
        /// from a string with Yaml contents.
        /// </summary>
        /// <param name="yamlPayload">The string with Yaml contents.</param>
        public static Config FromString(string yamlPayload)
        {
            Config configuration = new Config();

            using (StringReader reader = new StringReader(yamlPayload))
            {
                configuration.LoadYaml(reader);
            }

            return configuration;
        }

        /// <summary>
        /// Loads the Yaml configuration stream from the specified input.
        /// </summary>
        /// <param name="yamlReader">The input.</param>
        private void LoadYaml(TextReader yamlReader)
        {
            // Load the stream from the text reader.
            //
            YamlStream yaml = new YamlStream();
            yaml.Load(yamlReader);

            // Fetch the root of the document.
            //
            var root = (YamlMappingNode)yaml.Documents[0].RootNode;

            // Fetch the root of the accounts list.
            //
            var accountNodes = (YamlSequenceNode)root.Children[new YamlScalarNode("accounts")];

            foreach (YamlMappingNode accountNode in accountNodes)
            {
                AccountConfig newAccount = new AccountConfig
                {
                    PersonalAccessToken = accountNode.GetString("pat"),
                    OrganizationUrl = accountNode.GetUri("org_url"),
                    Project = accountNode.GetString("project_name"),
                };

                m_accounts.Add(newAccount);
            }
        }
    }
}
