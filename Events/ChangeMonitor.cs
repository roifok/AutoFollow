using System;
using System.Collections.Generic;
using System.Linq;
using AutoFollow.Networking;
using AutoFollow.Resources;
using Zeta.Bot;
using Zeta.Bot.Logic;
using Zeta.Bot.Navigation;
using Zeta.Common;
using Zeta.Game;

namespace AutoFollow.Events
{
    public static class ChangeMonitor
    {
        private static int _worldId;
        private static int _levelAreaId;
        private static Target _currentTarget;
        private static readonly HashSet<int> EngagedMonsters = new HashSet<int>();
        private static DateTime _lastChecked = DateTime.MinValue;
        private static bool _isDead;
        private static bool _inTrouble;

        static ChangeMonitor()
        {
            GameEvents.OnGameJoined += GameEvents_OnGameJoined;
            GameEvents.OnGameLeft += GameEvents_OnGameLeft;
            GameEvents.OnPlayerDied += GameEvents_OnPlayerDied;
            GameEvents.OnWorldChanged += GameEvents_OnWorldChanged;
            GameEvents.OnWorldTransferStart += GameEvents_OnWorldTransferStart;
        }

        private static void GameEvents_OnWorldTransferStart(object sender, EventArgs e)
        {
            if (ZetaDia.IsInTown) 
                return;

            Log.Info("World Transfer Start Fired!");

            var portal = Data.Portals.OrderByDescending(p => p.Distance).FirstOrDefault();
            if (portal != null)
            {
                var interactable = new Interactable(portal);
                Log.Info("Recording Last Portal Used as {0} WorldSnoId={1}", interactable.InternalName, interactable.WorldSnoId);
                Player.LastPortalUsed = interactable;
                EventManager.FireEvent(new EventData(EventType.UsedPortal, null, interactable));
            }
        }

        //private static void GameEvents_OnWorldTransferStart(object sender, EventArgs e)
        //{
        //    if (ZetaDia.IsInTown) // todo Detect if no portal was used / town portal spell
        //        return;

        //    // Look at distance to our last known position in previous world to figure out 
        //    // which of the portals we have been tracking was the one we used.

        //    //var lastPositionInWorld = BotHistory.PositionCache.GetLastPositionInWorld();
        //    //var closeEnoughPortals = BotHistory.PortalHistory.Values.Where(p => p.ActorPosition.Distance(lastPositionInWorld) < 15f);
        //    //var orderedPortals = closeEnoughPortals.OrderByDescending(p => p.ActorPosition.Distance(lastPositionInWorld));
        //    //orderedPortals.ForEach(p => Log.Verbose(p.ToString() + " DistanceInPreviousWorld=" + p.ActorPosition.Distance(lastPositionInWorld)));

        //    Log.Info("World Transfer Start Fired!");

        //    var portal = Data.Portals.OrderByDescending(p => p.Distance).FirstOrDefault();
        //    if (portal != null)
        //    {
        //        var interactable = BotHistory.PortalHistory.FirstOrDefault(p => p.Value.AcdId == portal.ACDId);
        //        if (interactable.Value != null)
        //        {
        //            Log.Info("Recording Last Portal Used as {0} WorldSnoId={1}", interactable.Value.InternalName, interactable.Value.WorldSnoId);
        //            Player.LastPortalUsed = interactable.Value;
        //            EventManager.FireEvent(new EventData(EventType.UsedPortal, null, interactable.Value));
        //        }
        //        //var interactable = new Interactable(portal);

        //    }
        //}


        //private static void GameEvents_OnWorldChanged(object sender, EventArgs e)
        //{
        //    var portal = Data.Portals.OrderByDescending(p => p.Distance).FirstOrDefault();
        //    if (portal != null)
        //    {
        //        //var interactable = new Interactable(portal);                
        //        var interactable = BotHistory.PortalHistory.FirstOrDefault(p => p.Value.AcdId == portal.ACDId);
        //        if (interactable.Value != null)
        //        {
        //            Log.Info("Processing portal relationship for {0} in WorldSnoId={1}", interactable.Value.InternalName, interactable.Value.WorldSnoId);

