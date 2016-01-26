using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using Zeta.Bot;

namespace AutoFollow.Resources
{
    public class ProfileUtils
    {
        public static bool ProfileHasTag(string tagName)
        {
            var profile = ProfileManager.CurrentProfile;
            if (profile != null && profile.Element != null)
            {
                return profile.Element.XPathSelectElement("descendant::" + tagName) != null;
            }
            return false;
        }

        public static XElement GetProfileTag(string tagName)
        {
            var profile = ProfileManager.CurrentProfile;
            if (profile != null && profile.Element != null)
            {
                return profile.Element.XPathSelectElement("descendant::" + tagName);
            }
            return null;
        }

        public static bool ProfileIsYarKickstart
        {
            get
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(ProfileManager.CurrentProfile.Path);
                return fileNameWithoutExtension != null && fileNameWithoutExtension.ToLower().StartsWith("yar_kickstart");
            }
        }
    }
}