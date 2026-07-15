using Immich.Client;

namespace Immich.Organizer.Core.Models
{
    public record class AlbumConfig
    {
        public ConflictResolutionMode Mode { get; init; } = ConflictResolutionMode.Remove;
        public Guid? TagId { get; init; }
        public List<MetadataSearchDto> Filters { get; init; } = new();
    }
}