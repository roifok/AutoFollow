using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Adventurer.Coroutines;
using Adventurer.Coroutines.RiftCoroutines;
using AutoFollow.Coroutines.Resources;
using AutoFollow.Resources;
using Buddy.Coroutines;
using Zeta.Bot.Coroutines;
using Zeta.Bot.Logic;

namespace AutoFollow.Coroutines
{
    public class Questing
    {
        private static DateTime LastRequestedGemUpgrade = DateTime.MinValue;

        private static ICoroutine _gemUpgrader = new UpgradeGemsCoroutine();

        /// <summary>
        /// Run adventurer Gem Upgrade coroutine
        /// </summary>
        public static async Task<bool> UpgradeGems()
        {
            if (DateTime.UtcNow.Subtract(LastRequestedGemUpgrade).TotalSeconds < 10)
                return false;

            if (RiftHelper.IsInRift && RiftHelper.RiftQuest.Step == RiftQuest.RiftStep.UrshiSpawned && RiftHelper.CurrentRift.IsCompleted)
            {
                if (AutoFollow.CurrentLeader.Distance > 150f)
                    await Coordination.TeleportToPlayer(AutoFollow.CurrentLeader);

                Log.Warn("Rift is Completed; requesting gem upgrade from other plugins.");

                while (await _gemUpgrader.GetCoroutine() == false)
                {
                    await Coroutine.Yield();
                }

                _gemUpgrader.Reset();

                await Coroutine.Sleep(5000);
                LastRequestedGemUpgrade = DateTime.UtcNow;
                return true;
            }

            return false;            
        }

        /// <summary>
        /// Move to orek and interact with him.
        /// </summary>
        public static async Task<bool> TalkToOrek()
        {
            if (!Player.IsVendoring && Player.IsInTown && RiftHelper.IsGreaterRiftProfile && RiftHelper.RiftQuest.Step == RiftQuest.RiftStep.Cleared)
            {
                if(!await Movement.MoveToAndInteract(Town.Actors.Orek))
                    return false;

                return true;
            }
            return false;
        }

        /// <summary>
        /// Move to orek and interact with him.
        /// </summary>
        public static async Task<bool> LeaveRiftWhenDone()
        {
            if (RiftHelper.IsInRift && RiftHelper.RiftQuest.Step == RiftQuest.RiftStep.Cleared)
            {
                if (!await CommonCoroutines.UseTownPortal("Rift Finished"))
                    return false;

                return true;
            }
            return false;
        }

        /// <summary>
        /// If we're participating in a greater rift, use a return portal or entrance portal to get back into it.
        /// </summary>
        public static async Task<bool> ReturnToGreaterRift()
        {
            if (!RiftHelper.IsStarted || !Player.IsInTown || !AutoFollow.CurrentLeader.IsInGreaterRift || BrainBehavior.IsVendoring || !Player.IsIdle)
                return false;

            //ActorId: 191492, Type: Gizmo, Name: hearthPortal-46321, Distance2d: 6.981126, CollisionRadius: 8.316568, MinimapActive: 0, MinimapIconOverride: -1, MinimapDisableArrow: 0 
            var returnPortal = Data.Portals.FirstOrDefault(p => p.ActorSnoId == 191492);
            if (returnPortal != null && AutoFollow.CurrentLeader.IsInRift && !Player.IsVendoring)
            {
                Log.Info("Entering the return portal back to rift... ");
                await Movement.MoveToAndInteract(returnPortal);
            }

            var riftEntrancePortal = Data.Portals.FirstOrDefault(p =>
                p.ActorSnoId == 396751 || // Greater/Empowered Rift
                p.ActorSnoId == 345935); // Normal Rift        

            Log.Info("Entering the open rift... ");

            if (riftEntrancePortal == null)
                return false;

            // todo: add rift obelisk locations for each act and go there instead.
            await Movement.MoveTo(Town.Locations.KanaisCube);
            await Movement.MoveToAndInteract(riftEntrancePortal);
            return true;
        }

    }
}
