﻿using System;
using System.Linq;
using AutoFollow.Events;
using AutoFollow.Networking;
using Trinity.Framework;
using Trinity.Framework.Objects;
using Trinity.Routines;
using Zeta.Bot;
using Zeta.Bot.Logic;
using Zeta.Bot.Navigation;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Service;
using Zeta.Game.Internals.SNO;

namespace AutoFollow.Resources
{
    public static class Player
    {
        public static double PrimaryResourcePct { get; set; }
        public static DateTime LastSlowUpdate { get; set; }
        public static Vector3 Destination { get; set; }
        public static int Level { get; set; }
        public static int Paragon { get; set; }
        public static Target Target { get; set; }
        public static int Index { get; set; }
        public static int RActorId { get; set; }
        public static int AcdId { get; set; }
        public static double HitpointsCurrent { get; set; }
        public static double HitpointsMaxTotal { get; set; }
        public static double HitpointsCurrentPct { get; set; }
        public static int HeroId { get; set; }
        public static int ProfileActorSno { get; set; }
        public static float ProfilePathPrecision { get; set; }    
        public static string ProfileTagName { get; set; }
        public static Vector3 Position { get; set; }
        public static int CurrentLevelAreaId { get; set; }
        public static int CurrentWorldSnoId { get; set; }
        public static int CurrentDynamicWorldId { get; set; }
        public static bool IsInTown { get; set; }
        public static bool IsInGame { get; set; }
        public static DateTime LastUpdate { get; set; }
        public static bool IsValid => ZetaDia.Me.IsValid;
        public static bool IsVendoring { get; set; }
        public static ActorClass ActorClass { get; set; }
        public static int ActorId { get; set; }
        public static string HeroName { get; set; }
        public static bool IsInCombat { get; set; }
        public static GameId GameId { get; set; }
        public static bool IsLoadingWorld { get; set; }
        public static bool IsQuickJoinEnabled { get; set; }
        public static bool IsParticipatingInGreaterRift { get; set; }
        public static bool IsInRift { get; set; }
        public static Vector3 ProfilePosition { get; set; }
        public static bool IsTryingToCastPortalSpell { get; set; }
        public static bool IsCastingTownPortal { get; set; }
        public static bool IsDead { get; set; }
        public static DateTime LastCastTownPortal { get; set; }
        public static bool IsIsInGreaterRift { get; set; }
        public static int FreeBackPackSlots { get; set; }
        public static int JewelUpgradesleft { get; set; }
        public static GameId CurrentGameId { get; set; }
        public static Interactable LastPortalUsed { get; set; }
        public static Interactable LastEntryPortal { get; set; }
        public static bool IsIdle { get; set; }
        public static bool IsCasting { get; set; }
        public static bool IsInBossEncounter { get; set; }
        public static int Strength { get; set; }
        public static int Dexterity { get; set; }
        public static int Vitality { get; set; }
        public static int Intelligence { get; set; }


        public static int CachedLevelAreaId = -1;
        public static DateTime LastUpdatedLevelAreaId = DateTime.MinValue;
        private static bool _lastIsPartyLeader = false;
        private static DateTime _lastUpdateIsPartyLeaderMembers = DateTime.MinValue;
        private static int _lastNumPartyMembers = 0;
        private static DateTime _lastUpdateNumPartyMembers = DateTime.MinValue;                

