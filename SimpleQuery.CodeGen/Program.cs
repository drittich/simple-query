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

			var generator = new CodeGenerator(settings.ConnectionString, settings.TargetFolder, settings.ExcludeTables);
			await generator.GenerateCodeAsync();
		}

		private static Settings GetSettings()
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

			IConfigurationRoot configuration = builder.Build();

			var settings = new Settings();
			configuration.GetSection("Settings").Bind(settings);

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
