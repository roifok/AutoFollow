#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFollow.Networking;
using AutoFollow.Resources;
using Zeta.Bot;
using Zeta.Common;
using Zeta.Game;
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

        public static Composite TreeStartBehavior = new ActionRunCoroutine(ret => ExecuteQueuedEventsTask());
        public static Composite OutOfGameBehavior = new ActionRunCoroutine(ret => OutOfGameTask());
        

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
        public static AsyncEvent<Message, EventData> LeavingGame;
        public static AsyncEvent<Message, EventData> GoingToTown;
        public static AsyncEvent<Message, EventData> StartingTownRun;
        public static AsyncEvent<Message, EventData> KilledRiftGaurdian;
        public static AsyncEvent<Message, EventData> SpawnedRiftGaurdian;        

        public delegate void NormalEvent();
        public static event NormalEvent OnPulseOutOfGame = () => { };

        static EventManager()
        {

        }

        private static Dictionary<EventType,DateTime> LastFiredEventStore = Enum.GetValues(typeof(EventType)).Cast<EventType>().ToDictionary(t => t, t => DateTime.MinValue);

        public static bool HooksInserted { get; set; }
        public static bool IsExecutionBreakRequested { get; private set; }

        public static void Enable()
        {
            Pulsator.OnPulse += Pulsator_OnPulse;
            TreeHooks.Instance.OnHooksCleared += Instance_OnHooksCleared;
        }

        private static void Pulsator_OnPulse(object sender, EventArgs eventArgs)
        {
            Update();
        }

        public static void Disable()
        {
            Pulsator.OnPulse -= Pulsator_OnPulse;
            TreeHooks.Instance.OnHooksCleared -= Instance_OnHooksCleared;
        }

        private static void Instance_OnHooksCleared(object sender, EventArgs e)
        {
            HooksInserted = false;
        }

        /// <summary>
        /// Queues events and maintains hooks and event history size.
        /// </summary>
        public static void Update()
        {
            if (DateTime.UtcNow.Subtract(_lastPulse).TotalMilliseconds < 250)
                return;

            if (!ZetaDia.Service.IsValid || !ZetaDia.Service.Hero.IsValid)
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
                TreeHooks.Instance.InsertHook("OutOfGame", 0, TreeStartBehavior);
                HooksInserted = true;
            }
            else if (!AutoFollow.Enabled && HooksInserted)
            {
                Log.Verbose("Removing EventManager Hook");
                TreeHooks.Instance.RemoveHook("TreeStart", TreeStartBehavior);
                TreeHooks.Instance.InsertHook("OutOfGame", 0, TreeStartBehavior);
                HooksInserted = false;
            }
        }

        /// <summary>
        /// Makes sure the number of events stored doesnt get too large.
        /// </summary>
        private static void ClearEvents()
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromSeconds(2);
            if (_lastClearedEvents > cutoff) return;

            var beforeCount = Events.Count;

            lock (Synchronizer)
            {
                Events.RemoveWhere(e => e.Time < cutoff);
                _lastClearedEvents = DateTime.UtcNow;
            }

            _hasFiredIds.RemoveAll(e => DateTime.UtcNow.Subtract(e.Value).TotalSeconds > 10);
            _lastClearedEvents = DateTime.UtcNow;
       
            if (Settings.Misc.DebugLogging)
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
                Events.Where(e => !HasFired(e)).ForEach(item => FireEvent(item));
            }
        }

        /// <summary>
        /// Fire an event
        /// </summary>
        /// <param name="e">event data to send</param>
        /// <param name="debounceTime">duration to block subequent events of this type.</param>
        public static void FireEvent(EventData e, TimeSpan debounceTime = default(TimeSpan))
        {
            if (debounceTime == default(TimeSpan))
                debounceTime = TimeSpan.FromSeconds(1);

            var lastFiredType = LastFiredEventStore[e.Type];
            var timeSinceLastFiredType = DateTime.UtcNow.Subtract(lastFiredType);
            if (timeSinceLastFiredType < debounceTime)
            {
                //Log.Debug("Debouncing event. LastFired={0}s DebounceTime={1} Remaining={2}",
                //    timeSinceLastFiredType.TotalSeconds, debounceTime.TotalSeconds, (debounceTime - timeSinceLastFiredType).TotalSeconds);

                return;
            }         

            LastFiredEventStore[e.Type] = DateTime.UtcNow;

            Log.Debug("New Event {0} EventId={1}", e.ToString(), e.GetHashCode());

            Message m;
            if (!TryGetMessageForId(e, out m))
            {
                Log.Debug("Unable to find message for the event. Owner='{0}'", e.OwnerId);
                return;
            }

            if (e.IsMyEvent)
            {
                
            }
            else if (e.IsLeaderEvent)
            {
                if (Settings.Misc.DebugLogging)
                    Log.Warn("Firing {0} EventId={1}", e.ToString(), e.GetHashCode());
            }
            else
            {
                if (Settings.Misc.DebugLogging)
                    Log.Info("Firing {0} EventId={1}", e.ToString(), e.GetHashCode());
            }

            //if (!e.IsMyEvent)
            //{
                FireEventByType(e, m);
            //}

            lock (Synchronizer)
            {
                _hasFiredIds.Add(e.Id, DateTime.UtcNow);

                if (!Events.Contains(e))
                    Add(e);
            }

            if (e.BreakExecution)
            {
                Log.Debug("Execution break was requested");
                IsExecutionBreakRequested = true;
            }

        }

        private static void FireEventByType(EventData e, Message m)
        {
            // These events are async so we can't just fire them here.
            // They need to be awaited within a bot hook.
            switch (e.Type)
            {
                case EventType.GreaterRiftStarted:
                    Queue(GreaterRiftStarted, e, m);
                    break;
                case EventType.NormalRiftStarted:
                    Queue(NormalRiftStarted, e, m);
                    break;
                case EventType.InTrouble:
                    Queue(InTrouble, e, m);
                    break;
                case EventType.JoinedParty:
                    Queue(JoinedParty, e, m);
                    break;
                case EventType.LeftParty:
                    Queue(LeftParty, e, m);
                    break;
                case EventType.LeftGame:
                    Queue(LeftGame, e, m);
                    break;
                case EventType.JoinedGame:
                    Queue(JoinedGame, e, m);
                    break;
                case EventType.LevelAreaChanged:
                    Queue(LevelAreaChanged, e, m);
                    break;
                case EventType.WorldAreaChanged:
                    Queue(WorldAreaChanged, e, m);
                    break;
                case EventType.ObjectiveFound:
                    Queue(ObjectiveFound, e, m);
                    break;
                case EventType.EngagedElite:
                    Queue(EngagedElite, e, m);
                    break;
                case EventType.UsedPortal:
                    Queue(UsedPortal, e, m);
                    break;
                case EventType.Died:
                    Queue(Died, e, m);
                    break;
                case EventType.InviteRequest:
                    Queue(InviteRequest, e, m);
                    break;
                case EventType.LeavingGame:
                    Queue(LeavingGame, e, m);
                    break;
                case EventType.GoingToTown:
                    Queue(GoingToTown, e, m);
                    break;
                case EventType.StartingTownRun:
                    Queue(StartingTownRun, e, m);
                    break;
                case EventType.KilledRiftGaurdian:
                    Queue(KilledRiftGaurdian, e, m);
                    break;
                case EventType.SpawnedRiftGaurdian:
                    Queue(SpawnedRiftGaurdian, e, m);
                    break;
            }
        }

        private static bool TryGetMessageForId(EventData e, out Message m)
        {
            if (e.OwnerId == Player.BattleTagHash)
            {
                m = Player.CurrentMessage;
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

        private static async Task<bool> OutOfGameTask()
        {                                    
            OnPulseOutOfGame();
            Update();
            await ExecuteQueuedEventsTask();
            return false;
       }
        /// <summary>
        /// Fires events as async coroutines inside TreeStart hook.
        /// </summary>
        private static async Task<bool> ExecuteQueuedEventsTask()
        {
            if (!ZetaDia.Service.IsValid || !ZetaDia.Service.Hero.IsValid)
                return false;

            if (IsExecutionBreakRequested)
            {
                Log.Debug("EventManager hook fired and execution request was cleared");
                IsExecutionBreakRequested = false;
            }

            while (EventQueue.Any())
            {
                var e = EventQueue.Dequeue();
                Log.Debug("Firing Queue Task Event {0} from {1}", e.EventData.Type, e.SenderMessage.HeroAlias);
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

                Log.Debug("Added event {0} from {1} to EventManager", e.Type, e.OwnerHeroAlias);
                Events.Add(e);
            }
        }
    }
}