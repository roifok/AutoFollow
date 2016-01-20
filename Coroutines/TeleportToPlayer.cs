using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoFollow.Networking;
using AutoFollow.Resources;
using Buddy.Coroutines;
using Zeta.Bot;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.SNO;

namespace AutoFollow.Coroutines
{
    public class TeleportToPlayer
    {
        private static Stopwatch _teleportTimer = new Stopwatch();

        public static async Task<bool> Execute(Message playerMessage)
        {
            Log.Debug("Started TeleportToPlayer Task DistToTarget={0}", playerMessage.Distance);            

            if (!ZetaDia.IsInGame || ZetaDia.IsLoadingWorld)
                return false;

            if (playerMessage.InGreaterRift)
                return false;

            if (playerMessage.IsInSameWorld && playerMessage.Distance < 100f)
                return false;

            if (ZetaDia.Service.Party.NumPartyMembers <= 1)
            {
                Log.Info("Cant teleport because you are not in a party!");
                return false;
            }

            //if (Data.Monsters.Any(m => m.Distance < 60f))
            //{
            //    Log.Info("Cant teleport because there are Monsters nearby! Please kill them first!");
            //    await Coroutine.Sleep(5000);
            //    await Coroutine.Yield();
            //}

            if (!_teleportTimer.IsRunning)
                _teleportTimer.Restart();

            if (_teleportTimer.ElapsedMilliseconds < 4000)
                return false;

            _teleportTimer.Stop();

            Log.Warn("Teleporting to player {0} SameGame={1} SameWorld={2}", 
                playerMessage.HeroName, playerMessage.IsInSameGame, playerMessage.IsInSameWorld);

            ZetaDia.Me.TeleportToPlayerByIndex(playerMessage.Index);
            await Coroutine.Sleep(250);

            while (ZetaDia.IsLoadingWorld || ZetaDia.Me.LoopingAnimationEndTime != 0)
            {
                await Coroutine.Sleep(250);
                await Coroutine.Yield();
            }

            Log.Debug("Fnished TeleportToPlayer Task");
            return true;
        }
    }
}
