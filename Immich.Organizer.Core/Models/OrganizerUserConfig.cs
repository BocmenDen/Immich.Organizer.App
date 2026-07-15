namespace Immich.Organizer.Core.Models
{
    public class OrganizerUserConfig
    {
        public required string ApiKey { get; init; }
        public Dictionary<Guid, AlbumConfig> AlbumConfigs { get; set; } = [];
    }
}