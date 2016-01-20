#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using AutoFollow.Behaviors.Structures;
using AutoFollow.Events;
using AutoFollow.ProfileTags;
using AutoFollow.Resources;
using Zeta.Bot;
using Zeta.Bot.Logic;
using Zeta.Bot.Profile;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.Service;
using Zeta.Game.Internals.SNO;

#endregion

namespace AutoFollow.Networking
{
    [DataContract]
    [KnownType(typeof(Interactable))]
    [KnownType(typeof(Target))]
    public class Message : ITargetable
    {
        private static readonly SimpleAES Crypto = new SimpleAES();

        [DataMember]
        public List<EventData> Events = new List<EventData>();

        [DataMember]
        public int LeaderId;

        public Message()
        {
            ActorClass = ActorClass.Invalid;
            LastUpdated = DateTime.MinValue;
            IsInGame = false;
            IsLoadingWorld = false;
            Position = Vector3.Zero;
            ProfilePosition = Vector3.Zero;
            ProfilePathPrecision = 10f;
            IsInCombat = false;
            IsInTown = false;
        }

        [DataMember]
        public int Index { get; set; }

        [DataMember]
        public int WorldSnoId { get; set; }

        [DataMember]
        public int LevelAreaId { get; set; }

        [DataMember]
        public Vector3 Position { get; set; }

        [DataMember]
        public Vector3 ProfilePosition { get; set; }

        [DataMember]
        public int ProfileActorSno { get; set; }

        [DataMember]
        public float ProfilePathPrecision { get; set; }

        [DataMember]
        public bool IsInCombat { get; set; }

        [DataMember]
        public bool IsInTown { get; set; }

        [DataMember]
        public bool IsInGame { get; set; }

        [DataMember]
        public bool IsLoadingWorld { get; set; }

        [DataMember]
        public bool IsInParty { get; set; }

        [DataMember]
        public int ActorSNO { get; set; }

        [DataMember]
        public int AcdId { get; set; }

        [DataMember]
        public ActorClass ActorClass { get; set; }

        [DataMember]
        public double HitpointsMaxTotal { get; set; }

        [DataMember]
        public double HitpointsCurrent { get; set; }

        [DataMember]
        public GameId GameId { get; set; }

        [DataMember]
        public DateTime LastUpdated { get; set; }

        [DataMember]
        public int OwnerId { get; set; }

        [DataMember]
        public string ProfileTagName { get; set; }

        [DataMember]
        public int NumPartymembers { get; set; }

        [DataMember]
        public string BattleTagEncrypted { get; set; }

        [DataMember]
        public Target CurrentTarget { get; set; }

        [DataMember]
        public string HeroName { get; set; }

        [DataMember]
        public bool IsClient { get; set; }

        [DataMember]
        public bool IsServer { get; set; }

        [DataMember]
        public bool IsRequestingLeader { get; set; }

        [DataMember]
        public bool IsQuickJoinEnabled { get; set; }

        [DataMember]
        public int BNetPartyMembers { get; set; }

        [DataMember]
        public bool InGreaterRift { get; set; }

        [DataMember]
        public Interactable LastPortalUsed { get; set; }

        [DataMember]
        public bool IsVendoring { get; set; }

        [DataMember]
        public BehaviorType BehaviorType { get; set; }

        [DataMember]
        public bool IsInRift { get; set; }

        public bool IsInSameGame
        {
            get
            {
                if (!ZetaDia.IsInGame)
                    return false;

                var gameId = Player.Instance.GameId;

                var result = GameId.FactoryIdHighLowComparer.Equals(GameId, Player.Instance.GameId);
                    //gameId.FactoryId == GameId.FactoryId && (gameId.High == GameId.High || gameId.Low == GameId.Low);
                if (!result)
                    Log.Info("Player (High: {0} Low: {1} Factory: {2}) Other (High: {3} Low: {4} Factory: {5}) DefaultComparerResult={6}",
                        Player.Instance.GameId.High, Player.Instance.GameId.Low, Player.Instance.GameId.FactoryId, 
                        GameId.High, GameId.Low, GameId.FactoryId, GameId.FactoryIdHighLowComparer.Equals(GameId, Player.Instance.GameId));

                return result;
            }
        }

