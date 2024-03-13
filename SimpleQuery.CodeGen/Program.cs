using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

namespace drittich.SimpleQuery.CodeGen
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			var settings = GetSettings();

			Console.WriteLine($"ConnectionString: {settings.ConnectionString}");
			Console.WriteLine($"TargetFolder: {settings.TargetFolder}");
			Console.WriteLine($"ExcludeTables: {string.Join(",", settings.ExcludeTables)}");
			Console.WriteLine($"ModelNamespace: {settings.ModelNamespace}");
			Console.WriteLine($"OneLineNamespaceDeclaration: {settings.OneLineNamespaceDeclaration}");

			var generator = new CodeGenerator(settings.ConnectionString, settings.TargetFolder, settings.ExcludeTables, settings.ModelNamespace, settings.OneLineNamespaceDeclaration);
			await generator.GenerateCodeAsync();
		}

		private static Settings GetSettings()
		{
			Console.WriteLine($"Loading settings from {Directory.GetCurrentDirectory()}");
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

			IConfigurationRoot configuration = builder.Build();

			var settings = new Settings();
			//configuration.GetSection("Settings").Bind(settings);
			var config = configuration.GetSection("Settings");
			settings.ConnectionString = config.GetValue<string>("ConnectionString") ?? string.Empty;
			settings.TargetFolder = config.GetValue<string>("TargetFolder") ?? string.Empty;
			settings.ExcludeTables = config.GetSection("ExcludeTables").Get<string[]>();
			settings.ModelNamespace = config.GetValue<string>("ModelNamespace") ?? string.Empty;
			settings.OneLineNamespaceDeclaration = config.GetValue<bool>("OneLineNamespaceDeclaration");


			if (string.IsNullOrEmpty(settings.ConnectionString))
			{
				throw new InvalidOperationException("ConnectionString is required.");
			}

			if (string.IsNullOrEmpty(settings.TargetFolder))
			{
				throw new InvalidOperationException("TargetFolder is required.");
			}

			return settings;
		}
	}
}
