using System.Runtime.CompilerServices;

namespace Immich.Client
{
    public static class ImmichClientExtensions
    {
        public const int PAGE_SIZE = 100;

        public static async Task<bool> IsConnect(this ImmichClient immichClient)
        {
            try
            {
                await immichClient.PingServerAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async IAsyncEnumerable<AssetResponseDto> SearchAllAssetsAsync(this ImmichClient immichClient, MetadataSearchDto dto, [EnumeratorCancellation] CancellationToken ct)
        {
            int page = 1;
            while (!ct.IsCancellationRequested)
            {
                dto.Page = page;
                dto.Size = PAGE_SIZE;

                var result = await immichClient.SearchAssetsAsync(null, null, dto, ct);
                if (result?.Assets?.Items == null || result.Assets.Count == 0)
                    yield break;

                foreach (var item in result.Assets.Items)
                    yield return item;

                if (result.Assets.Items.Count < PAGE_SIZE)
                    yield break;

                page++;
            }
        }
    }
}
