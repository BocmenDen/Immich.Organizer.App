using Immich.Client;
using Immich.Organizer.Core.Models;
using Microsoft.Extensions.Logging;

namespace Immich.Organizer.Core
{
    public class OrganizerUser
    {
        public string Name { get; private init; } = default!;
        private readonly List<AlbumSynchronizer> _albumSynchronizers = default!;
        private OrganizerUser() { }
        private OrganizerUser(List<AlbumSynchronizer> albumSynchronizers, string name)
        {
            _albumSynchronizers = albumSynchronizers;
            Name = name;
        }

        public async Task SynchronizeAsync(ILogger logger)
        {
            foreach (var album in _albumSynchronizers)
            {
                using var loggerAlbom = logger.BeginScope(album.Name);
                await album.SynchronizeAsync(logger);
            }
        }

        public static async Task<OrganizerUser?> Build(string host, OrganizerUserConfig organizerUserConfig, ILogger buildLogger)
        {
            ImmichClient immichClient = ImmichClientBuilder.Build(host, organizerUserConfig.ApiKey);

            if (!(await immichClient.IsConnect()))
            {
                buildLogger.LogError("Не удалось подключиться к серверу [{host}] с ключом [{apiKey}]", host, organizerUserConfig.ApiKey);
                return null;
            }

            var userName = await immichClient.GetUserName();

            using var scopeUser = buildLogger.BeginScope(userName);

            List<AlbumSynchronizer> albumSynchronizers = [];
            foreach (var (idAlbum, albumConfig) in organizerUserConfig.AlbumConfigs)
            {
                var albumSynchronizer = await CreateAlbumSynchronizer(immichClient, idAlbum, albumConfig, buildLogger);
                if (albumSynchronizer != null)
                    albumSynchronizers.Add(albumSynchronizer);
            }

            if (albumSynchronizers.Count == 0)
            {
                buildLogger.LogWarning("Не найдено ни одного действительного альбома для синхронизации");
                return null;
            }
            buildLogger.LogInformation("Данные группировки для пользователя построены");
            return new OrganizerUser(albumSynchronizers, userName);
        }

        private static async Task<AlbumSynchronizer?> CreateAlbumSynchronizer(ImmichClient immichClient, Guid albumId, AlbumConfig albumConfig, ILogger buildLogs)
        {
            try
            {
                var albumInfo = await immichClient.GetAlbumInfoAsync(albumId, null, null);
                var name = albumInfo.AlbumName;

                if (albumConfig.Mode == ConflictResolutionMode.AddTag)
                {
                    if (albumConfig.TagId == null)
                    {
                        buildLogs.LogWarning("Альбом \"{albumId}\" не имеет назначенного тега [{tag}]", name, albumConfig.TagId);
                        return null;
                    }

                    try
                    {
                        var tagInfo = await immichClient.GetTagByIdAsync(albumConfig.TagId!.Value);
                    }
                    catch
                    {
                        buildLogs.LogWarning("Назначенный тег [{tagId}] для альбома \"{albumId}\" не найден", albumConfig.TagId, name);
                        return null;
                    }
                }
                buildLogs.LogInformation("Альбом \"{albumId}\" успешно собран, mode={mode}, tag=\"{tagId}\"", name, albumConfig.Mode, albumConfig.TagId);
                return new AlbumSynchronizer(immichClient, albumId, albumConfig, albumInfo.AlbumName);
            }
            catch
            {
                buildLogs.LogWarning("Не удалось найти альбом с ID {albumId}", albumId);
                return null;
            }
        }
    }
}
