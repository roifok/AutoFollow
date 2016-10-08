﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using AutoFollow.Behaviors.Structures;
using AutoFollow.Events;
using AutoFollow.Resources;
using Trinity;
using Trinity.Components.Combat.Resources;
using Trinity.Framework.Actors.ActorTypes;
using Trinity.Framework.Objects;
using Trinity.Framework.Objects.Api;
using Zeta.Bot;
using Zeta.Bot.Logic;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.Service;

namespace AutoFollow.Networking
{
    [DataContract]
    [KnownType(typeof(Interactable))]
    [KnownType(typeof(Target))]
    [KnownType(typeof(IPartyMember))]
    [KnownType(typeof(TrinityObjectType))]
    [KnownType(typeof(PartyObjective))]    
    public class Message : IPartyMember
    {
        private static readonly SimpleAES Crypto = new SimpleAES();
        private static string _myEncryptedBattleTag;
        private static string _myEncryptedRealId;

        public Message()
        {
            ActorClass = ActorClass.Invalid;
            LastUpdated = DateTime.MinValue;
            ProfilePathPrecision = 10f;
            Events = new List<EventData>();
        }

        [DataMember]
        public int CatchUpDistance { get; set; }
        [DataMember]
        public int FollowDistance { get; set; }
        [DataMember]
        public int WorldDynamicId { get; set; }

        [DataMember]
        public PartyObjective Objective { get; set; }

        public string SettingsCode { get; set; }

        [DataMember]
        public Vector3 Destination { get; set; }

        [DataMember]
        public string ProfileName { get; set; }

        [DataMember]
        public int Dexterity { get; set; }

        [DataMember]
        public int Intelligence { get; set; }

        [DataMember]
        public int Vitality { get; set; }

        [DataMember]
        public int Strength { get; set; }

        [DataMember]
        public string HeroName { get; set; }

        [DataMember]
        public List<EventData> Events { get; set; }

        [DataMember]
        public int LeaderId { get; set; }

        [DataMember]
        public int Index { get; set; }

        [DataMember]
        public int LevelAreaId { get; set; }

        [DataMember]
        public int Paragon { get; set; }

        [DataMember]
        public int Level { get; set; }

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
        public int ActorSnoId { get; set; }

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
        public bool IsDead { get; set; }

        [DataMember]
        public bool IsInBossEncounter { get; set; }

        [DataMember]
        public int JewelUpgradesleft { get; set; }

        [DataMember]
        public int FreeBackPackSlots { get; set; }

        [DataMember]
        public bool IsParticipatingInGreaterRift { get; set; }

        [DataMember]
        public string BattleTagEncrypted { get; set; }

        [DataMember]
        public string RealIdNameEncrypted { get; set; }

        [DataMember]
        public Target CurrentTarget { get; set; }

        [DataMember]
        public int HeroId { get; set; }

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
        public bool IsInGreaterRift { get; set; }

        [DataMember]
        public Interactable LastPortalUsed { get; set; }

        [DataMember]
        public bool IsVendoring { get; set; }

        [DataMember]
        public BehaviorCategory BehaviorCategory { get; set; }

        [DataMember]
        public bool IsInRift { get; set; }

        [DataMember]
        public Vector3 LastPositionInPreviousWorld { get; set; }

        [DataMember]
        public int PreviousWorldSnoId { get; set; }

        [DataMember]
        public bool IsCastingTownPortal { get; set; }

        [DataMember]
        public ApiBuild Build { get; set; }

        [DataMember]
        public ApiRoutine Routine { get; set; }

        public bool IsInSameGame
        {
            get
            {
                if (!ZetaDia.IsInGame)
                    return false;

                return GameId.FactoryIdHighLowComparer.Equals(GameId, Player.GameId);
            }
        }

        public bool IsInSameWorld
        {
            get
            {
                if (!ZetaDia.IsInGame || !IsInSameGame)
                    return false;

                return Player.CurrentWorldSnoId == WorldSnoId;
            }
        }

        public bool IsMe => Player.BattleTagHash == OwnerId;

        public bool IsValid => !string.IsNullOrEmpty(HeroAlias) && (!IsInGame || IsLoadingWorld || GameId.FactoryId != 0);

        public bool IsLeader => OwnerId == LeaderId;

        public bool IsFollower => !IsLeader;

        [DataMember]
        public int WorldSnoId { get; set; }



        [DataMember]
        public Vector3 Position { get; set; }


        /// <summary>
        /// Returns the AcdId of this message's actor in the 'current' bot's game world.
        /// (AcdId changes for each D3 client)
        /// </summary>
        public int AcdId => Data.GetAcdIdByHeroId(HeroId);

