using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFollow.Resources;
using Zeta.Bot;
using Zeta.Game;
using Zeta.Game.Internals.Actors;

namespace AutoFollow.Coroutines
{
    public static class Combat
    {
        //private static DateTime _lastPowerAreaTick = DateTime.MinValue;

        //public async static Task<bool> StandInFocussedPowerArea()
        //{
        //    if (!Player.IsInCombat || AutoFollow.CurrentLeader.Distance > Settings.Coordination.CatchUpDistance)
        //        return false;

        //    if (DateTime.UtcNow.Subtract(_lastPowerAreaTick).TotalSeconds < 2)
        //        return false;

        //    var currentPower = CombatBase.CurrentPower;
        //    var target = CombatBase.CurrentTarget;
        //    if (currentPower == null || target == null)
        //        return false;

        //    _lastPowerAreaTick = DateTime.UtcNow;

        //    //[1EEA0C30] Type: ClientEffect Name: p2_itemPassive_unique_ring_017_dome-99510 ActorSnoId: 433966, Distance: 23.9435            

        //    var powerAreas = ZetaDia.Actors.GetActorsOfType<DiaObject>().Where(a => a.ACDId == 433966 && a.Distance < 30f).ToList();
        //    var powerArea = powerAreas.FirstOrDefault();
        //    if (powerArea == null)
        //        return false;

        //    var distBetweenAreaAndTarget = target.Position.Distance(powerArea.Position);
        //    if (distBetweenAreaAndTarget < currentPower.MinimumRange)
        //    {
        //        Log.Warn("Moving to area of focussed power Distance={0} Power={1} Target={2}", powerArea.Distance, currentPower.SNOPower, target.InternalName);
        //        await Movement.MoveTo(() => new Target(powerArea), "Power Area", 3f, targetable => false);
        //        return true;
        //    }

        //    return false;
        //}
    }
}
