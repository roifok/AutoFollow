using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFollow.Coroutines.Resources;
using AutoFollow.Resources;
using Buddy.Coroutines;
using Zeta.Game;
using Zeta.TreeSharp;
using System.Diagnostics;
using AutoFollow.Behaviors;
using Zeta.Game.Internals.Service;

namespace AutoFollow.Coroutines
{
    public class SafeLeaveGame
    {
        private static DateTime LastLeaveGameAttempt = DateTime.MinValue;        

        public static async Task<bool> Execute()
        {
            if (!ZetaDia.IsInGame)
                return true;

            if (ZetaDia.IsLoadingWorld || GameUI.LeaveGameButton.IsVisible)
                return false;

            if (ZetaDia.Service.Party.CurrentPartyLockReasonFlags != PartyLockReasonFlag.None)
                return false;

            if (DateTime.UtcNow.Subtract(LastLeaveGameAttempt).TotalSeconds < 5)
                return false;
    
            ZetaDia.Service.Party.LeaveGame(true);
            LastLeaveGameAttempt = DateTime.UtcNow;
            return true;
        }
    }
}
