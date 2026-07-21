using Immich.Organizer.Core.Models;

namespace Immich.Organizer.App
{
    public class AppConfig : OrganizerEngineConfig
    {
        public TimeSpan Timer { get; init; } = TimeSpan.FromMinutes(10);
    }
}
