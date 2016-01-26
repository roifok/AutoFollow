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
                return RunStatus.Success;

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

        internal static Regex ItemNameRegex = new Regex(@"-\d+", RegexOptions.Compiled);

        internal static string GetBaseInternalName(string itemInternalName)
        {
            return ItemNameRegex.Replace(itemInternalName, "");
        }

        /// <summary>
        /// Strips out a name from color encoded UI string e.g. 
        /// </summary>
        public static string CleanString(string s)
        {
            try
            {
                //(?<=(\}|\>\W))\S+((?=\{\/c\})|\Z)
                //http://regexstorm.net/tester

                //slot2NameElement.Text	"{c:ff82c5ff}<Clan>{/c}\n{c:ff82c5ff}Name{/c}"	string

                //{c:ff6969ff}<Test> Name{/c}
                //{c:ff6969ff}Name{/c}
                //<Test> Name
                //var regex = new Regex(@"[^/w/}/>\s]+(?=\{\/c\})");

                //var regex = new Regex(@"(?<=(\}|\>\W))\S+((?=\{\/c\})|\Z)");
                //var match = regex.Match(s); 
                //if(match.Success)
                //    return  match.Value.Trim();

                var removedCurlyGroups = Regex.Replace(s, @"\{.*?\}(\s|)+", "");
                var removedAngleGroups = Regex.Replace(removedCurlyGroups, @"\<.*?\>(\s|)+", "");
                var final = removedAngleGroups.Replace("\n","").Trim();
                return final;

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