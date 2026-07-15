namespace Immich.Organizer.Core.Models
{
    public class OrganizerEngineConfig
    {
        public required string Host { get; init; }
        public List<OrganizerUserConfig> OrganizerUsers { get; init; } = [];
    }
}