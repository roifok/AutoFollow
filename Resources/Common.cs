using System;
using System.Text.RegularExpressions;
using Zeta.Game;
using Zeta.TreeSharp;

namespace AutoFollow.Resources
{
    public class Common
    {
        internal static Regex ItemNameRegex = new Regex(@"-\d+", RegexOptions.Compiled);

        internal static string GetBaseInternalName(string itemInternalName)
        {
            return ItemNameRegex.Replace(itemInternalName, "");
        }

        /// <summary>
        ///RealId	[0-9]	`NotMyName`
        ///Character[10 - 23]	`NotMyCharName`
        /// </summary>
        internal static Regex InviteRequestRegex = new Regex(@"^(?<RealId>.*).*\((?<Character>.*)\)", RegexOptions.Compiled);

        /// <summary>
        ///Activity	[0-20]	`Greater Rift Tier 52`
        ///Level	[23-25]	`70`
        ///Paragon	[39-42]	`488`
        ///Class	[48-57]	`Barbarian`
        /// </summary>
        internal static Regex SocialPanelEntryPresenceRegex = new Regex(@"^(?<Activity>.*)\s\-\s(?<Level>\d+)\s{.+}\((?<Paragon>\d+)\){\/c}\s(?<Class>\w+)", RegexOptions.Compiled);

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
                var removedSpaces = removedAngleGroups.Replace(" ", "");
                var final = removedSpaces.Replace("\n","").Trim();
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