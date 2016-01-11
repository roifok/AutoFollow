using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFollow.Resources;

namespace AutoFollow.Coroutines
{
    public class Questing
    {
        public static async Task<bool> UpgradeGems()
        {
            if (RiftHelper.IsInRift && RiftHelper.CurrentRift.IsCompleted)
            {
                Log.Warn("Rift is Completed; requesting gem upgrade from other plugins.");
                PluginCommunicator.BroadcastGemUpgradRequest();
                return true;
            }

            return false;            
        }
    }
}
