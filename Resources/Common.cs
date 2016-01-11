using System;
using System.Text.RegularExpressions;
using AutoFollow.Coroutines.Resources;
using Zeta.Game;
using Zeta.TreeSharp;

namespace AutoFollow.Resources
{
    public class Common
    {
        private static DateTime _leaveGameAttemptTime = DateTime.MinValue;
        public static RunStatus SafeLeaveGame()
        {
            if (!ZetaDia.IsInGame)
            {
                return RunStatus.Success;
            }

            if (ZetaDia.IsLoadingWorld)
                return RunStatus.Running;

            if (GameUI.LeaveGameButton.IsVisible)
                return RunStatus.Running;

            if (DateTime.UtcNow.Subtract(_leaveGameAttemptTime).TotalSeconds < 5)
                return RunStatus.Running;

            ZetaDia.Service.Party.LeaveGame(Player.IsClient);

            _leaveGameAttemptTime = DateTime.UtcNow;

            return RunStatus.Running;
        }

        /// <summary>
        /// Strips out a name from color encoded UI string e.g. 
        /// {c:ff6969ff}Corrupted Angel{/c}
        /// </summary>
        public static string CleanString(string s)
        {
            try
            {
                var regex = new Regex(@"[^/w/}/>\s]+(?=\{\/c\})");
                var match = regex.Match(s); 
                if(match.Success)
                    return  match.Value.Trim();
            }
            catch (Exception ex)
            {
                // exception due to unsafe stuff in string
                Log.Debug("Exception in CleanString {0}", ex);                
            }
            return s.Trim();
        }
    }
}