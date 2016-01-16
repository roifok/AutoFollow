using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFollow.Coroutines.Resources;
using AutoFollow.Resources;
using Buddy.Coroutines;

namespace AutoFollow.Coroutines
{
    public class Questing
    {
        private static DateTime LastRequestedGemUpgrade = DateTime.MinValue;

        public static async Task<bool> UpgradeGems()
        {
            if (DateTime.UtcNow.Subtract(LastRequestedGemUpgrade).TotalMinutes < 1)
                return false;

            if (RiftHelper.IsInRift && RiftHelper.RiftQuest.Step == RiftQuest.RiftStep.UrshiSpawned && RiftHelper.CurrentRift.IsCompleted)
            {
                if (AutoFollow.CurrentLeader.Distance > 150f)
                    await TeleportToPlayer.Execute(AutoFollow.CurrentLeader);

                Log.Warn("Rift is Completed; requesting gem upgrade from other plugins.");
                PluginCommunicator.BroadcastGemUpgradRequest();
                await Coroutine.Sleep(5000);
                LastRequestedGemUpgrade = DateTime.UtcNow;
                return true;
            }

            return false;            
        }

        public static async Task<bool> TalkToOrek()
        {
            if (!Player.Instance.IsVendoring && Player.Instance.IsInTown && RiftHelper.IsGreaterRiftProfile && RiftHelper.RiftQuest.Step == RiftQuest.RiftStep.Cleared)
            {
                if(!await Movement.MoveToAndInteract(Town.Actors.Orek))
                    return false;

                return true;
            }
            return false;
        }

    }
}
