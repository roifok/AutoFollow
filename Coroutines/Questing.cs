using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            if (RiftHelper.IsInRift && RiftHelper.CurrentRift.IsCompleted)
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
    }
}