        //            if (Player.LastPortalUsed != null)
        //            {
        //                interactable.Value.EntryPortal = Player.LastPortalUsed;
        //                Player.LastPortalUsed.ExitPortal = interactable.Value;
        //            }
        //        }

        //        EventManager.FireEvent(new EventData(EventType.UsedPortal, null, interactable));
        //    }
        //}

        private static void GameEvents_OnWorldChanged(object sender, EventArgs e)
        {
            var portal = Data.Portals.OrderByDescending(p => p.Distance).FirstOrDefault();
            if (portal != null)
            {
                //var interactable = new Interactable(portal);                
                var interactable = BotHistory.PortalHistory.FirstOrDefault(p => p.Value.AcdId == portal.ACDId);
                if (interactable.Value != null)
                {
                    Log.Info("Processing portal relationship for {0} in WorldSnoId={1}", interactable.Value.InternalName, interactable.Value.WorldSnoId);

                    Player.LastEntryPortal = interactable.Value;

                    //if (Player.LastPortalUsed != null)
                    //{
                    //    interactable.Value.EntryPortal = Player.LastPortalUsed;
                    //    Player.LastPortalUsed.ExitPortal = interactable.Value;
                    //}
                }

                EventManager.FireEvent(new EventData(EventType.UsedPortal, null, interactable));

                // There's an issue after zoning into a new area the navigator doesnt seem to want to work.
                // After moving the char manually a bit away from the portal it kicks in.
                Navigator.PlayerMover.MoveTowards(MathEx.GetPointAt(portal.Position, 20f, portal.Movement.Rotation));
            }            
        }

        private static void GameEvents_OnPlayerDied(object sender, EventArgs e)
        {
            EventManager.FireEvent(new EventData(EventType.Died));
        }

        private static void GameEvents_OnGameLeft(object sender, EventArgs e)
        {
            EventManager.FireEvent(new EventData(EventType.LeftGame));
        }

        private static void GameEvents_OnGameJoined(object sender, EventArgs e)
        {
            EventManager.FireEvent(new EventData(EventType.JoinedGame));
        }


        public static void CheckForChanges()
        {
            if (!ZetaDia.IsInGame || ZetaDia.Me == null || DateTime.UtcNow.Subtract(_lastChecked).TotalMilliseconds < 250)
                return;
            
            if (Player.IsServer)
            {
                //todo this doesn't belong here, move it.

                var currentBehavior = ProfileManager.CurrentProfileBehavior;
                if (currentBehavior != null)
                {
                    if (currentBehavior.GetType().Name.ToLower().Contains("town"))
                        AutoFollow.ServerMessage.IsInTown = true;

                    if (currentBehavior.GetType().Name.ToLower().Contains("leavegame"))
                        AutoFollow.ServerMessage.IsInGame = false;

                    AutoFollow.ServerMessage.ProfilePosition = Message.GetProfilePosition();
                    AutoFollow.ServerMessage.ProfileActorSno = Message.GetProfileActorSNO();
                }

                if (BrainBehavior.IsVendoring)
                    AutoFollow.ServerMessage.IsInTown = true;
            }

            var worldId = ZetaDia.CurrentWorldSnoId;
            if (ZetaDia.WorldInfo.IsValid && worldId != _worldId && worldId != 0)
            {                
                EventManager.FireEvent(new EventData(EventType.WorldAreaChanged, _worldId, worldId));
                _worldId = worldId;
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

            //var currentTarget = Player.Instance.Message.CurrentTarget;
            //if (currentTarget != null && !EngagedMonsters.Contains(currentTarget.AcdId))
            //{
            //    if (currentTarget.Quality >= MonsterQuality.Champion)
            //    {
            //        EventManager.FireEvent(new EventData(EventType.EngagedSpecialMonster, null, currentTarget));
            //        EngagedMonsters.Add(currentTarget.AcdId);
            //    }
            //    _currentTarget = currentTarget;
            //}

            //if (EngagedMonsters.Count > 100)
            //    EngagedMonsters.Remove(EngagedMonsters.First());
        }


    }
}
