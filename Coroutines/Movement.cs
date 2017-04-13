using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFollow.Behaviors;
using AutoFollow.Events;
using AutoFollow.Networking;
using AutoFollow.Resources;
using Buddy.Coroutines;
using Trinity.Components.Combat.Resources;
using Trinity.Framework;
using Zeta.Bot;
using Zeta.Bot.Coroutines;
using Zeta.Bot.Logic;
using Zeta.Bot.Navigation;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Actors.Gizmos;
using Zeta.Game.Internals.SNO;

namespace AutoFollow.Coroutines
{
    public class Movement
    {
        /// <summary>
        /// Moves to a world position 
        /// (naive, blocks execution, avoid using while in combat)
        /// </summary>
        /// <param name="location">where to move to</param>
        /// <param name="destinationName">name of location for debugging purposes</param>
        /// <param name="range">how close it should get</param>
        public static async Task<bool> MoveTo(Vector3 location, string destinationName = "", float range = 10f, Func<bool> stopCondition = null)
        {
            var distance = 0f;
            var name = string.IsNullOrEmpty(destinationName) ? location.ToString() : destinationName;

            Navigator.PlayerMover.MoveTowards(location);

            var startingWorldSnoId = ZetaDia.Globals.WorldSnoId;

            while (ZetaDia.IsInGame && (distance = location.Distance(ZetaDia.Me.Position)) >= range)
            {
                if (stopCondition != null && stopCondition())
                    break;

                if (ZetaDia.Me.IsDead || Navigator.StuckHandler.IsStuck)
                    break;

                if (ZetaDia.Globals.WorldSnoId != startingWorldSnoId)
                    break;

                if (Navigation.IsBlocked)
                {
                    Log.Verbose("Movement Failed, It looks like we're blocked!", name, distance);
                    break;
                }
                    
                Log.Verbose("Moving to {0} Distance={1}", name, distance);
                await Navigator.MoveTo(location, name);
                await Coroutine.Yield();
            }

            if (distance <= range)
                Navigator.PlayerMover.MoveStop();

            Log.Verbose("MoveTo Finished. Distance={0}", distance);
            return true;
        }

        /// <summary>
        /// Moves to an actor 
        /// (blocks execution but heavily validated, combat use OK)
        /// </summary>
        /// <param name="targetableProducer">returns an updated ITargetable object</param>
        /// <param name="destinationName">friendly name of destination for logging purposes</param>
        /// <param name="range">acceptable distance to destination</param>
        /// <param name="stopCondition">optional condition that would cause movement to abort</param>
        /// <returns></returns>
        public async static Task<bool> MoveTo(Func<ITargetable> targetableProducer, string destinationName, float range, Func<ITargetable, bool> stopCondition)
        {
            if (targetableProducer == null)
                return false;

            var target = targetableProducer();
            var distance = target.Distance;
            var name = string.IsNullOrEmpty(destinationName) ? "Unknown" : destinationName;
            var destination = target.Position;
            var acdId = target.AcdId;

            Navigator.PlayerMover.MoveTowards(destination);

            var i = 0;

            while (true)
            {
                if (!ZetaDia.IsInGame)
                {
                    Log.Verbose("Movement Failed, we're no longer in game!", name, distance);
                    break;
                }

                if (ZetaDia.Me.IsDead)
                {
                    Log.Verbose("Movement Failed, we're dead!", name, distance);
                    break;
                }

                if (Navigator.StuckHandler.IsStuck)
                {
                    Log.Verbose("Movement Failed, It looks like we're stuck!", name, distance);
                    break;
                }

                if (Navigation.IsBlocked)
                {
                    Log.Verbose("Movement Failed, It looks like we're blocked!", name, distance);
                    break;
                }
                
                var actor = Data.Actors.FirstOrDefault(p => p.ACDId == acdId);
                target = targetableProducer();

                if (target == null && actor == null)
                {
                    Log.Verbose("Movement failed, Target not found", name, distance);
                    return false;
                }

                if (stopCondition != null && stopCondition(target ?? new Target()))
                    break;

                if (target != null && target.WorldDynamicId != Player.CurrentDynamicWorldId)
                {
                    Log.Verbose("Movement Failed, Target is in a different world", name, distance);
                    return false;
                }

                if (Player.IsIdle && !Navigation.CanRayCast(Player.Position, destination))
                {
                    Log.Verbose("Movement Failed, Unable to reach destination", name, distance);
                    return false;
                }

                if (EventManager.IsExecutionBreakRequested)
                {
                    Log.Verbose("Movement Failed, Eventmanager requested execution to finish", name, distance);
                    return false;
                }

                if (Targetting.RoutineWantsToLoot())
                {
                    Log.Verbose("Movement Stopped, Combat routine wants to pick up an item.", name, distance);
                    return false;
                }

                if (Settings.Combat.AllowAvoidance && Core.Avoidance.Avoider.ShouldAvoid)
                {
                    Log.Verbose("Movement Stopped, Trinity wants to Avoid.", name, distance);
                    return false;
                }

                if (Settings.Combat.AllowKiting && Core.Avoidance.Avoider.ShouldKite)
                {
                    Log.Verbose("Movement Stopped, Trinity wants to Kite.", name, distance);
                    return false;
                }

                if (Targetting.RoutineWantsToClickGizmo())
                {
                    Log.Verbose("Movement Stopped, Combat routine wants to click on a gizmo.", name, distance);
                    return false;
                }

                destination = actor?.Position ?? target.Position;
                distance = destination.Distance(ZetaDia.Me.Position);

                if (distance <= range)
                    break;
                
                Log.Verbose("Moving to {0} Distance={1} (Dynamic) ({2})", name, distance, i);

                if (distance < 30f && Navigator.Raycast(Player.Position, destination))
                {
                    Navigator.PlayerMover.MoveTowards(destination);
                }
                else
                {
                    var result = await Navigator.MoveTo(destination, name);
                    switch (result)
                    {
                        case MoveResult.Failed:
                        case MoveResult.PathGenerationFailed:
                            Log.Verbose($"Failed Move Result = {result}");
                            break;
                    }
                }
                await Coroutine.Yield();

                i++;
            }

            if (distance <= range)
            {
                Navigator.PlayerMover.MoveStop();
                return true;
            }

            Log.Verbose("MoveTo Finished. Distance={0}", distance);
            return true;
        }

