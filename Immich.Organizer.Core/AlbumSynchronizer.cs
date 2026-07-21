using Immich.Client;
using Immich.Organizer.Core.Models;
using Microsoft.Extensions.Logging;

namespace Immich.Organizer.Core
{
    public class AlbumSynchronizer(ImmichClient client, Guid albumId, AlbumConfig config, string name)
    {
        public readonly string Name = name;
        private readonly Queue<List<Guid>> _conflictHistory = new(config.CountConflictHistory + 1);

        public async Task SynchronizeAsync(ILogger logger, CancellationToken cancellationToken = default)
        {
            HashSet<Guid> currentIds = await GetCurrentAlbobIds(cancellationToken);

            logger.LogInformation("Изначально в альбоме {count} файлов", currentIds.Count);
            HashSet<Guid> targetIds = await GetTargetIds(cancellationToken);

            var toAdd = targetIds.Except(currentIds).ToList();
            var toConflict = currentIds.Except(targetIds).ToList();

            if (config.CountConflictHistory > 1)
                toConflict = GetStableConflicts(toConflict);

            logger.LogInformation("Фильтром выбрано {countAllSelect} из них {countAdd} ожидают добавления, а {countConflict} ожидают действия {mode}", targetIds.Count, toAdd.Count, toConflict.Count, config.Mode);

            foreach (var chunkAdd in toAdd.Chunk(ImmichClientExtensions.PAGE_SIZE))
                await client.AddAssetsToAlbumAsync(albumId, new BulkIdsDto { Ids = chunkAdd }, cancellationToken);

            logger.LogInformation("Успешно добавлено в альбом {count} файлов", toAdd.Count);

            if (toConflict.Count == 0) return;

            await FixConflict(logger, toConflict, cancellationToken);
        }

        private List<Guid> GetStableConflicts(List<Guid> currentConflict)
        {
            _conflictHistory.Enqueue(currentConflict);

            while (_conflictHistory.Count > config.CountConflictHistory)
                _conflictHistory.Dequeue();

            if (_conflictHistory.Count < config.CountConflictHistory)
                return [];

            var result = (IEnumerable<Guid>)_conflictHistory.First();
            foreach (var set in _conflictHistory.Skip(1))
                result = result.Intersect(set);
            return [.. result];
        }

        private async Task FixConflict(ILogger logger, List<Guid> toConflict, CancellationToken cancellationToken)
        {
            async Task actionConflictMedia(Func<Guid[], Task> applay, string messageInfo)
            {
                foreach (var chunkAddTag in toConflict.Chunk(ImmichClientExtensions.PAGE_SIZE))
                    await applay(chunkAddTag);
                logger.LogInformation(messageInfo, toConflict.Count);
            }

            if (config.TagId != null && (config.Mode == ConflictResolutionMode.AddTag || config.Mode == ConflictResolutionMode.AddTagAndRemove))
            {
                await actionConflictMedia(
                        (chankItem) => client.TagAssetsAsync(config.TagId.Value, new BulkIdsDto() { Ids = chankItem }, cancellationToken),
                        "Успешно присвоена метка {count} файлам"
                    );
            }

            if (config.Mode == ConflictResolutionMode.Remove || config.Mode == ConflictResolutionMode.AddTagAndRemove)
            {
                await actionConflictMedia(
                    (chankItem) => client.RemoveAssetFromAlbumAsync(albumId, new BulkIdsDto() { Ids = chankItem }, cancellationToken),
                    "Успешно убраны из альбома {count} файлов"
                );
            }

            if (config.Mode == ConflictResolutionMode.Delete)
            {
                await actionConflictMedia(
                    (chankItem) => client.DeleteAssetsAsync(new AssetBulkDeleteDto() { Ids = chankItem }, cancellationToken),
                    "Успешно удалены {count} файлов"
                );
            }
        }

        private async Task<HashSet<Guid>> GetTargetIds(CancellationToken cancellationToken)
        {
            var targetIds = new HashSet<Guid>();
            foreach (var filter in config.Filters)
            {
                filter.WithDeleted ??= false;
                filter.WithStacked ??= true;
                await foreach (var assetId in client.SearchAllAssetsAsync(filter, cancellationToken))
                {
                    targetIds.Add(assetId.Id);
                }
            }

            return targetIds;
        }

        private async Task<HashSet<Guid>> GetCurrentAlbobIds(CancellationToken cancellationToken)
        {
            return await client.SearchAllAssetsAsync(new MetadataSearchDto()
            {
                AlbumIds = [albumId]
            }, cancellationToken).Select(x => x.Id).ToHashSetAsync(cancellationToken: cancellationToken);
        }
    }
}