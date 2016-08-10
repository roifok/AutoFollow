using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoFollow.Networking;
using AutoFollow.Resources;
using Buddy.Coroutines;
using Zeta.Bot;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.SNO;

namespace AutoFollow.Coroutines
{
    public class TeleportToPlayer
    {
        private static DateTime _teleportCooldownTimer = DateTime.MinValue;
        private static Stopwatch _teleportTimer = new Stopwatch();

        public static async Task<bool> Execute(int playerMessage)
        {

            Log.Warn("Teleporting to player {0}.", playerMessage);
            if (!_teleportTimer.IsRunning)
                _teleportTimer.Start();
            while (!ZetaDia.IsLoadingWorld)
            {
                ZetaDia.Me.TeleportToPlayerByIndex(playerMessage);
                if (_teleportTimer.ElapsedMilliseconds > 5000)
                {
                    _teleportTimer.Stop();
                    _teleportTimer.Reset();
                    return false;
                }
                if (ZetaDia.Me.IsInCombat || ZetaDia.Me.IsDead)
                    return false;

                await Coroutine.Sleep(500);
                await Coroutine.Yield();
            }

            if (ZetaDia.Me.IsInCombat || ZetaDia.Me.IsDead)
                return false;

            while (ZetaDia.IsLoadingWorld)
            {
                await Coroutine.Sleep(500);
                await Coroutine.Yield();
            }
            _teleportCooldownTimer = DateTime.Now.AddSeconds(10);
            Log.Debug("Finished TeleportToPlayer Task");
            return true;
        }

        public static async Task<bool> Execute(Message playerMessage)
        {

            if (playerMessage.IsInTown || !ZetaDia.IsInGame || ZetaDia.IsLoadingWorld ||
                DateTime.Now < _teleportCooldownTimer | playerMessage.Distance < 50 ||
                ZetaDia.IsInTown && playerMessage.IsInTown || ZetaDia.Me.IsDead)
                return false;


            if (ZetaDia.Service.Party.NumPartyMembers <= 1)
            {
                Log.Info("Can not teleport because you are not in a party!");
                return false;
            }

            if ((RiftHelper.IsInRift || playerMessage.IsInRift) && RiftHelper.IsGreaterRiftStarted &&
                RiftHelper.RiftQuest.Step != RiftQuest.RiftStep.Cleared &&
                RiftHelper.RiftQuest.Step != RiftQuest.RiftStep.UrshiSpawned)
            {
                //Log.Info(RiftHelper.IsInRift + " " + RiftHelper.IsGreaterRiftStarted + " " + RiftHelper.RiftQuest.State + " " + RiftHelper.RiftQuest.Step);
                Log.Verbose("Can not Port to Leader as they are in a Greater Rift.");
                return false;
            }
            if ((ZetaDia.Me.IsInCombat || Data.Monsters.Any(m => m.IsHostile && m.Distance < 60f)) &&
                RiftHelper.RiftQuest.Step != RiftQuest.RiftStep.Cleared &&
                RiftHelper.RiftQuest.Step != RiftQuest.RiftStep.UrshiSpawned)
            {
                Log.Verbose("Can not Port to Leader as we are in Combat.");
                return false;
            }

            if (playerMessage.Distance < 50)
            {
                Log.Info("Cant teleport because the player is too close.");
                return false;
            }

            Log.Warn("Teleporting to player {0} SameGame={1} SameWorld={2}",
                playerMessage.HeroName, playerMessage.IsInSameGame, playerMessage.IsInSameWorld);
            if (!_teleportTimer.IsRunning)
                _teleportTimer.Start();
            while (!ZetaDia.IsLoadingWorld && !ZetaDia.Me.IsInCombat)
            {
                ZetaDia.Me.TeleportToPlayerByIndex(playerMessage.Index);
                if (_teleportTimer.ElapsedMilliseconds > 5000)
                {
                    _teleportTimer.Stop();
                    _teleportTimer.Reset();
                    return false;
                }
                if (playerMessage.Distance < 50 || ZetaDia.IsInTown && playerMessage.IsInTown || ZetaDia.Me.IsDead)
                    return false;
                await Coroutine.Sleep(500);
                await Coroutine.Yield();
            }

            if (ZetaDia.Me.IsInCombat || ZetaDia.Me.IsDead)
                return false;

            while (ZetaDia.IsLoadingWorld)
            {
                await Coroutine.Sleep(500);
                await Coroutine.Yield();
            }
            _teleportCooldownTimer = DateTime.Now.AddSeconds(10);
            Log.Debug("Fnished TeleportToPlayer Task");
            return true;
        }
    }
}