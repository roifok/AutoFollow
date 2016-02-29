using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AutoFollow.Networking;
using AutoFollow.Resources;
using Zeta.Bot;
using Zeta.Bot.Logic;
using Zeta.Bot.Navigation;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Service;

namespace AutoFollow.Events
{
    /// <summary>
    /// Responsible for tracking and recording state of the players and game.
    /// </summary>
    public static class ChangeMonitor
    {
        private static int _worldId;
        private static int _levelAreaId;
        private static Target _currentTarget;
        private static bool _isSuspectedIdle;
        private static DateTime _lastSuspectedIdleStart = DateTime.MaxValue;

        private static bool _isDead;
        private static bool _inTrouble;
        private static int _lastClosestDeathGateAcdId;
        private static Vector3 _lastPosition;
        private static bool _isVendoring;

        public static DateTime LastChecked = DateTime.MinValue;
        public static DateTime LastCastTeleport = DateTime.MinValue;
        public static DateTime LastCastWaypoint = DateTime.MinValue;
        public static DateTime LastCastTownPortal = DateTime.MinValue;
        public static DateTime LastCastPortalSpell = DateTime.MinValue;
        public static DateTime LastPulseTime = DateTime.MinValue;
        public static DateTime LastBotStartedTime = DateTime.MinValue;
        public static DateTime LastGameJoinedTime = DateTime.MinValue;
        public static DateTime LastBotStoppedTime = DateTime.MinValue;
        public static DateTime LastPlayerDiedTime = DateTime.MinValue;
        public static DateTime LastGameLeftTime = DateTime.MinValue;
        public static DateTime LastWorldChange = DateTime.MinValue;
        public static DateTime LastLoadedProfileTime = DateTime.MinValue;
        public static DateTime LastRiftGaurdianKilledTime = DateTime.MinValue;

        public static DateTime LastSeenLeaderTime;
        public static int LastSeenLeaderWorld;
        public static Vector3 LastSeenLeaderPosition;


        public static int LastWorldId;
        public static Vector3 LastWorldPosition;
        public static bool IsIdle;
        
        static ChangeMonitor()
        {

        }

        public static void Enable()
        {
            Pulsator.OnPulse += OnPulse;
            GameEvents.OnGameJoined += GameEvents_OnGameJoined;
            GameEvents.OnGameLeft += GameEvents_OnGameLeft;
            GameEvents.OnPlayerDied += GameEvents_OnPlayerDied;
            GameEvents.OnWorldChanged += GameEvents_OnWorldChanged;
            GameEvents.OnWorldTransferStart += GameEvents_OnWorldTransferStart;
            BotMain.OnStart += OnBotStart;
            BotMain.OnStart += OnBotStop;
            ProfileManager.OnProfileLoaded += OnProfileLoaded;
        }

        public static void Disable()
        {
            Pulsator.OnPulse += OnPulse;
            GameEvents.OnGameJoined -= GameEvents_OnGameJoined;
            GameEvents.OnGameLeft -= GameEvents_OnGameLeft;
            GameEvents.OnPlayerDied -= GameEvents_OnPlayerDied;
            GameEvents.OnWorldChanged -= GameEvents_OnWorldChanged;
            GameEvents.OnWorldTransferStart -= GameEvents_OnWorldTransferStart;
            BotMain.OnStart -= OnBotStart;
            BotMain.OnStart -= OnBotStop;
            ProfileManager.OnProfileLoaded -= OnProfileLoaded;
        }

        private static void OnProfileLoaded(object sender, EventArgs eventArgs)
        {
            Log.Warn("Profile Loaded: {0}", ProfileManager.CurrentProfile.Name);
            LastLoadedProfileTime = DateTime.UtcNow;
        }

        private static void OnPulse(object sender, EventArgs eventArgs)
        {
            LastPulseTime = DateTime.UtcNow;
        }

        private static void OnBotStop(IBot bot)
        {
            LastBotStoppedTime = DateTime.UtcNow;
        }

        private static void OnBotStart(IBot bot)
        {
            Log.Info("Bot Started");
            LastBotStartedTime = DateTime.UtcNow;
        }

