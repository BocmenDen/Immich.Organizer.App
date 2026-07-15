using Immich.Organizer.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Immich.Organizer.Core
{
    public class OrganizerEngine
    {
        private readonly List<OrganizerUser> _organizerUsers = default!;

        private OrganizerEngine() { }
        private OrganizerEngine(List<OrganizerUser> organizerUsers) { _organizerUsers = organizerUsers; }

        public async Task SynchronizeAsync(ILogger logger)
        {
            foreach (var user in _organizerUsers)
            {
                using var loggerUser = logger.BeginScope(user.Name);
                await user.SynchronizeAsync(logger);
            }
        }

        public static async Task<OrganizerEngine> Build(OrganizerEngineConfig organizerEngineConfig, ILogger buildLogger)
        {
            List<OrganizerUser> organizerUsers = [];

            foreach (var user in organizerEngineConfig.OrganizerUsers)
            {
                var userBuild = await OrganizerUser.Build(organizerEngineConfig.Host, user, buildLogger);
                if (userBuild == null) continue;
                organizerUsers.Add(userBuild);
            }

            return new OrganizerEngine(organizerUsers);
        }
    }
}