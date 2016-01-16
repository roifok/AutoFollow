using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFollow.Behaviors;
using AutoFollow.Coroutines.Resources;
using AutoFollow.Networking;
using AutoFollow.Resources;
using Buddy.Coroutines;
using Zeta.Bot;
using Zeta.Bot.Coroutines;
using Zeta.Bot.Logic;
using Zeta.Bot.Navigation;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Actors.Gizmos;

namespace AutoFollow.Coroutines
{
    public class Movement
    {
        /// <summary>
        /// Moves to somewhere.
        /// </summary>
        /// <param name="location">where to move to</param>
        /// <param name="destinationName">name of location for debugging purposes</param>
        /// <param name="range">how close it should get</param>
        public static async Task<bool> MoveTo(Vector3 location, string destinationName = "", float range = 10f, Func<bool> stopCondition = null)
        {
            var distance = 0f;
            var name = string.IsNullOrEmpty(destinationName) ? location.ToString() : destinationName;

            Navigator.PlayerMover.MoveTowards(location);

            while (ZetaDia.IsInGame && (distance = location.Distance(ZetaDia.Me.Position)) >= range)
            {
                if (stopCondition != null && stopCondition())
                    break;

                if (ZetaDia.Me.IsDead || Navigator.StuckHandler.IsStuck)
                    break;

                Log.Verbose("Moving to {0} Distance={1}", name, distance);
                //await CommonCoroutines.MoveTo(location, name);
                //NavigateTo(location, name);
                await Navigator.MoveTo(location, name);
                await Coroutine.Yield();
            }

            if (distance <= range)
                Navigator.PlayerMover.MoveStop();

            Log.Verbose("MoveTo Finished. Distance={0}", distance);
            return true;
        }