        public static TimeSpan BotRunningTime
        {
            get
            {
                return BotMain.IsRunning
                    ? DateTime.UtcNow.Subtract(LastBotStartedTime)
                    : LastBotStoppedTime.Subtract(LastBotStartedTime);
            }
        }

        private static void GameEvents_OnWorldTransferStart(object sender, EventArgs e)
        {
            if (ZetaDia.Service.Party.CurrentPartyLockReasonFlags != PartyLockReasonFlag.None)
                return;

            if (ZetaDia.IsLoadingWorld || ZetaDia.IsInTown)
                return;

            Log.Info("World Transfer Start Fired!");
        }

        private static void GameEvents_OnWorldChanged(object sender, EventArgs e)
        {
            LastWorldChange = DateTime.UtcNow;
            
            if (DateTime.UtcNow.Subtract(LastCastPortalSpell).TotalSeconds < 10)
                return;

            //LastWorldPosition = _lastPosition;
            //LastWorldId = _worldId;

            PortalUsedEvent(false);

            IsIdle = false;
        }

        private static void PortalUsedEvent(bool isDeathGate)
        {
            Log.Info("Last World Id={0} LastWorldPosition={1}", LastWorldId, LastWorldPosition);

            var portal = Data.Portals.OrderByDescending(p => p.Distance).FirstOrDefault();
            if (portal != null)
            {
                var interactable = BotHistory.PortalHistory .FirstOrDefault(p => DateTime.UtcNow.Subtract(p.Value.LastTimeCloseTo).TotalSeconds < 15 && p.Value.WorldSnoId != ZetaDia.CurrentWorldSnoId);
                if (interactable.Value != null)
                {
                    Log.Info("Portal Used: {0} in WorldSnoId={1}", interactable.Value.InternalName, interactable.Value.WorldSnoId);
                    Player.LastEntryPortal = interactable.Value;
                    IsIdle = false;
                    EventManager.FireEvent(new EventData(EventType.UsedPortal, null, interactable.Value, true));
                }
                else
                {
                    Log.Debug("Unable to figure out which portal was interacted with.");
                }
            }
        }



        private static void GameEvents_OnPlayerDied(object sender, EventArgs e)
        {
            LastPlayerDiedTime = DateTime.UtcNow;
            EventManager.FireEvent(new EventData(EventType.Died));
        }

        private static void GameEvents_OnGameLeft(object sender, EventArgs e)
        {
            LastGameLeftTime = DateTime.UtcNow;
            EventManager.FireEvent(new EventData(EventType.LeftGame));
        }

        private static void GameEvents_OnGameJoined(object sender, EventArgs e)
        {
            LastGameJoinedTime = DateTime.UtcNow;
            EventManager.FireEvent(new EventData(EventType.JoinedGame));
        }

        private static bool CheckVisualEffectNoneForPower(ACD commonData, SNOPower power)
        {
            if (commonData.GetAttribute<int>(((int)power << 12) + ((int)ActorAttributeType.PowerBuff0VisualEffectNone & 0xFFF)) == 1)
                return true;

            return false;
        }

