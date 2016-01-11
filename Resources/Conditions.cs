using System;
using System.Linq;
using AutoFollow.Events;
using Zeta.Common;

namespace AutoFollow.Resources
{
    public static class Conditions
    {
        public static void Initialize()
        {
            ScriptManager.RegisterShortcutsDefinitions((typeof(Conditions)));
        }

        public static bool LeaderEvent(string type)
        {
            EventType eventType;
            Enum.TryParse(type, out eventType);
            return EventManager.Events.Any(e => !EventManager.HasFired(e) && e.Type == eventType);
        }
    }
}
