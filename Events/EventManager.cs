#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFollow.Networking;
using AutoFollow.Resources;
using AutoFollow.UI.Settings;
using Zeta.Bot;
using Zeta.Common;
using Zeta.TreeSharp;

#endregion

namespace AutoFollow.Events
{
    /// <summary>
    /// Fires awaitable events from all bots; fires once on every bot.
    /// </summary>
    public static class EventManager
    {
        public static HashSet<EventData> Events = new HashSet<EventData>();
        private static readonly object Synchronizer = new object();
        private static DateTime _lastPulse = DateTime.MinValue;
        public static Composite TreeStartBehavior = new ActionRunCoroutine(ret => TreeStartTask());
        private static DateTime _lastClearedEvents = DateTime.MinValue;
        public static Queue<EventDispatcher> EventQueue = new Queue<EventDispatcher>();
        private static readonly Dictionary<int, DateTime> _hasFiredIds = new Dictionary<int, DateTime>();

        public static AsyncEvent<Message, EventData> SearchRequest;
        public static AsyncEvent<Message, EventData> ObjectiveFound;
        public static AsyncEvent<Message, EventData> WorldAreaChanged;
        public static AsyncEvent<Message, EventData> LevelAreaChanged;
        public static AsyncEvent<Message, EventData> JoinedGame;
        public static AsyncEvent<Message, EventData> LeftGame;
        public static AsyncEvent<Message, EventData> JoinedParty;
        public static AsyncEvent<Message, EventData> LeftParty;
        public static AsyncEvent<Message, EventData> InTrouble;
        public static AsyncEvent<Message, EventData> GreaterRiftStarted;
        public static AsyncEvent<Message, EventData> NormalRiftStarted;
        public static AsyncEvent<Message, EventData> EngagedElite;
        public static AsyncEvent<Message, EventData> UsedPortal;
        public static AsyncEvent<Message, EventData> Died;
        public static AsyncEvent<Message, EventData> InviteRequest;

        static EventManager()
        {
        }

        public static bool HooksInserted { get; set; }

        public static void Enable()
        {
            Pulsator.OnPulse += Pulse;
            TreeHooks.Instance.OnHooksCleared += Instance_OnHooksCleared;
        }

        public static void Disable()
        {
            Pulsator.OnPulse -= Pulse;
            TreeHooks.Instance.OnHooksCleared -= Instance_OnHooksCleared;
        }

        private static void Instance_OnHooksCleared(object sender, EventArgs e)
        {
            HooksInserted = false;
        }

        /// <summary>
        /// Queues events and maintains hooks and event history size.
        /// </summary>
        private static void Pulse(object sender, EventArgs eventArgs)
        {
            if (DateTime.UtcNow.Subtract(_lastPulse).TotalMilliseconds < 250)
                return;

            _lastPulse = DateTime.UtcNow;

            UpdateHooks();

            lock (Synchronizer)
            {
                ClearEvents();
                ProcessEvents();
            }
        }

        /// <summary>
        /// Makes sure the required hooks are installed.
        /// </summary>
        private static void UpdateHooks()
        {
            if (AutoFollow.Enabled && !HooksInserted)
            {
                Log.Verbose("Inserting EventManager Hook");
                TreeHooks.Instance.InsertHook("TreeStart", 0, TreeStartBehavior);
                HooksInserted = true;
            }
            else if (!AutoFollow.Enabled && HooksInserted)
            {
                Log.Verbose("Removing EventManager Hook");
                TreeHooks.Instance.RemoveHook("TreeStart", TreeStartBehavior);
                HooksInserted = false;
            }
        }

        /// <summary>
        /// Makes sure the number of events stored doesnt get too large.
        /// </summary>
        private static void ClearEvents()
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromSeconds(5);
            if (_lastClearedEvents > cutoff) return;

            var beforeCount = Events.Count;

            lock (Synchronizer)
            {
                Events.RemoveWhere(e => e.Time < cutoff);
                _lastClearedEvents = DateTime.UtcNow;
            }

            _hasFiredIds.RemoveAll(e => DateTime.UtcNow.Subtract(e.Value).TotalSeconds > 30);
            _lastClearedEvents = DateTime.UtcNow;
       
            if (AutoFollowSettings.Instance.DebugLogging)
                Log.Debug("Cleared Events Cache Before={0} After={1}", beforeCount, Events.Count);
        }