        public bool IsInSameWorld
        {
            get
            {
                if (!ZetaDia.IsInGame || !IsInSameGame)
                    return false;

                return Player.Instance.CurrentWorldSnoId == WorldSnoId;
            }
        }

        public bool IsMe
        {
            get { return Player.BattleTagHash == OwnerId; }
        }

        public bool IsLeavingGame
        {
            get
            {
                if (ProfileTagName == null)
                    return false;

                return ProfileTagName.ToLower().Contains("leavegame");
            }
        }

        public bool IsTownPortalling
        {
            get
            {
                if (ProfileTagName == null)
                    return false;

                return ProfileTagName.ToLower().Contains("town");
            }
        }

        public bool IsTakingPortalBack
        {
            get
            {
                if (ProfileTagName == null)
                    return false;

                return ProfileTagName.ToLower().Contains("usetownportal");
            }
        }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrEmpty(HeroName)
                       && (!IsInGame || IsLoadingWorld || GameId.FactoryId != 0);
                //&& DateTime.UtcNow.Subtract(LastUpdated).TotalSeconds < 10;
            }
        }

        public bool IsLeader
        {
            get { return OwnerId == LeaderId; }
        }

        public bool IsFollower
        {
            get { return !IsLeader; }
        }

        public double Distance
        {
            get { return Position.Distance(ZetaDia.Me.Position); }
        }



        /// <summary>
        /// Used by leaders and followers to pass updates
        /// </summary>
        /// <returns></returns>
        public static Message GetMessage()
        {
            try
            {
                Message m;

                if (!ZetaDia.Service.IsValid || !ZetaDia.Service.Platform.IsConnected)
                {
                    m = new Message
                    {
                        IsInGame = false,
                        Events = EventManager.Events.ToList(),
                };
                    return m;
                }

                if (ZetaDia.Me == null)
                {
                    m = new Message
                    {
                        Index = Player.Instance.Index,
                        IsInGame = false,
                        LastUpdated = DateTime.UtcNow,
                        IsInParty = Player.IsInParty,
                        OwnerId = Player.BattleTagHash,
                        NumPartymembers = Player.NumPlayersInParty,
                        Events = EventManager.Events.ToList(),
                        BNetPartyMembers = ZetaDia.Service.Party.NumPartyMembers,
                        IsServer = Service.ConnectionMode == ConnectionMode.Server,
                        IsClient = Service.ConnectionMode == ConnectionMode.Client,
                        IsRequestingLeader = AutoFollow.CurrentBehavior.Category == BehaviorCategory.Leader,
                        BehaviorType = AutoFollow.CurrentBehavior.Type,
                        IsQuickJoinEnabled = Player.Instance.IsQuickJoinEnabled,
                        BattleTagEncrypted = GetMyEncryptedBattleTag(),
                        AcdId = Player.Instance.AcdId,
                    };
                }
                else if (ZetaDia.IsInGame && !ZetaDia.IsLoadingWorld && ZetaDia.Me.IsValid)
                {
                    m = new Message
                    {
                        Index = Player.Instance.Index,
                        LastUpdated = DateTime.UtcNow,
                        IsInGame = ZetaDia.IsInGame,
                        OwnerId = Player.BattleTagHash,
                        IsInParty = Player.IsInParty,
                        NumPartymembers = Player.NumPlayersInParty,
                        IsLoadingWorld = Player.Instance.IsLoadingWorld,
                        ActorClass = Player.Instance.ActorClass,
                        ActorSNO = Player.Instance.ActorId,
                        GameId = Player.Instance.CurrentGameId,
                        HitpointsCurrent = Player.Instance.HitpointsCurrent,
                        HitpointsMaxTotal = Player.Instance.HitpointsMaxTotal,
                        LevelAreaId = Player.LevelAreaId,
                        IsInTown = Player.LevelAreaId != 55313 && Player.Instance.IsInTown, // A2 Caldeum Bazaar
                        Position = Player.Instance.Position,
                        ProfilePosition = GetProfilePosition(),
                        ProfileActorSno = GetProfileActorSNO(),
                        ProfilePathPrecision = GetProfilePathPrecision(),
                        ProfileTagName = GetProfileTagname(),
                        IsInCombat = Player.Instance.IsInCombat,
                        WorldSnoId = Player.Instance.CurrentWorldSnoId,
                        IsVendoring = BrainBehavior.IsVendoring,
                        BattleTagEncrypted = GetMyEncryptedBattleTag(),
                        HeroName = Player.Instance.HeroName,
                        Events = EventManager.Events.ToList(),
                        CurrentTarget = GetCurrentTarget(),
                        InGreaterRift = Player.Instance.InGreaterRift,
                        BNetPartyMembers = ZetaDia.Service.Party.NumPartyMembers,
                        IsServer = Service.ConnectionMode == ConnectionMode.Server,
                        IsClient = Service.ConnectionMode == ConnectionMode.Client,
                        IsRequestingLeader = AutoFollow.CurrentBehavior.Category == BehaviorCategory.Leader,
                        IsQuickJoinEnabled = Player.Instance.IsQuickJoinEnabled,
                        LastPortalUsed = Player.LastPortalUsed,
                        BehaviorType = AutoFollow.CurrentBehavior.Type,
                        IsInRift = RiftHelper.IsInRift,
                        AcdId = Player.Instance.AcdId,
                    };
                }
                else if (ZetaDia.IsInGame && ZetaDia.IsLoadingWorld)
                {
                    m = new Message
                    {
                        Index = Player.Instance.Index,
                        IsInGame = true,
                        IsLoadingWorld = true,
                        GameId = Player.Instance.CurrentGameId,
                        OwnerId = Player.BattleTagHash,
                        IsInTown = false,
                        WorldSnoId = -1,
                        LevelAreaId = -1,
                        LastUpdated = DateTime.UtcNow,
                        IsInParty = Player.IsInParty,
                        NumPartymembers = Player.NumPlayersInParty,
                        Events = EventManager.Events.ToList(),
                        CurrentTarget = null,
                        BNetPartyMembers = ZetaDia.Service.Party.NumPartyMembers,
                        IsServer = Service.ConnectionMode == ConnectionMode.Server,
                        IsClient = Service.ConnectionMode == ConnectionMode.Client,
                        IsQuickJoinEnabled = Player.Instance.IsQuickJoinEnabled,
                        BehaviorType = AutoFollow.CurrentBehavior.Type,
                        BattleTagEncrypted = GetMyEncryptedBattleTag(),
                        IsInRift = RiftHelper.IsInRift,
                        AcdId = Player.Instance.AcdId,
                    };
                }
                else
                {
                    m = new Message
                    {
                        Index = Player.Instance.Index,
                        IsInGame = false,
                        IsInTown = false,
                        OwnerId = Player.BattleTagHash,
                        WorldSnoId = -1,
                        LastUpdated = DateTime.UtcNow,
                        IsInParty = Player.IsInParty,
                        NumPartymembers = Player.NumPlayersInParty,
                        Events = EventManager.Events.ToList(),
                        CurrentTarget = null,
                        BNetPartyMembers = ZetaDia.Service.Party.NumPartyMembers,
                        IsServer = Service.ConnectionMode == ConnectionMode.Server,
                        IsClient = Service.ConnectionMode == ConnectionMode.Client,
                        IsQuickJoinEnabled = Player.Instance.IsQuickJoinEnabled,
                        BehaviorType = AutoFollow.CurrentBehavior.Type,
                        BattleTagEncrypted = GetMyEncryptedBattleTag(),
                        AcdId = Player.Instance.AcdId,
                    };
                }

                return m;
            }
            catch (Exception ex)
            {
                Log.Info("Exception in GetMessage() {0}", ex);
                return new Message();
            }
        }

        private static string _myEncryptedBattleTag;
        private static string GetMyEncryptedBattleTag()
        {
            return _myEncryptedBattleTag ?? (_myEncryptedBattleTag = Crypto.EncryptToString(ZetaDia.Service.Hero.BattleTagName.Split('#').First()));
        }

        public double GetMillisecondsSinceLastUpdate()
        {
            return DateTime.UtcNow.Subtract(LastUpdated).TotalMilliseconds;
        }

        public override string ToString()
        {
            return string.Format(
                "WorldID: {0} LevelAreaId: {1} Position: {2} IsInTown: {3} IsInGame: {4} ActorSnoId: {5} ActorClass: {6} " +
                "Hitpointsmax: {7} HitpointsCurrent: {8} GameId: {9} LastUpdated: {10} IsVendoring: {11} IsLoadingWorld: {12} " +
                "ProfileTagName: {13} Class={14} IsInParty={15} Id={16} EventsCount={17} PlayerName={18} Target={19} BattleTagHash={20}",
                WorldSnoId,
                LevelAreaId,
                Position,
                IsInTown,
                IsInGame,
                ActorSNO,
                ActorClass,
                HitpointsMaxTotal,
                HitpointsCurrent,
                GameId,
                LastUpdated,
                IsVendoring,
                IsLoadingWorld,
                ProfileTagName,
                ActorClass,
                IsInParty,
                OwnerId,
                Events.Count,
                HeroName,
                CurrentTarget,
                BattleTagEncrypted
                );
        }

        public static Vector3 GetProfilePosition()
        {
            if (!ZetaDia.IsInGame)
                return Vector3.Zero;

            if (ProfileManager.CurrentProfileBehavior == null)
                return Vector3.Zero;

            var currentBehavior = ProfileManager.CurrentProfileBehavior;

            var pos = Vector3.Zero;

            if (currentBehavior != null)
            {
                foreach (var pi in currentBehavior.GetType().GetProperties().ToList())
                {
                    if (pi.Name == "X")
                        pos.X = (float) pi.GetValue(currentBehavior, null);
                    if (pi.Name == "Y")
                        pos.Y = (float) pi.GetValue(currentBehavior, null);
                    if (pi.Name == "Z")
                        pos.Z = (float) pi.GetValue(currentBehavior, null);
                }
            }

            return pos;
        }

        public static ProfileBehavior GetCurrentProfileBehavior()
        {
            return null;
        }

        public static int GetProfileActorSNO()
        {
            if (!ZetaDia.IsInGame)
                return -1;

            if (ProfileManager.CurrentProfileBehavior == null)
                return -1;

            var currentBehavior = ProfileManager.CurrentProfileBehavior;

            var id = -1;

            if (currentBehavior == null)
                return id;
            foreach (var pi in currentBehavior.GetType().GetProperties().ToList().Where(pi => pi.Name.ToLowerInvariant() == "actorid"))
            {
                id = (int) pi.GetValue(currentBehavior, null);
            }

            return id;
        }

        public static List<EventData> GetEvents()
        {
            return EventManager.Events.ToList();
        }

        public static float GetProfilePathPrecision()
        {
            var pathPrecision = 10f;

            if (!ZetaDia.IsInGame)
                return pathPrecision;

            if (ProfileManager.CurrentProfileBehavior == null)
                return pathPrecision;

            var currentBehavior = ProfileManager.CurrentProfileBehavior;

            if (currentBehavior == null)
                return pathPrecision;
            foreach (var pi in currentBehavior.GetType().GetProperties().ToList())
            {
                object val = null;
                if (pi.Name == "PathPrecision")
                    val = pi.GetValue(currentBehavior, null);

                if (val is float)
                    pathPrecision = (float) val;
            }
            return pathPrecision;
        }

        public static bool GetIsInCombat()
        {
            if (CombatTargeting.Instance == null)
                return false;

            if (CombatTargeting.Instance.FirstObject == null)
                return false;

            if (CombatTargeting.Instance.FirstObject == null)
                return false;

            if (CombatTargeting.Instance.FirstObject.IsValid && CombatTargeting.Instance.FirstObject.ActorType == ActorType.Monster)
                return true;

            return false;
        }

        public static string GetProfileTagname()
        {
            var name = string.Empty;

            if (ProfileManager.CurrentProfileBehavior != null)
            {
                name = ProfileManager.CurrentProfileBehavior.GetType().ToString();
            }

            return name;
        }

        internal static bool IsBattleTag(string name, string encryptedBattleTag)
        {
            return Crypto.EncryptToString(name) == encryptedBattleTag;
        }

        public static Target GetCurrentTarget()
        {
            return new Target(CombatTargeting.Instance.Provider.GetObjectsByWeight().FirstOrDefault());
        }
    }


}

