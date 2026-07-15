using Immich.Organizer.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Immich.Organizer.App
{
    public class AppConfig : OrganizerEngineConfig
    {
        public TimeSpan TimeSpan { get; init; } = TimeSpan.FromMinutes(10);
    }
}