        public static void CheckForChanges()
        {
            var now = DateTime.UtcNow;

            if (!ZetaDia.IsInGame || ZetaDia.Me == null || DateTime.UtcNow.Subtract(LastChecked).TotalMilliseconds < 250)
                return;

            var me = ZetaDia.Me;
            if (!me.IsValid || me.CommonData == null)
                return;

            var commonData = ZetaDia.Me.CommonData;
            if (!commonData.IsValid || commonData.IsDisposed)
                return;

            var currentPosition = ZetaDia.Me.Position;
            if (currentPosition == Vector3.Zero)
                return;

            var distanceTravelled = _lastPosition.Distance(currentPosition);

            var isCastingTownPortal = CheckVisualEffectNoneForPower(commonData, SNOPower.UseStoneOfRecall);
            var isCastingTeleport = CheckVisualEffectNoneForPower(commonData, SNOPower.TeleportToPlayer_Cast);
            var isCastingWaypoint = CheckVisualEffectNoneForPower(commonData, SNOPower.TeleportToWaypoint_Cast);

            if (isCastingTownPortal)
                LastCastWaypoint = now;  

            if (isCastingTeleport)
                LastCastWaypoint = now;

            if (isCastingWaypoint)
                LastCastWaypoint = now;

            if (isCastingTownPortal || isCastingTeleport || isCastingWaypoint)
                LastCastPortalSpell = now;

            CheckForDeathGateUsage(currentPosition, distanceTravelled);

            CheckForIdle();

            var isVendoring = BrainBehavior.IsVendoring;
            if (isVendoring != _isVendoring)
            {
                if (isVendoring)
                {
                    EventManager.FireEvent(new EventData(EventType.StartingTownRun));                   
                }
                _isVendoring = isVendoring;
            }

            var worldId = ZetaDia.CurrentWorldSnoId;
            if (ZetaDia.WorldInfo.IsValid && worldId != _worldId && worldId != 0)
            {                
                EventManager.FireEvent(new EventData(EventType.WorldAreaChanged, _worldId, worldId));
                LastWorldId = _worldId;
                LastWorldPosition = _lastPosition;
                _worldId = worldId;
            }

            if (!AutoFollow.CurrentLeader.IsMe)
            {
                var leaderActor = Data.GetPlayerActor(AutoFollow.CurrentLeader);               
                if (leaderActor != _leaderActor)
                {
                    if (leaderActor == null)
                    {
                        Log.Warn("Lost leader, he was right here");
                    }
                    else
                    {
                        Log.Warn("Found leader! ");
                        LastSeenLeaderPosition = leaderActor.Position;
                        LastSeenLeaderWorld = _worldId;
                        LastSeenLeaderTime = DateTime.UtcNow;
                    }
                    _leaderActor = leaderActor;
                }
            }
            else
            {
                _leaderActor = null;
            }

            var levelAreaId = Player.LevelAreaId;
            if (ZetaDia.WorldInfo.IsValid && levelAreaId != _levelAreaId && levelAreaId != 0)
            {
                EventManager.FireEvent(new EventData(EventType.LevelAreaChanged, _levelAreaId, levelAreaId));                
                _levelAreaId = levelAreaId;
            }

            var isDead = ZetaDia.Me.IsDead;
            if (Data.IsValid(ZetaDia.Me) && isDead != _isDead)
            {
                EventManager.FireEvent(new EventData(EventType.Died));
                _isDead = isDead;
            }

            var inTrouble = ZetaDia.Me.HitpointsCurrentPct < 0.5;
            if (Data.IsValid(ZetaDia.Me) && isDead != _inTrouble)
            {
                EventManager.FireEvent(new EventData(EventType.InTrouble));
                _inTrouble = inTrouble;
            }

            if (RiftHelper.RiftQuest != null)
            {
                var riftGaurdianKilled = RiftHelper.RiftQuest.Step == RiftQuest.RiftStep.Cleared || RiftHelper.RiftQuest.Step == RiftQuest.RiftStep.UrshiSpawned;
                if (riftGaurdianKilled != _riftGaurdianKilled)
                {
                    if (riftGaurdianKilled)
                    {
                        Log.Warn("So sad, that poor misunderstood greebly just wanted a hug.");
                        EventManager.FireEvent(new EventData(EventType.KilledRiftGaurdian));
                    }
                    _riftGaurdianKilled = riftGaurdianKilled;
                }
            }

            var riftGaurdianHiding = RiftHelper.CurrentRift.IsStarted && !RiftHelper.CurrentRift.HasGuardianSpawned;
            if (riftGaurdianHiding != _riftGaurdianHiding)
            {
                if (!riftGaurdianHiding && RiftHelper.CurrentRift.IsStarted)
                {
                    Log.Warn("Whats this, there's something lurking nearby?");
                    EventManager.FireEvent(new EventData(EventType.SpawnedRiftGaurdian));
                }                    
                _riftGaurdianHiding = riftGaurdianHiding;
            }

            _lastPosition = ZetaDia.Me.Position;
        }


