using System;
using AutoFollow.Networking;
using AutoFollow.UI.Settings;
using Zeta.Bot;
using Zeta.Bot.Logic;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Service;

namespace AutoFollow.Resources
{
    public class Player
    {
        public int Index { get; set; }
        public int RActorId { get; set; }
        public int AcdId { get; set; }
        public double HitpointsCurrent { get; set; }
        public double HitpointsMaxTotal { get; set; }
        public double HitpointsCurrentPct { get; set; }

        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public int CurrentLevelAreaId { get; set; }
        public int CurrentWorldSnoId { get; set; }
        public int CurrentDynamicWorldId { get; set; }
        public bool IsInTown { get; set; }
        public bool IsInGame { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool IsValid { get { return ZetaDia.Me.IsValid; } }
        public bool IsVendoring { get; set; }
        public ActorClass ActorClass { get; set; }
        public int ActorId { get; set; }
        public string HeroName { get; set; }
        public bool IsInCombat { get; set; }
        public GameId GameId { get; set; }
        public bool IsLoadingWorld { get; set; }
        public bool IsQuickJoinEnabled { get; set; }
        public bool InGreaterRift { get; set; }

        public static int CachedLevelAreaId = -1;
        public static DateTime LastUpdatedLevelAreaId = DateTime.MinValue;
        private static bool _lastIsPartyLeader = false;
        private static DateTime _lastUpdateIsPartyLeaderMembers = DateTime.MinValue;
        private static int _lastNumPartyMembers = 0;
        private static DateTime _lastUpdateNumPartyMembers = DateTime.MinValue;        

        public Message Message = new Message();
        private Vector3 _position;

        public static Message CurrentMessage
        {
            get { return Instance.Message; }            
        }

        public static int LevelAreaId
        {
            get
            {
                if (!ZetaDia.IsInGame)
                    return 0;
                if (ZetaDia.IsLoadingWorld)
                    return 0;
                if (ZetaDia.Me == null)
                    return 0;
                if (!ZetaDia.Me.IsValid)
                    return 0;

                if (CachedLevelAreaId != -1 && !(DateTime.UtcNow.Subtract(LastUpdatedLevelAreaId).TotalSeconds > 2)) 
                    return CachedLevelAreaId;
                
                CachedLevelAreaId = ZetaDia.CurrentLevelAreaSnoId;
                LastUpdatedLevelAreaId = DateTime.UtcNow;
                return CachedLevelAreaId;
            }
        }

        private static AnimationState _lastAnimationState = AnimationState.Invalid;
        public static AnimationState CurrentAnimationState
        {
            get
            {
                try
                {
                    _lastAnimationState = ZetaDia.Me.CommonData.AnimationState;
                }
                catch { }
                return _lastAnimationState;
            }
        }

        private static Player instance;
        public static Player Instance
        {
            get
            {
                return instance ?? (instance = new Player());
            }
        }

        public Player()
        {
            Log.Info("Creating Player Obj");
            LastUpdate = DateTime.MinValue;
            GameEvents.OnGameJoined += (s, e) => LastGameJoinedTime = DateTime.UtcNow;
            //Update();
        }

        public Player(int rActorId)
        {
            Log.Info("Creating Player Obj");
            this.RActorId = rActorId;
            //Update();
        }

        private static string lastLogMessage = "";

        public void Update()
        {
            if (DateTime.UtcNow.Subtract(LastUpdate).TotalMilliseconds < 50)
                return;

            if (instance != null)
            {
                Message = Message.GetMessage();
            }

            if (!ZetaDia.IsInGame || ZetaDia.IsLoadingWorld || ZetaDia.Me == null)
                return;

            if (!ZetaDia.Me.IsValid || !ZetaDia.Me.CommonData.IsValid || ZetaDia.Me.CommonData.IsDisposed)
                return;

            if (ZetaDia.PlayerData == null || !ZetaDia.PlayerData.IsValid)
                return;

            using (new MemoryHelper())
            {
                try
                {
                    Index = ZetaDia.PlayerData.Index;
                    RActorId = ZetaDia.Me.RActorId;
                    LastUpdate = DateTime.UtcNow;
                    IsInGame = ZetaDia.IsInGame;
                    AcdId = ZetaDia.Me.ACDId;
                    HitpointsCurrent = ZetaDia.Me.HitpointsCurrent;
                    HitpointsMaxTotal = ZetaDia.Me.HitpointsMaxTotal;
                    HitpointsCurrentPct = HitpointsMaxTotal > 0 ? ZetaDia.Me.HitpointsCurrent/ZetaDia.Me.HitpointsMaxTotal : 0;
                    //TryUpdate(ref _position, () => ZetaDia.Me.Position);
                    Position = ZetaDia.Me.Position;
                    CurrentLevelAreaId = LevelAreaId;
                    CurrentWorldSnoId = ZetaDia.CurrentWorldSnoId;
                    CurrentDynamicWorldId = ZetaDia.WorldId;
                    IsInTown = ZetaDia.IsInTown;
                    IsVendoring = BrainBehavior.IsVendoring;
                    ActorId = ZetaDia.Me.ActorSnoId;
                    ActorClass = ZetaDia.Me.ActorClass;
                    HeroName = Common.CleanString(ZetaDia.Service.Hero.Name);
                    CurrentGameId = ZetaDia.Service.CurrentGameId;
                    IsInCombat = ZetaDia.Me.IsInCombat;
                    GameId = ZetaDia.Service.CurrentGameId;
                    IsLoadingWorld = ZetaDia.IsLoadingWorld;
                    IsQuickJoinEnabled = ZetaDia.SocialPreferences.QuickJoinEnabled;
                    InGreaterRift = !ZetaDia.IsInTown && ZetaDia.Me.IsParticipatingInTieredLootRun;
                }
                catch (Exception ex)
                {
                    Log.Verbose("Exception {0}", ex);
                }
            }
        }

        //static void TryUpdate<T>(ref T property, Func<T> func)
        //{
        //    try
        //    {
        //        var result = func();
        //        if (!ReferenceEquals(result,default(T)))
        //            property = result;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (!ex.Message.StartsWith("Only part"))
        //            throw;
        //    }
        //}

        public bool UsePower(SNOPower power, Vector3 position)
        {
            return ZetaDia.Me.UsePower(power, position);
        }

        public override string ToString()
        {            
            return String.Format("Player ({12} - {13}): RActorId={0} AcdId={1} HitpointsCurrent={2:0} HitpointsCurrentPct={3:0} HitpointsMaxTotal={4:0} Position={5} LevelAreaId={6} WorldSnoId={7} DynamicWorldId={8} IsInGame={9} IsInTown={10} IsVendoring: {11}",
                this.RActorId, this.AcdId, this.HitpointsCurrent, this.HitpointsCurrentPct*100, this.HitpointsMaxTotal, this.Position, this.CurrentLevelAreaId, this.CurrentWorldSnoId, this.CurrentDynamicWorldId, this.IsInGame, this.IsInTown, this.IsVendoring, AutoFollow.CurrentBehaviorType, AutoFollow.CurrentBehavior.Category);
        }

        public static int BattleTagHash
        {
            get { return ZetaDia.Service.Hero.BattleTagName.GetHashCode(); }
        }

        public static int NumPlayersInParty
        {
            get
            {
                if (!ZetaDia.Service.IsValid)
                    return _lastNumPartyMembers;

                if (!ZetaDia.Service.Hero.IsValid)
                    return _lastNumPartyMembers;

                if (ZetaDia.IsLoadingWorld)
                    return _lastNumPartyMembers;

                if (DateTime.UtcNow.Subtract((DateTime) _lastUpdateNumPartyMembers).TotalSeconds < 5)
                    return _lastNumPartyMembers;

                if (ZetaDia.Service.IsValid &&
                    ZetaDia.Service.Platform.IsValid &&
                    ZetaDia.Service.Platform.IsConnected &&
                    ZetaDia.Service.Hero.IsValid &&
                    ZetaDia.Service.Party.IsValid)
                {
                    _lastUpdateNumPartyMembers = DateTime.UtcNow;
                    _lastNumPartyMembers = ZetaDia.Service.Party.NumPartyMembers;
                    return _lastNumPartyMembers;
                }
                return 0;
            }
        }

        public static DateTime LastGameJoinedTime { get; set; }

        public static bool IsPartyleader
        {
            get
            {
                if (!ZetaDia.Service.IsValid)
                    return false;
                if (!ZetaDia.Service.Hero.IsValid)
                    return false;
                if (ZetaDia.IsLoadingWorld)
                    return false;

                if (DateTime.UtcNow.Subtract((DateTime) _lastUpdateIsPartyLeaderMembers).TotalSeconds < 5)
                    return _lastIsPartyLeader;

                if (ZetaDia.Service.IsValid &&
                    ZetaDia.Service.Platform.IsValid &&
                    ZetaDia.Service.Platform.IsConnected &&
                    ZetaDia.Service.Hero.IsValid &&
                    ZetaDia.Service.Party.IsValid)
                {

                    _lastUpdateIsPartyLeaderMembers = DateTime.UtcNow;
                    _lastIsPartyLeader = ZetaDia.Service.Party.IsPartyLeader;
                    return _lastIsPartyLeader;
                }
                return false;
            }
        }

        public static bool IsInParty
        {
            get
            {
                return NumPlayersInParty > 1;
            }
        }

        public static bool IsClient
        {
            get { return Service.ConnectionMode == ConnectionMode.Client; }
        }

        public static bool IsServer
        {
            get { return Service.ConnectionMode == ConnectionMode.Server; }
        }

        public static bool IsLeader
        {
            get { return AutoFollow.CurrentLeader != null && Instance.Message != null && Instance.Message.OwnerId == AutoFollow.CurrentLeader.OwnerId; }
        }

        public static bool IsFollower
        {
            get { return !IsLeader; }
        }


        public GameId CurrentGameId { get; set; }

        public static Interactable LastPortalUsed { get; set; }
        public static Interactable LastEntryPortal { get; set; }
    }
}