        public static Message CurrentMessage = new Message();

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
                        pos.X = (float)pi.GetValue(currentBehavior, null);
                    if (pi.Name == "Y")
                        pos.Y = (float)pi.GetValue(currentBehavior, null);
                    if (pi.Name == "Z")
                        pos.Z = (float)pi.GetValue(currentBehavior, null);
                }
            }

            return pos;
        }

        public static int LevelAreaId
        {
            get
            {
                if (!ZetaDia.IsInGame)
                    return 0;
                if (ZetaDia.Globals.IsLoadingWorld)
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

        static Player()
        {
            Log.Debug("Creating Player Obj");
            LastUpdate = DateTime.MinValue;

            Core.Routines.Changed += RoutinesOnChanged;            
        }

        private static void RoutinesOnChanged(IRoutine newRoutine)
        {
            //Routine = ApiRoutineBuilder.CreateRoutine(); 
            //Build = ApiBuildBuilder.CreateBuild();
        }

        public static void UpdateOutOfGame()
        {
            var hero = ZetaDia.Service.Hero;
            HeroId = hero.HeroId;
            HeroName = Common.CleanString(hero.Name);
            IsLoadingWorld = ZetaDia.Globals.IsLoadingWorld || ZetaDia.Globals.IsPlayingCutscene;
            ActorClass = ZetaDia.Service.Hero.Class;
            IsInGame = false;
            Level = ZetaDia.Service.Hero.Level;
            Paragon = ZetaDia.Service.Hero.ParagonLevel;
            CurrentMessage = Message.GetMessage();            
        }

        public static void Update()
        {
            if (DateTime.UtcNow.Subtract(LastUpdate).TotalMilliseconds < 25)
                return;

            var shouldUpdate = ZetaDia.IsInGame && !ZetaDia.Globals.IsLoadingWorld && ZetaDia.Me != null;

            if (ZetaDia.Me != null && (!ZetaDia.Me.IsValid || !ZetaDia.Me.CommonData.IsValid || ZetaDia.Me.CommonData.IsDisposed))
                shouldUpdate = false;

            if (!Core.TrinityIsReady)
                shouldUpdate = false;

            //var playerData = ZetaDia.Storage.PlayerDataManager.ActivePlayerData;
            //if (!ZetaDia.IsInGame && (playerData == null || !playerData.IsValid))
            //    shouldUpdate = false;            

            if (shouldUpdate)
            {
                try
                {
                    //Index = playerData.Index;
                    RActorId = ZetaDia.Me.RActorId;
                    LastUpdate = DateTime.UtcNow;
                    IsInGame = ZetaDia.IsInGame;
                    AcdId = ZetaDia.Me.ACDId;
                    HitpointsCurrent = ZetaDia.Me.HitpointsCurrent;
                    HitpointsMaxTotal = ZetaDia.Me.HitpointsMaxTotal;
                    HitpointsCurrentPct = HitpointsMaxTotal > 0 ? ZetaDia.Me.HitpointsCurrent / ZetaDia.Me.HitpointsMaxTotal : 0;
                    PrimaryResourcePct = Core.Player.PrimaryResourcePct;
                    Position = Core.Player.Position;
                    CurrentLevelAreaId = LevelAreaId;
                    CurrentWorldSnoId = Core.Player.WorldSnoId;
                    CurrentDynamicWorldId = ZetaDia.Globals.WorldId;
                    IsInTown = Core.Player.IsInTown;
                    IsVendoring = BrainBehavior.IsVendoring;
                    ActorId = Core.Player.ActorSnoId;
                    ActorClass = ZetaDia.Service.Hero.Class;
                    Level = ZetaDia.Service.Hero.Level;
                    Paragon = ZetaDia.Me.ParagonLevel;
                    HeroName = Common.CleanString(ZetaDia.Service.Hero.Name);
                    HeroId = ZetaDia.Service.Hero.HeroId;
                    CurrentGameId = ZetaDia.Service.CurrentGameId;
                    IsInCombat = GetIsInCombat();
                    GameId = ZetaDia.Service.CurrentGameId;
                    IsLoadingWorld = ZetaDia.Globals.IsLoadingWorld || ZetaDia.Globals.IsPlayingCutscene;
                    IsQuickJoinEnabled = ZetaDia.SocialPreferences.QuickJoinEnabled;
                    IsInRift = RiftHelper.IsInRift;
                    IsIsInGreaterRift = RiftHelper.IsInGreaterRift;
                    IsParticipatingInGreaterRift = !ZetaDia.IsInTown && ZetaDia.Me.IsParticipatingInTieredLootRun;
                    IsTryingToCastPortalSpell = DateTime.UtcNow.Subtract(ChangeMonitor.LastCastPortalSpell).TotalSeconds < 10;
                    IsIdle = ChangeMonitor.IsIdle;
                    IsCasting = ZetaDia.Me.LoopingAnimationEndTime > 0;
                    IsCastingTownPortal = IsCasting && DateTime.UtcNow.Subtract(ChangeMonitor.LastCastTownPortal).TotalSeconds < 5;
                    Target = GetCurrentTarget();
                    ProfilePosition = GetProfilePosition();
                    ProfileActorSno = GetProfileActorSNO();
                    ProfilePathPrecision = GetProfilePathPrecision();
                    ProfileTagName = GetProfileTagname();
                    IsInBossEncounter = ZetaDia.Me.IsInBossEncounter;
                    JewelUpgradesleft = ZetaDia.Me.JewelUpgradesLeft;
                    FreeBackPackSlots = InventoryManager.NumFreeBackpackSlots;
                    IsDead = ZetaDia.Me.IsDead;
                    LastCastTownPortal = ChangeMonitor.LastCastTownPortal;
                    Strength = (int)ZetaDia.Me.Strength;
                    Intelligence = (int)ZetaDia.Me.Intelligence;
                    Dexterity = (int)ZetaDia.Me.Dexterity;
                    Vitality = (int)ZetaDia.Me.Vitality;

                    //var navigator = (Navigator.NavigationProvider as DefaultNavigationProvider);
                    //Destination = navigator?.CurrentPathDest ?? MathEx.GetPointAt(Position, 10f, ZetaDia.Me.Movement.Rotation);
                    Destination = MathEx.GetPointAt(Position, 10f, ZetaDia.Me.Movement.Rotation);
                }
                catch (Exception ex)
                {
                    Log.Verbose("Exception {0}", ex);
                }
            }

            //if (DateTime.UtcNow.Subtract(LastSlowUpdate).TotalSeconds > 30)
            //{
            //    SettingsCode = Trinity.Settings.SettingsManager.GetCurrrentSettingsExportCode();
            //    LastSlowUpdate = DateTime.UtcNow;
            //}
            
            CurrentMessage = Message.GetMessage();
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

        public static bool GetIsInCombat()
        {
            if (CombatTargeting.Instance == null)
                return false;

            if (CombatTargeting.Instance.FirstObject == null)
                return false;

            if (CombatTargeting.Instance.FirstObject == null)
                return false;

            if (ZetaDia.Me.IsInCombat)
                return true;

            if (CombatTargeting.Instance.FirstObject.IsValid && CombatTargeting.Instance.FirstObject.ActorType == ActorType.Monster)
                return true;

            return false;
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
                    pathPrecision = (float)val;
            }
            return pathPrecision;
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
                id = (int)pi.GetValue(currentBehavior, null);
            }

            return id;
        }

        public static Target GetCurrentTarget()
        {
            try
            {
                return new Target(CombatTargeting.Instance.Provider.GetObjectsByWeight().FirstOrDefault());
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("Only part of a ReadProcessMemory"))
                    throw;
            }
            return new Target();
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

        public static string GetString()
        {            
            return string.Format("Player ({12} - {13}): RActorId={0} AcdId={1} HitpointsCurrent={2:0} HitpointsCurrentPct={3:0} HitpointsMaxTotal={4:0} Position={5} LevelAreaId={6} WorldSnoId={7} DynamicWorldId={8} IsInGame={9} IsInTown={10} IsVendoring: {11}",
                RActorId, AcdId, HitpointsCurrent, HitpointsCurrentPct*100, HitpointsMaxTotal, Position, CurrentLevelAreaId, CurrentWorldSnoId, CurrentDynamicWorldId, IsInGame, IsInTown, IsVendoring, string.Empty, AutoFollow.CurrentBehavior.Category);
        }

        public static int BattleTagHash => ZetaDia.Service.Hero.BattleTagName.GetHashCode();

        public static int NumPlayersInParty
        {
            get
            {
                if (!ZetaDia.Service.IsValid)
                    return _lastNumPartyMembers;

                if (!ZetaDia.Service.Hero.IsValid)
                    return _lastNumPartyMembers;

                if (ZetaDia.Globals.IsLoadingWorld)
                    return _lastNumPartyMembers;

                if (DateTime.UtcNow.Subtract((DateTime) _lastUpdateNumPartyMembers).TotalSeconds < 5)
                    return _lastNumPartyMembers;

                if (ZetaDia.Service.IsValid &&
                    ZetaDia.Service.Platform.IsValid &&
                    //ZetaDia.Service.Platform.IsConnected &&
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

        //public static bool IsPartyleader
        //{
        //    get
        //    {
        //        if (!ZetaDia.Service.IsValid)
        //            return false;
        //        if (!ZetaDia.Service.Hero.IsValid)
        //            return false;
        //        if (ZetaDia.Globals.IsLoadingWorld)
        //            return false;

        //        if (DateTime.UtcNow.Subtract((DateTime) _lastUpdateIsPartyLeaderMembers).TotalSeconds < 5)
        //            return _lastIsPartyLeader;

        //        if (ZetaDia.Service.IsValid &&
        //            ZetaDia.Service.Platform.IsValid &&
        //            ZetaDia.Service.Platform.IsConnected &&
        //            ZetaDia.Service.Hero.IsValid &&
        //            ZetaDia.Service.Party.IsValid)
        //        {

        //            _lastUpdateIsPartyLeaderMembers = DateTime.UtcNow;
        //            _lastIsPartyLeader = ZetaDia.Service.Party.IsPartyLeader;
        //            return _lastIsPartyLeader;
        //        }
        //        return false;
        //    }
        //}

        public static bool IsInParty => NumPlayersInParty > 1;

        public static bool IsClient => Service.ConnectionMode == ConnectionMode.Client;

        public static bool IsServer => Service.ConnectionMode == ConnectionMode.Server;

        public static bool IsLeader => AutoFollow.CurrentLeader != null && CurrentMessage != null && CurrentMessage.OwnerId == AutoFollow.CurrentLeader.OwnerId;

        public static bool IsFollower => !IsLeader;

        public static string SettingsCode { get; set; }

        //public static ApiRoutine Routine { get; set; }

        //public static ApiBuild Build { get; set; }

    }
}