        private static void CheckForDeathGateUsage(Vector3 currentPosition, float distanceTravelled)
        {
            var nearestDeathGate = Data.FindNearestDeathGate();
            if (nearestDeathGate != null && nearestDeathGate.Distance < 20f && _lastPosition != Vector3.Zero &&
                currentPosition != Vector3.Zero)
            {
                if (_lastClosestDeathGateAcdId != nearestDeathGate.ACDId && distanceTravelled > 20f)
                {
                    Log.Warn("Used a Death Gate!");
                    PortalUsedEvent(true);
                }
                _lastClosestDeathGateAcdId = nearestDeathGate.ACDId;
            }
        }

        private static void CheckForIdle()
        {
            if (BotRunningTime.TotalSeconds < 5)
                return;

            if (ZetaDia.Me.Movement.SpeedXY < 0.5 && !Navigation.IsBlocked && IdleConditions())
            {
                if (!IsIdle && !_isSuspectedIdle)
                {
                    Log.Debug("Suspected Idle");
                    _isSuspectedIdle = true;
                    _lastSuspectedIdleStart = DateTime.UtcNow;
                }

                if (_isSuspectedIdle && DateTime.UtcNow.Subtract(_lastSuspectedIdleStart).TotalSeconds > 5)
                {
                    Log.Debug("Is Idle");
                    IsIdle = true;
                    _isSuspectedIdle = false;
                }
            }
            else
            {
                if (IsIdle)
                    Log.Debug("No longer Idle");

                _isSuspectedIdle = false;
                IsIdle = false;
            }
        }

        private static bool IdleConditions()
        {
            if (!ZetaDia.IsInGame || ZetaDia.Me == null || !ZetaDia.Me.IsValid)
                return false;

            if (ZetaDia.IsInTown && (UIElements.VendorWindow.IsVisible || UIElements.SalvageWindow.IsVisible))
                return false;

            if (ZetaDia.Me.IsInConversation || ZetaDia.IsPlayingCutscene || ZetaDia.IsLoadingWorld)
                return false;

            if (ZetaDia.Me.LoopingAnimationEndTime > 0)
                return false;

            if (DateTime.UtcNow.Subtract(LastBotStartedTime).TotalSeconds < 10)
                return false;

            if (_busyAnimationStates.Contains(ZetaDia.Me.CommonData.AnimationState))
                return false;

            if (ZetaDia.Me.IsDead || UIElements.WaypointMap.IsVisible)
                return false;

            if (IsProfileBusy())
                return false;

            if (ZetaDia.Me.Movement.SpeedXY > 0.3 && _lastPosition.Distance(ZetaDia.Me.Position) > 2f)
                return false;

            return true;
        }

        private static readonly HashSet<AnimationState> _busyAnimationStates = new HashSet<AnimationState>
        {
            AnimationState.Running,
            AnimationState.Attacking,
            AnimationState.Channeling,
            AnimationState.Casting,
            AnimationState.Dead,
            AnimationState.Invalid,
        };

        private static DiaPlayer _leaderActor;
        private static bool _riftGaurdianHiding;
        private static bool _riftGaurdianKilled;


        private static bool IsProfileBusy()
        {
            ProfileBehavior currentProfileBehavior = null;
            try
            {
                if (ProfileManager.CurrentProfileBehavior != null)
                    currentProfileBehavior = ProfileManager.CurrentProfileBehavior;
            }
            catch (Exception ex)
            {
                Log.Debug("Exception while checking for current profile behavior! {0}", ex);
            }
            if (currentProfileBehavior != null)
            {
                var profileBehaviortype = currentProfileBehavior.GetType();
                var behaviorName = profileBehaviortype.Name;
                if (profileBehaviortype == typeof(UseTownPortalTag) ||
                     profileBehaviortype == typeof(WaitTimerTag) ||
                     behaviorName.ToLower().Contains("townrun") ||
                     behaviorName.ToLower().Contains("townportal") ||
                     behaviorName.ToLower().Contains("leave") ||
                     behaviorName.ToLower().Contains("wait"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
