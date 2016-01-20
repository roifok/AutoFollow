using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeta.Bot;

namespace AutoFollow.Resources
{
    public enum CombatState
    {
        None = 0,
        Enabled,
        Disabled,
        Pulsing
    }

    public static class Combat
    {
        private static DateTime _nextPulse = DateTime.MinValue;

        public static CombatState State { get; set; }

        static Combat()
        {
            Pulsator.OnPulse += Pulsator_OnPulse;
        }

        private static void Pulsator_OnPulse(object sender, EventArgs eventArgs)
        {
            switch (State)
            {
                case CombatState.Pulsing:

                    if (DateTime.UtcNow >= _nextPulse)
                    {
                        ToggleCombat();
                        _nextPulse = DateTime.UtcNow.Add(TimeSpan.FromMilliseconds(1000));
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

    }
}

