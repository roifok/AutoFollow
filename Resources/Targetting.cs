using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeta.Bot;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Actors.Gizmos;
using Zeta.Game.Internals.SNO;

namespace AutoFollow.Resources
{
    public enum CombatState
    {
        None = 0,
        Enabled,
        Disabled,
        Pulsing
    }

    public static class Targetting
    {
        private static DateTime _nextPulse = DateTime.MinValue;
        private static CombatState _state;

        public static CombatState State
        {
            get { return _state; }
            set
            {
                //if(Settings.Misc.DebugLogging)
                //    Log.Warn("CombatState Changed to {0}", value);

                _state = value;
            }
        }

        static Targetting()
        {
            Pulsator.OnPulse += Pulsator_OnPulse;
        }

        private static void Pulsator_OnPulse(object sender, EventArgs eventArgs)
        {

            if (ZetaDia.Me == null || !ZetaDia.Me.IsValid)
                return;

            switch (State)
            {
                case CombatState.Pulsing:

                    if (DateTime.UtcNow >= _nextPulse)
                    {
                        ToggleCombat();

                        if(CombatTargeting.Instance.AllowedToKillMonsters)
                            _nextPulse = DateTime.UtcNow.Add(TimeSpan.FromMilliseconds(500));
                        else
                            _nextPulse = DateTime.UtcNow.Add(TimeSpan.FromMilliseconds(2000));
                    }
                    break;     
                        
                case CombatState.Disabled:
                    TurnCombatOff();
                    break;

                default:
                    TurnCombatOn();
                    break;
            }
        }

        private static void ToggleCombat()
        {
            if (CombatTargeting.Instance.AllowedToKillMonsters)
                TurnCombatOff();
            else
                TurnCombatOn();
        }

        private static void TurnCombatOff()
        {
            if (CombatTargeting.Instance.AllowedToKillMonsters)
            {
                Log.Debug("Combat was turned off");
                CombatTargeting.Instance.AllowedToKillMonsters = false;
            }
        }
        private static void TurnCombatOn()
        {
            if (!CombatTargeting.Instance.AllowedToKillMonsters)
            {
                Log.Debug("Combat was turned on");
                CombatTargeting.Instance.AllowedToKillMonsters = true;
            }
        }
        public static bool RoutineWantsToAttackGoblin()
        {
            var combatTarget = CombatTargeting.Instance.Provider.GetObjectsByWeight().FirstOrDefault();
            return combatTarget != null && combatTarget.MonsterInfo.MonsterRace == MonsterRace.TreasureGoblin;
        }

        public static bool RoutineWantsToLoot()
        {
            var combatTarget = CombatTargeting.Instance.Provider.GetObjectsByWeight().FirstOrDefault();
            return combatTarget != null && combatTarget.ActorType == ActorType.Item;
        }

        public static bool RoutineWantsToClickGizmo()
        {
            var combatTarget = CombatTargeting.Instance.Provider.GetObjectsByWeight().FirstOrDefault();
            return combatTarget != null && combatTarget is GizmoShrine && combatTarget.Distance < 80f;
        }

        public static bool RoutineWantsToAttackUnit()
        {
            var combatTarget = CombatTargeting.Instance.Provider.GetObjectsByWeight().FirstOrDefault();
            return combatTarget != null && combatTarget is DiaUnit && combatTarget.Distance < 80f;
        }

    }
}



