using Immich.Organizer.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Immich.Organizer.App
{
    internal class Program
    {
        private const string IMMICH_ORGANIZER_CONFIG_PATH = nameof(IMMICH_ORGANIZER_CONFIG_PATH);

        static async Task Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                });
            });
            var loggerInit = loggerFactory.CreateLogger("Init");
            var logger = loggerFactory.CreateLogger("Organizer");
            var scopeConfigFind = loggerInit.BeginScope(nameof(Program));
            string configPath = GetConfigPath(args);
            if (!File.Exists(configPath))
            {
                loggerInit.LogCritical($"Файл конфигурации по пути {{path}} не найден, задайте путь через переменные окружения как {IMMICH_ORGANIZER_CONFIG_PATH} или передайте в качестве первого параметра", configPath);
                return;
            }

            AppConfig? config = null;

            try
            {
                string json = File.ReadAllText(configPath);
                config = JsonConvert.DeserializeObject<AppConfig>(json);
                if (config == null)
                {
                    loggerInit.LogCritical("Не удалось десериализовать конфигурацию.");
                    return;
                }
            }
            catch (Exception e)
            {
                loggerInit.LogCritical(e, "Не удалось десериализовать конфигурацию.");
                return;
            }
            scopeConfigFind?.Dispose();

            var engine = await OrganizerEngine.Build(config, loggerInit);

            if (engine == null)
                return;

            while (true)
            {
                try
                {
                    await engine.SynchronizeAsync(logger);
                }
                catch (Exception e)
                {
                    using var scopeError = logger.BeginScope(nameof(Program));
                    logger.LogWarning(e, "При выполнении организации объектов произошла ошибка");
                }
                await Task.Delay(config.Timer);
            }
        }

        private static string GetConfigPath(string[] args)
        {
            if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                return args[0];

            var envPath = Environment.GetEnvironmentVariable(IMMICH_ORGANIZER_CONFIG_PATH);
            if (!string.IsNullOrEmpty(envPath))
                return envPath;

            return "config.json";
        }
    }
}
