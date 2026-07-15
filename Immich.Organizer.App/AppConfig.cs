using Immich.Organizer.Core.Models;

namespace Immich.Organizer.App
{
    public class AppConfig : OrganizerEngineConfig
    {
        public TimeSpan TimeSpan { get; init; } = TimeSpan.FromMinutes(10);
    }
}
