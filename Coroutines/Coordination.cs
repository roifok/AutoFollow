using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFollow.Resources;
using Buddy.Coroutines;
using Zeta.Bot;
using Zeta.Game.Internals.Actors;

namespace AutoFollow.Coroutines
{
    public static class Coordination
    {
        static Coordination()
        {
            BotMain.OnStart += Reset;
        }

        private static void Reset(IBot bot)
        {
            _startedWaiting = null;
            _ignoreOtherBotsUntilTime = DateTime.MinValue;
        }

        public static async Task<bool> WaitForGreaterRiftInProgress()
        {
            if (RiftHelper.IsLockedOutOfRift)
            {
                Log.Info("Locked out of rift, waiting for it to finish.");
                await Coroutine.Sleep(5000);
                return true;
            }
            return false;            
        }

        private static DateTime? _startedWaiting;
        private static DateTime _ignoreOtherBotsUntilTime;

        public static async Task<bool> WaitBeforeStartingRift()
        {
            if (!RiftHelper.IsGreaterRiftProfile || RiftHelper.IsGreaterRiftStarted)
                return false;

            if (_ignoreOtherBotsUntilTime > DateTime.UtcNow)
                return false;

            if (AutoFollow.CurrentFollowers.Any(f => f.IsVendoring))
            {
                Log.Info("Waiting for followers to finish vendoring.");
                await Coroutine.Sleep(15000);
                return false;
            }

            if (AutoFollow.NumberOfConnectedBots == 0 || !AutoFollow.CurrentFollowers.All(f => f.IsInSameGame))
            {
                if (!_startedWaiting.HasValue)
                {
                    _startedWaiting = DateTime.UtcNow;
                    Log.Info("Waiting for bots to connect/join.");
                    await Coroutine.Sleep(8000);
                    return true;
                }

                var secondsWaiting = DateTime.UtcNow.Subtract(_startedWaiting.Value).TotalSeconds;
                if (secondsWaiting < 60)
                {
                    Log.Info("Waiting for bots to connect/join. Waited {0} seconds", secondsWaiting);
                    await Coroutine.Sleep(8000);
                    return true;
                }

                Log.Info("Waited for {0}s bots to connect.. starting without them.", secondsWaiting);
                await Coroutine.Sleep(5000);
                _startedWaiting = null;
                _ignoreOtherBotsUntilTime = DateTime.UtcNow.AddMinutes(2);
            }

            return false;
        }
    }

}