        /// <summary>
        /// Moves to something and interacts with it. 
        /// (blocks execution, avoid using while in combat)
        /// </summary>
        /// <param name="obj">object to interact with</param>
        /// <param name="range">how close to get</param>
        /// <param name="interactLimit">maximum number of times to interact</param>
        public static async Task<bool> MoveToAndInteract(DiaObject obj, float range = -1f, int interactLimit = 5)
        {
            if (!Data.IsValid(obj))
                return false;

            var startWorldSnoId = ZetaDia.Globals.WorldSnoId;

            if (interactLimit < 1) interactLimit = 5;
            if (range < 0) range = obj.CollisionSphere.Radius - 2;
            
            if (obj.Position == default(Vector3))
            {
                Log.Verbose("Destination is invalid (Vector3.Zero)");
            }

            if (obj.Position.Distance(ZetaDia.Me.Position) > 600f)
            {
                Log.Verbose("Destination is too far away");
            }

            if (obj.Position.Distance(ZetaDia.Me.Position) > range)
            {
                if (!await MoveTo(obj.Position, obj.Name, range))
                    return false;
            }
            
            var distance = obj.Position.Distance(ZetaDia.Me.Position);
            if (distance <= range || distance - obj.CollisionSphere.Radius <= range)
            {
                for (int i = 1; i <= interactLimit; i++)
                {
                    Log.Verbose("Interacting with {0} ({1}) Attempt={2}", obj.Name, obj.ActorSnoId, i);
                    if (obj.Interact())
                        break;

                    await Coroutine.Sleep(1000);
                    await Coroutine.Yield();
                }
            }           

            Navigator.PlayerMover.MoveTowards(obj.Position);
            await Coroutine.Sleep(250);

            if(!ZetaDia.Globals.IsLoadingWorld && Data.IsValid(obj))
                obj.Interact();

            await Coroutine.Sleep(1000);

            if (obj is GizmoPortal && ZetaDia.Globals.IsLoadingWorld || ZetaDia.Globals.WorldSnoId != startWorldSnoId)
            {
                Log.Verbose("A portal was successfully interacted with");
            }

            return true;
        }