        /// <summary>
        /// Object used to send information between bots.
        /// This is used in the communication thread and DB doesn't play nicely when multiple threads access D3 memory.
        /// Must be be created from non-memory accessing properties, which is why it's populated from the Player object.
        /// </summary>
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
                        Events = EventManager.Events.ToList()
                    };
                    return m;
                }

                if (ZetaDia.Me == null)
                {
                    m = new Message
                    {
                        Index = Player.Index,
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
                        BehaviorCategory = AutoFollow.CurrentBehavior.Category,
                        IsQuickJoinEnabled = Player.IsQuickJoinEnabled,
                        BattleTagEncrypted = GetMyEncryptedBattleTag(),
                        RealIdNameEncrypted = GetMyEncryptedRealId(),
                        HeroName = Player.HeroName,
                        HeroId = Player.HeroId,
                        ActorClass = Player.ActorClass,
                        Paragon = Player.Paragon,
                        Level = Player.Level,
                        IsLoadingWorld = Player.IsLoadingWorld,
                    };
                }
                else if (ZetaDia.IsInGame && !ZetaDia.IsLoadingWorld && ZetaDia.Me.IsValid)
                {
                    m = new Message
                    {
                        Index = Player.Index,
                        LastUpdated = DateTime.UtcNow,
                        IsInGame = Player.IsInGame,
                        OwnerId = Player.BattleTagHash,
                        IsInParty = Player.IsInParty,
                        NumPartymembers = Player.NumPlayersInParty,
                        IsLoadingWorld = Player.IsLoadingWorld,
                        ActorClass = Player.ActorClass,
                        ActorSnoId = Player.ActorId,
                        GameId = Player.CurrentGameId,
                        HitpointsCurrent = Player.HitpointsCurrent,
                        HitpointsMaxTotal = Player.HitpointsMaxTotal,
                        LevelAreaId = Player.LevelAreaId,
                        IsInTown = Player.LevelAreaId != 55313 && Player.IsInTown, // A2 Caldeum Bazaar
                        Position = Player.Position,
                        Destination = Player.Destination,
                        ProfilePosition = Player.GetProfilePosition(),
                        ProfileActorSno = Player.ProfileActorSno,
                        ProfilePathPrecision = Player.ProfilePathPrecision,
                        ProfileTagName = Player.GetProfileTagname(),
                        ProfileName = Path.GetFileName(ProfileManager.CurrentProfile.Path),
                        IsInCombat = Player.IsInCombat,
                        WorldSnoId = Player.CurrentWorldSnoId,
                        IsVendoring = BrainBehavior.IsVendoring,
                        BattleTagEncrypted = GetMyEncryptedBattleTag(),
                        RealIdNameEncrypted = GetMyEncryptedRealId(),
                        HeroName = Player.HeroName,
                        HeroId = Player.HeroId,
                        Events = EventManager.Events.Take(25).ToList(),
                        CurrentTarget = Player.Target,
                        IsInRift = Player.IsInRift,
                        IsInGreaterRift = Player.IsIsInGreaterRift,
                        IsParticipatingInGreaterRift = Player.IsParticipatingInGreaterRift,
                        FreeBackPackSlots = Player.FreeBackPackSlots,
                        JewelUpgradesleft = Player.JewelUpgradesleft,
                        IsDead = Player.IsDead,
                        BNetPartyMembers = ZetaDia.Service.Party.NumPartyMembers,
                        IsServer = Service.ConnectionMode == ConnectionMode.Server,
                        IsClient = Service.ConnectionMode == ConnectionMode.Client,
                        PreviousWorldSnoId = ChangeMonitor.LastWorldId,
                        LastPositionInPreviousWorld = ChangeMonitor.LastWorldPosition,
                        IsRequestingLeader = AutoFollow.CurrentBehavior.Category == BehaviorCategory.Leader,
                        IsQuickJoinEnabled = Player.IsQuickJoinEnabled,
                        LastPortalUsed = Player.LastPortalUsed,
                        BehaviorCategory = AutoFollow.CurrentBehavior.Category,
                        IsInBossEncounter = Player.IsInBossEncounter,
                        IsCastingTownPortal = Player.IsCastingTownPortal,
                        Strength = Player.Strength,
                        Vitality = Player.Vitality,
                        Intelligence = Player.Intelligence,
                        Dexterity = Player.Dexterity,   
                        Paragon = Player.Paragon,
                        SettingsCode = Player.SettingsCode,
                        FollowDistance = Settings.Coordination.CatchUpDistance,
                        CatchUpDistance = Settings.Coordination.CatchUpDistance,
                        Objective = AutoFollow.CurrentBehavior.Objective,
                        Build = Player.Build,
                        Routine = Player.Routine,
                    };
                }
                else if (ZetaDia.IsInGame && ZetaDia.IsLoadingWorld)
                {
                    m = new Message
                    {
                        Index = Player.Index,
                        IsInGame = true,
                        IsLoadingWorld = true,
                        GameId = Player.CurrentGameId,
                        OwnerId = Player.BattleTagHash,
                        HeroName = Player.HeroName,
                        HeroId = Player.HeroId,
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
                        IsQuickJoinEnabled = Player.IsQuickJoinEnabled,
                        BehaviorCategory = AutoFollow.CurrentBehavior.Category,
                        BattleTagEncrypted = GetMyEncryptedBattleTag(),
                        RealIdNameEncrypted = GetMyEncryptedRealId(),
                        IsInRift = RiftHelper.IsInRift,
                        ProfileTagName = Player.GetProfileTagname(),
                        ProfileName = Path.GetFileName(ProfileManager.CurrentProfile.Path),
                        Strength = Player.Strength,
                        Vitality = Player.Vitality,
                        Intelligence = Player.Intelligence,
                        Dexterity = Player.Dexterity,
                        Paragon = Player.Paragon,
                        SettingsCode = Player.SettingsCode,
                        WorldDynamicId = Player.CurrentDynamicWorldId,
                    };
                }
                else
                {
                    m = new Message
                    {
                        Index = Player.Index,
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
                        IsQuickJoinEnabled = Player.IsQuickJoinEnabled,
                        BehaviorCategory = AutoFollow.CurrentBehavior.Category,
                        BattleTagEncrypted = GetMyEncryptedBattleTag(),
                        RealIdNameEncrypted = GetMyEncryptedRealId(),
                        HeroName = Player.HeroName,
                        HeroId = Player.HeroId,
                        IsLoadingWorld = Player.IsLoadingWorld,
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




        private static string GetMyEncryptedRealId()
        {
            if (!Settings.Misc.IsRealIdEnabled)
                return string.Empty;

            return _myEncryptedRealId ??
                   (_myEncryptedRealId =
                       Crypto.EncryptToString(Common.CleanString(Settings.Misc.RealId)));
        }

        private static string GetMyEncryptedBattleTag()
        {
            return _myEncryptedBattleTag ??
                   (_myEncryptedBattleTag =
                       Crypto.EncryptToString(ZetaDia.Service.Hero.BattleTagName.Split('#').First()));
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
                ActorSnoId,
                ActorClass,
                HitpointsMaxTotal,
                HitpointsCurrent,
                "N/A", //GameId, // Possible identifying information.
                LastUpdated,
                IsVendoring,
                IsLoadingWorld,
                ProfileTagName,
                ActorClass,
                IsInParty,
                OwnerId,
                Events.Count,
                HeroAlias,
                CurrentTarget,
                "N/A" // BattleTagEncrypted // Possible identifying information.
                );
        }

        /// <summary>
        /// Checks if a text string is the same as an encrypted battle tag.
        /// </summary>
        internal static bool IsBattleTag(string name, string encryptedBattleTag)
        {
            return Crypto.EncryptToString(name) == encryptedBattleTag;
        }

        /// <summary>
        /// Checks if a text string is the same as an encrypted battle tag.
        /// </summary>
        internal static bool IsRealId(string name, string encryptedBattleTag)
        {
            if (!Settings.Misc.IsRealIdEnabled)
                return false;

            return Crypto.EncryptToString(name) == encryptedBattleTag;
        }

        public float Distance => Player.Position.Distance(Position);

        public string HeroAlias => Settings.Misc.HideHeroName ? HeroId.ToString() : HeroName;

        public string ShortSummary => string.Format(
            "[{0}] {1} ({2}) {3}{4}{5}{6}{7}{8}{9}{10} Age={11}ms Events={12}",
            OwnerId,
            HeroAlias,
            ActorClass,
            IsInGame ? "InGame " : "OutOfGame ",
            IsLoadingWorld ? "IsLoading " : string.Empty,
            IsInGame ? "InParty " : string.Empty,
            IsLeader ? "Leader " : string.Empty,
            IsFollower ? "Follower " : string.Empty,
            IsServer ? "Server " : string.Empty,
            IsClient ? "Client " : string.Empty,
            IsInGame ? $"World={WorldSnoId} Level={LevelAreaId}" : string.Empty,
            DateTime.UtcNow.Subtract(LastUpdated).TotalMilliseconds,
            Events.Count
            );

        public bool InDifferentLevelArea => Player.LevelAreaId != LevelAreaId;


        #region IPartyMember / ITargetable

        ActorClass IPartyMember.ActorClass => ActorClass;
        bool IPartyMember.IsLeader => IsLeader;
        bool IPartyMember.IsFollower => IsFollower;
        bool IPartyMember.IsInCombat => IsInCombat;
        bool IPartyMember.IsMe => IsMe;
        int IPartyMember.MemberId => OwnerId;
        PartyRole IPartyMember.Role => BehaviorCategory == BehaviorCategory.Leader ? PartyRole.Leader : PartyRole.Follower;
        float IPartyMember.LeashDistance => CatchUpDistance;
        PartyObjective IPartyMember.Objective => Objective;        
        ITargetable IPartyMember.Target => CurrentTarget;
        int IPartyMember.HeroId => HeroId;
        string ITargetable.Name => HeroName;
        int ITargetable.WorldDynamicId => WorldDynamicId;


        TrinityObjectType ITargetable.Type => TrinityObjectType.Player;

        #endregion
    }
}