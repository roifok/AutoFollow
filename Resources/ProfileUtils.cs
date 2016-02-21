using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using Zeta.Bot;
using Zeta.Game;

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

        public static string GetProfileAttribute(string tagName, string attrName)
        {
            var profileTag = GetProfileTag(tagName);
            if (profileTag != null)
            {
                var behaviorAttr = profileTag.Attribute(attrName);
                if (behaviorAttr != null && !string.IsNullOrEmpty(behaviorAttr.Value))
                {
                    return behaviorAttr.Value;
                }
            }
            
            return string.Empty;
        }
    }

}