        /// <summary>
        /// Moves to a position, finds actor by Id and interacts with it 
        /// (blocks execution, avoid using while in combat)
        /// </summary>
        /// <param name="actorId">id of actor to interact with</param>
        /// <param name="range">how close to get</param>
        /// <param name="position">position from which to interact</param>
        /// <param name="interactLimit">maximum number of times to interact</param>
        public static async Task<bool> MoveToAndInteract(Vector3 position, int actorId, float range = -1f, int interactLimit = 5)
        {
            if (position == Vector3.Zero)
                return false;

            if (interactLimit < 1) interactLimit = 5;
            if (range < 0) range = 2f;

            if (position.Distance(ZetaDia.Me.Position) > range)
            {
                if (!await MoveTo(position, position.ToString()))
                    return false;
            }

            var actor = Data.Actors.FirstOrDefault(a => a.ActorSnoId == actorId);
            if (actor == null)
            {
                Log.Verbose("Interaction Failed: Actor not found with Id={0}", actorId);
                return false;
            }

            var distance = position.Distance(ZetaDia.Me.Position);
            if (distance <= range || distance - actor.CollisionSphere.Radius <= range)
            {
                for (int i = 1; i <= interactLimit; i++)
                {
                    Log.Verbose("Interacting with {0} ({1}) Attempt={2}", actor.Name, actor.ActorSnoId, i);
                    if (actor.Interact())
                        break;

                    await Coroutine.Sleep(100);
                    await Coroutine.Yield();
                }
            }

            Navigator.PlayerMover.MoveTowards(actor.Position);
            await Coroutine.Sleep(250);
            actor.Interact();
            return true;
        }

        /// <summary>
        /// Move to another bot
        /// </summary>
        /// <param name="player">a player to move to</param>
        /// <param name="range">the closeness required</param>
        public static async Task<bool> MoveToPlayer(Message player, float range)
        {
            if (!ZetaDia.IsInGame || !player.IsInSameGame || !player.IsInSameWorld)
                return false;

            if (player.Distance > range)
            {
                Log.Verbose("Moving to Player {0} CurrentDistance={1} DistanceRequired={2} ",
                    player.HeroAlias, player.Distance, range);

                await MoveTo(() => AutoFollow.GetUpdatedMessage(player), player.HeroAlias, range,
                    t =>
                    {
                        if (!player.IsInSameWorld)
                            return true;

                        if (t.Distance > Settings.Coordination.TeleportDistance && !RiftHelper.IsInGreaterRift)
                            return true;

                        if (t.Distance < Settings.Coordination.CatchUpDistance && Targetting.RoutineWantsToAttackUnit())
                            return true;

                        return false;
                    });

                return true;
            }

            Log.Debug("Player {0} is close enough CurrentDistance={1} DistanceRequired={2} ",
                player.HeroAlias, player.Distance, range);

            return true;
        }

        /// <summary>
        /// Finds the marker for a greater rift exit and follows it.
        /// </summary>
        public static async Task<bool> MoveToGreaterRiftExitPortal()
        {
            //Id=1471427315 MinimapTextureSnoId=102320 NameHash=1938876095 IsPointOfInterest=False IsPortalEntrance=False IsPortalExit=True IsWaypoint=False Location=x="372" y="1030" z="2"  Distance=101            

            var markerExitTextures = new HashSet<int>
            {
                102320,
                215744
            };

            if (AutoFollow.CurrentLeader.IsInGreaterRift && (!AutoFollow.CurrentLeader.IsInSameWorld || AutoFollow.CurrentLeader.Distance > 100f))
            {
                var marker = Data.Markers.FirstOrDefault(m => markerExitTextures.Contains(m.MinimapTextureSnoId) || m.IsPortalExit);
                if (marker != null)
                {
                    // Level 1 entrance from town
                    //Id = -885252509 MinimapTextureSnoId = 215746 NameHash = -1464312746 IsPointOfInterest = False IsPortalEntrance = False IsPortalExit = False IsWaypoint = False Location = x = "1074" y = "601" z = "-2"  Distance = 16

                    //Level 1 Exit
                    // Id = 820810589 MinimapTextureSnoId = 215744 NameHash = 1938876094 IsPointOfInterest = False IsPortalEntrance = False IsPortalExit = False IsWaypoint = False Location = x = "375" y = "602" z = "-1"  Distance = 683

                    // level 2 entrance
                    //Id = -1896858840 MinimapTextureSnoId = 102321 NameHash = 1938876093 IsPointOfInterest = False IsPortalEntrance = True IsPortalExit = False IsWaypoint = False Location = x = "280" y = "310" z = "-3"  Distance = 2

                    // Level 2 exit
                    //Id = 1834932994 MinimapTextureSnoId = 102320 NameHash = 1938876095 IsPointOfInterest = False IsPortalEntrance = False IsPortalExit = True IsWaypoint = False Location = x = "480" y = "691" z = "-3"  Distance = 430

                    // level 3 entrance
                    //Id = 851042175 MinimapTextureSnoId = 102321 NameHash = 1938876094 IsPointOfInterest = False IsPortalEntrance = True IsPortalExit = False IsWaypoint = False Location = x = "311" y = "281" z = "-3"  Distance = 1

                    // level 3 exit
                    //Id = -1689941019 MinimapTextureSnoId = 102320 NameHash = 1938876096 IsPointOfInterest = False IsPortalEntrance = False IsPortalExit = True IsWaypoint = False Location = x = "480" y = "691" z = "-3"  Distance = 444

                    // level 4 entrance
                    //Id = -780668287 MinimapTextureSnoId = 102321 NameHash = 1938876095 IsPointOfInterest = False IsPortalEntrance = True IsPortalExit = False IsWaypoint = False Location = x = "311" y = "481" z = "-3"  Distance = 1

                    Log.Verbose("Exit Marker found! Id={0} Distance={1}",
                        marker.Id, marker.Position.Distance(Player.Position));

                    await MoveTo(marker.Position, "Exit Marker", 15f);
                    return true;
                }
            }

            return false;
        }



    }
}

