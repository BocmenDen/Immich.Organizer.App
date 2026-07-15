using Immich.Client;
using Immich.Organizer.Core.Models;
using Microsoft.Extensions.Logging;

namespace Immich.Organizer.Core
{
    public class AlbumSynchronizer
    {
        public readonly string Name;
        private readonly ImmichClient _client;
        private readonly Guid _albumId;
        private readonly AlbumConfig _config;

        public AlbumSynchronizer(ImmichClient client, Guid albumId, AlbumConfig config, string name)
        {
            _client = client;
            _albumId = albumId;
            _config = config;
            Name = name;

            if (config.Mode == ConflictResolutionMode.AddTag && config.TagId != null)
            {
                config.Filters.Add(new MetadataSearchDto()
                {
                    TagIds = [config.TagId!.Value]
                });
            }
        }

        public async Task SynchronizeAsync(ILogger logger, CancellationToken cancellationToken = default)
        {
            var currentIds = await _client.SearchAllAssetsAsync(new MetadataSearchDto()
            {
                AlbumIds = [_albumId]
            }, cancellationToken).Select(x => x.Id).ToHashSetAsync(cancellationToken: cancellationToken);

            logger.LogInformation("Изначально в альбоме {count} файлов", currentIds.Count);

            var targetIds = new HashSet<Guid>();
            foreach (var filter in _config.Filters)
            {
                await foreach (var assetId in _client.SearchAllAssetsAsync(filter, cancellationToken))
                {
                    targetIds.Add(assetId.Id);
                }
            }

            var toAdd = targetIds.Except(currentIds).ToList();
            var toConflict = currentIds.Except(targetIds).ToList();

            logger.LogInformation("Фильтром выбрано {countAllSelect} из них {countAdd} ожидают добавления, а {countConflict} ожидают действия {mode}", targetIds.Count, toAdd.Count, toConflict.Count, _config.Mode);

            foreach (var chunkAdd in toAdd.Chunk(ImmichClientExtensions.PAGE_SIZE))
                await _client.AddAssetsToAlbumAsync(_albumId, new BulkIdsDto { Ids = chunkAdd }, cancellationToken);

            logger.LogInformation("Успешно добавлено в альбом {count} файлов", toAdd.Count);

            if (toConflict.Count == 0) return;

            if (_config.TagId != null && (_config.Mode == ConflictResolutionMode.AddTag || _config.Mode == ConflictResolutionMode.AddTagAndRemove))
            {
                foreach (var chunkAddTag in toConflict.Chunk(ImmichClientExtensions.PAGE_SIZE))
                    await _client.TagAssetsAsync(_config.TagId.Value, new BulkIdsDto()
                    {
                        Ids = chunkAddTag
                    }, cancellationToken);
                logger.LogInformation("Успешно присвоена метка {count} файлам", toConflict.Count);
            }


            if (_config.Mode == ConflictResolutionMode.Remove || _config.Mode == ConflictResolutionMode.AddTagAndRemove)
            {
                foreach (var chunkDelete in toConflict.Chunk(ImmichClientExtensions.PAGE_SIZE))
                    await _client.RemoveAssetFromAlbumAsync(_albumId, new BulkIdsDto { Ids = chunkDelete }, cancellationToken);
                logger.LogInformation("Успешно убраны из альбома {count} файлов", toConflict.Count);
            }

            if (_config.Mode == ConflictResolutionMode.Delete)
            {
                foreach (var chunkDelete in toConflict.Chunk(ImmichClientExtensions.PAGE_SIZE))
                    await _client.DeleteAssetsAsync(new AssetBulkDeleteDto() { Ids = chunkDelete }, cancellationToken);
            }
        }
    }
}