        public async static Task<bool> MoveTo(ITargetable message, string destinationName, float range, Func<bool> stopCondition)
        {
            var distance = 0f;
            var location = message.Position;
            var name = string.IsNullOrEmpty(destinationName) ? location.ToString() : destinationName;

            Navigator.PlayerMover.MoveTowards(location);

            while (ZetaDia.IsInGame)
            {
                if (stopCondition != null && stopCondition())
                    break;

                if (ZetaDia.Me.IsDead || Navigator.StuckHandler.IsStuck)
                    break;

                var actor = Data.Players.FirstOrDefault(p => p.ACDId == message.AcdId);
                if (actor != null)
                {
                    location = actor.Position;
                }

                distance = location.Distance(ZetaDia.Me.Position);
                if (distance <= range)
                    break;

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
        /// Moves to something and interacts with it
        /// </summary>
        /// <param name="obj">object to interact with</param>
        /// <param name="range">how close to get</param>
        /// <param name="interactLimit">maximum number of times to interact</param>
        public static async Task<bool> MoveToAndInteract(DiaObject obj, float range = -1f, int interactLimit = 5)
        {
            if (!Data.IsValid(obj))
                return false;

            var startWorldSnoId = ZetaDia.CurrentWorldSnoId;

            if (interactLimit < 1) interactLimit = 5;
            if (range < 0) range = obj.CollisionSphere.Radius;
            
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

            if(!ZetaDia.IsLoadingWorld)
                obj.Interact();

            await Coroutine.Sleep(1000);

            if (obj is GizmoPortal && ZetaDia.IsLoadingWorld || ZetaDia.CurrentWorldSnoId != startWorldSnoId)
            {
                Log.Verbose("A portal was successfully interacted with");
                GameEvents.FireWorldTransferStart();
            }

            return true;
        }

        /// <summary>
        /// Moves to a position, finds actor by Id and interacts with it
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

        public static async Task<bool> MoveToPlayer(Message player, float range)
        {
            if (!ZetaDia.IsInGame || ZetaDia.Me.IsInCombat)
                return false;

            if (player.IsInSameGame && player.IsInSameWorld && player.Distance > range)
            {
                Log.Verbose("Moving to Player {0} CurrentDistance={1} DistanceRequired={2} ",
                    player.HeroName, player.Distance, range);
                
                await MoveTo(player, player.HeroName, range, () => !player.IsInSameWorld);
                return true;
            }

            return false;
        }

        public static async Task<bool> FollowThroughPortal(Message player)
        {
            if (!ZetaDia.IsInGame || ZetaDia.Me.IsInCombat)
                return false;


            if (player.LastPortalUsed == null || player.LastPortalUsed.IsWorldEntryPoint)
                return false;

            var notInSameWorldAsPortal = ZetaDia.CurrentWorldSnoId != player.LastPortalUsed.WorldSnoId;
            if (notInSameWorldAsPortal)
                return false;

            var myPosition = ZetaDia.Me.Position;

            Func<GizmoPortal, bool> distanceToMe = portal => portal.Position.Distance(myPosition) <= 200f;
            Func<GizmoPortal, bool> distanceToPortal = portal => portal.Position.Distance(player.LastPortalUsed.ActorPosition) <= 200f;
            Func<GizmoPortal, bool> matchingSNO = portal => portal.ActorSnoId == player.LastPortalUsed.ActorSnoId;

            var matches = ZetaDia.Actors.GetActorsOfType<GizmoPortal>(true).Where(i => distanceToMe(i) && distanceToPortal(i) && matchingSNO(i)).ToList();
            var match = matches.OrderBy(i => i.Distance).FirstOrDefault();
            if (match == null)
                return false;
           
            if (player.IsInSameGame && player.IsInSameWorld && player.Distance > 8f)
            {
                Log.Verbose("Following {0} through portal {0} CurrentDistance={1} DistanceRequired={2} ",
                    player.HeroName, match.Name, match.Distance, 8f);

                await MoveToAndInteract(match);                
                return true;
            }

            return false;
        }

        public static async Task<bool> FindExitPortal()
        {
            //Id=1471427315 MinimapTextureSnoId=102320 NameHash=1938876095 IsPointOfInterest=False IsPortalEntrance=False IsPortalExit=True IsWaypoint=False Location=x="372" y="1030" z="2"  Distance=101            

            if (AutoFollow.CurrentLeader.IsInSameWorld && AutoFollow.CurrentLeader.IsInRift)
                return false;

            var marker = Data.Markers.FirstOrDefault(m => m.MinimapTextureSnoId == 102320 && m.IsPortalExit);
            if (marker != null)
            {
                Log.Verbose("Exit Marker found! Id={0} Distance={1}",
                    marker.Id, marker.Position.Distance(Player.Instance.Position));

                await MoveTo(marker.Position, "Exit Marker", 15f);
                return true;
            }

            return false;
        }

        public static async Task<bool> UseNearbyRiftDeeperPortal()
        {
            if (!RiftHelper.IsInRift || AutoFollow.CurrentLeader.IsInSameWorld)
                return false;

            var nearbyPortal = Data.Portals.FirstOrDefault(p => p.Distance < 80f);
            if (nearbyPortal == null)
                return false;

            var portalExitMarker = Data.NearbyMarkers.FirstOrDefault(m => m.Position.Distance(nearbyPortal.Position) < 10f && m.IsPortalExit);
            if (portalExitMarker == null)
                return false;

            Log.Verbose("Assuming we should use this exit portal nearby: {0} Position={1} IsExit={2} NameHash={3} MinimapTex={4} CurrentDepth={5}",
                nearbyPortal.Name, nearbyPortal.Position, portalExitMarker.IsPortalExit,  portalExitMarker.NameHash, 
                portalExitMarker.MinimapTextureSnoId, RiftHelper.CurrentDepth);

            await MoveToAndInteract(nearbyPortal);
            return true;       
        }

        //public static async Task<bool> UseReturnPortalInTown()
        //{
        //    if (!RiftHelper.IsStarted || !Player.Instance.IsInTown || !AutoFollow.CurrentLeader.IsInRift || BrainBehavior.IsVendoring)
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

        public static async Task<bool> UseOpenRiftPortalInTown()
        {
            if (!RiftHelper.IsStarted || !Player.Instance.IsInTown || !AutoFollow.CurrentLeader.IsInRift || BrainBehavior.IsVendoring)
                return false;

            //ActorId: 191492, Type: Gizmo, Name: hearthPortal-46321, Distance2d: 6.981126, CollisionRadius: 8.316568, MinimapActive: 0, MinimapIconOverride: -1, MinimapDisableArrow: 0 
            var returnPortal = Data.Portals.FirstOrDefault(p => p.ActorSnoId == 191492);
            if (returnPortal != null && AutoFollow.CurrentLeader.IsInRift && !Player.Instance.IsVendoring)
            {
                Log.Info("Entering the return portal back to rift... ");
                await MoveToAndInteract(returnPortal);
            }

            var riftEntrancePortal = Data.Portals.FirstOrDefault(p =>
                p.ActorSnoId == 396751 || // Greater/Empowered Rift
                p.ActorSnoId == 345935); // Normal Rift        

            Log.Info("Entering the open rift... ");

            if (riftEntrancePortal == null)
                return false;

            // todo: add rift obelisk locations for each act and go there instead.
            await MoveTo(Town.Locations.KanaisCube);

            await MoveToAndInteract(riftEntrancePortal);
            return true;
        }

    }
}