//public static async Task<bool> FollowThroughPortal(Message player)
//{
//    if (!ZetaDia.IsInGame || ZetaDia.Me.IsInCombat)
//        return false;


//    if (player.LastPortalUsed == null || player.LastPortalUsed.IsWorldEntryPoint)
//        return false;

//    var notInSameWorldAsPortal = ZetaDia.CurrentWorldSnoId != player.LastPortalUsed.WorldSnoId;
//    if (notInSameWorldAsPortal)
//        return false;

//    var myPosition = ZetaDia.Me.Position;

//    Func<GizmoPortal, bool> distanceToMe = portal => portal.Position.Distance(myPosition) <= 200f;
//    Func<GizmoPortal, bool> distanceToPortal = portal => portal.Position.Distance(player.LastPortalUsed.ActorPosition) <= 200f;
//    Func<GizmoPortal, bool> matchingSNO = portal => portal.ActorSnoId == player.LastPortalUsed.ActorSnoId;

//    var matches = ZetaDia.Actors.GetActorsOfType<GizmoPortal>(true).Where(i => distanceToMe(i) && distanceToPortal(i) && matchingSNO(i)).ToList();
//    var match = matches.OrderBy(i => i.Distance).FirstOrDefault();
//    if (match == null)
//        return false;

//    if (player.IsInSameGame && player.IsInSameWorld && player.Distance > 8f)
//    {
//        Log.Verbose("Following {0} through portal {0} CurrentDistance={1} DistanceRequired={2} ",
//            player.HeroAlias, match.Name, match.Distance, 8f);

//        await MoveToAndInteract(match);                
//        return true;
//    }

//    return false;
//}


//public static async Task<bool> UseReturnPortalInTown()
//{
//    if (!RiftHelper.IsStarted || !Player.IsInTown || !AutoFollow.CurrentLeader.IsInRift || BrainBehavior.IsVendoring)
//        return false;

//    var riftEntrancePortal = Data.Portals.FirstOrDefault(p =>
//        p.ActorSnoId == 396751 || // Greater/Empowered Rift
//        p.ActorSnoId == 345935); // Normal Rift        

//    Log.Info("Entering the open rift... ");

//    if (riftEntrancePortal == null)
//        return false;

//    await MoveToAndInteract(riftEntrancePortal);
//    return true;
//}

//public static async Task<bool> UseNearbyRiftDeeperPortal()
//{
//    if (!RiftHelper.IsInRift || AutoFollow.CurrentLeader.IsInSameWorld)
//        return false;

//    var nearbyPortal = Data.Portals.FirstOrDefault(p => p.Distance < 80f);
//    if (nearbyPortal == null)
//        return false;

//    var portalExitMarker = Data.NearbyMarkers.FirstOrDefault(m => m.Position.Distance(nearbyPortal.Position) < 10f && m.IsPortalExit);
//    if (portalExitMarker == null)
//        return false;

//    Log.Verbose("Assuming we should use this exit portal nearby: {0} Position={1} IsExit={2} NameHash={3} MinimapTex={4} CurrentDepth={5}",
//        nearbyPortal.Name, nearbyPortal.Position, portalExitMarker.IsPortalExit,  portalExitMarker.NameHash, 
//        portalExitMarker.MinimapTextureSnoId, RiftHelper.CurrentDepth);

//    await MoveToAndInteract(nearbyPortal);
//    return true;       
//}