        /// <summary>
        /// The events list is updated externally with events from other bots
        /// So we must always look for any new/unfired events.
        /// </summary>
        public static void ProcessEvents()
        {
            lock (Synchronizer)
            {
                Events.Where(e => !HasFired(e)).ForEach(FireEvent);
            }
        }

        /// <summary>
        /// Properly prepare and queue an specific event.
        /// </summary>
        public static void FireEvent(EventData e)
        {           
            Log.Debug("New Event {0} EventId={1}", e.ToString(), e.GetHashCode());

            Message m;
            if (!TryGetMessageForId(e, out m))
            {
                Log.Debug("Unable to find message for the event. Owner='{0}'", e.OwnerId);
                return;
            }
                
            if (e.IsLeaderEvent)
            {
                if (AutoFollowSettings.Instance.DebugLogging)
                    Log.Warn("Firing {0} EventId={1}", e.ToString(), e.GetHashCode());
            }
            else
            {
                if (AutoFollowSettings.Instance.DebugLogging)
                    Log.Info("Firing {0} EventId={1}", e.ToString(), e.GetHashCode());
            }

            // These events are async so we can't just fire them here.
            // They need to be awaited within a bot hook.
            switch (e.Type)
            {
                case EventType.GreaterRiftStarted: Queue(GreaterRiftStarted, e, m); break;
                case EventType.NormalRiftStarted: Queue(NormalRiftStarted, e, m); break;
                case EventType.InTrouble: Queue(InTrouble, e, m); break;
                case EventType.JoinedParty: Queue(JoinedParty, e, m); break;
                case EventType.LeftParty: Queue(LeftParty, e, m); break;
                case EventType.LeftGame: Queue(LeftGame, e, m); break;
                case EventType.JoinedGame: Queue(JoinedGame, e, m); break;
                case EventType.LevelAreaChanged: Queue(LevelAreaChanged, e, m); break;
                case EventType.WorldAreaChanged: Queue(WorldAreaChanged, e, m); break;
                case EventType.ObjectiveFound: Queue(ObjectiveFound, e, m); break;
                case EventType.EngagedElite: Queue(EngagedElite, e, m); break;
                case EventType.UsedPortal: Queue(UsedPortal, e, m); break;
                case EventType.Died: Queue(Died, e, m); break;
                case EventType.InviteRequest: Queue(InviteRequest, e, m); break;
            }

            lock (Synchronizer)
            {
                _hasFiredIds.Add(e.Id, DateTime.UtcNow);

                if (!Events.Contains(e))
                    Add(e);
            }
        }

        private static bool TryGetMessageForId(EventData e, out Message m)
        {
            if (e.OwnerId == Player.BattleTagHash)
            {
                m = Player.Instance.Message;
            }
            else if (e.OwnerId == AutoFollow.CurrentLeader.OwnerId)
            {
                m = AutoFollow.CurrentLeader;
            }
            else
            {
                m = AutoFollow.CurrentParty.FirstOrDefault(message => message.OwnerId == e.OwnerId);                
            }            
            return m != null;
        }

        public static bool HasFired(EventData e)
        {
            return _hasFiredIds.ContainsKey(e.Id);
        }

        /// <summary>
        /// Fires events as async coroutines inside TreeStart hook.
        /// </summary>
        private static async Task<bool> TreeStartTask()
        {
            //Log.Verbose("EventManager Task");
            while (EventQueue.Any())
            {
                var e = EventQueue.Dequeue();
                Log.Verbose("Firing Queue Task Event {0} from {1}", e.EventData.Type, e.SenderMessage.HeroName);
                await e.AsyncEvent.InvokeAsync(e.SenderMessage, e.EventData);
            }

            return false;
        }

        private static void Queue(AsyncEvent<Message, EventData> e, EventData data, Message m)
        {
            if (e != null)
                EventQueue.Enqueue(new EventDispatcher(e, data, m));
        }

        public static void Add(IList<EventData> events)
        {
            if (events == null)
                return;

            lock (Synchronizer)
            {
                events.ForEach(Add);
            }
        }

        public static void Add(EventData e)
        {
            if (e == null)
                return;

            lock (Synchronizer)
            {
                if (e.Time < _lastClearedEvents)
                    return;

                if (Events.Contains(e))
                    return;

                Log.Info("Added event {0} from {1} to EventManager", e.Type, e.OwnerHeroName);
                Events.Add(e);
            }
        }
    }
}