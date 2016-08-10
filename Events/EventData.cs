using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using AutoFollow.Resources;
using Zeta.Game;

namespace AutoFollow.Events
{
    [Serializable]
    public class EventData : EventArgs
    {

        public EventData(EventType type, bool breakExecution) : this(type, null, null, breakExecution)
        {

        }

        public EventData(EventType type, object oldValue = null, object newValue = null, bool breakExecution = false)
        {
            Type = type;
            Time = DateTime.UtcNow;
            OwnerId = Player.BattleTagHash;
            OwnerHeroAlias = Settings.Misc.HideHeroName ?  ZetaDia.Service.Hero.HeroId.ToString() : ZetaDia.Service.Hero.Name;
            IsLeaderEvent = !Player.IsFollower;
            IsFollowerEvent = Player.IsFollower;
            OldValue = oldValue;
            NewValue = newValue;
            Id = Time.GetHashCode() ^ (int)Type ^ OwnerId;
            BreakExecution = breakExecution;
        }

        public DateTime Time { get; private set; }
        public EventType Type { get; private set; }
        public int OwnerId { get; private set; }
        public bool IsLeaderEvent { get; private set; }
        public bool IsFollowerEvent { get; private set; }
        public string OwnerHeroAlias { get; private set; }
        public object OldValue { get; private set; }
        public object NewValue { get; private set; }
        public int Id { get; private set; }

        public bool IsMyEvent => Player.BattleTagHash == OwnerId;

        public TimeSpan Elapsed => DateTime.UtcNow.Subtract(Time);

        public bool BreakExecution { get; private set; }

        public override int GetHashCode()
        {
            return Id;
        }

        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0} EventData >> {1} ({2}) {3} {4:0.#}s ago Old={5} New={6} BreakExecution={7}", 
                IsLeaderEvent ? "Leader" : "Follower", OwnerHeroAlias, OwnerId, 
                Type, DateTime.UtcNow.Subtract(Time).TotalSeconds, OldValue, NewValue, BreakExecution);
        }


    }
}
