using System;
using System.IO;
using System.Reflection;
using Zeta.Game;

namespace AutoFollow.Resources
{
    public static class FileUtils
    {
        private static string _demonBuddyPath;
        public static string DemonBuddyPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_demonBuddyPath))
                    _demonBuddyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                return _demonBuddyPath;
            }
        }

        private static string _pluginPath;
        public static string PluginPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_pluginPath))
                    _pluginPath = Path.Combine(DemonBuddyPath, "Plugins", "zAutoFollow");
                return _pluginPath;
            }
        }

        private static string _logPath;
        public static string LogPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_logPath))
                    _logPath = Path.Combine(DemonBuddyPath, "AutoFollowLogs");
                return _logPath;
            }
        }

        public static string SettingsFolder
        {
            get
            {
                return Path.Combine(DemonBuddyPath, "Settings", ZetaDia.Service.Hero.BattleTagName, "AutoFollow", ZetaDia.Service.Hero.HeroId.ToString());
            }
        }    

        public static string SettingsPath
        {
            get
            {
                return Path.Combine(DemonBuddyPath, "Settings", ZetaDia.Service.Hero.BattleTagName, "AutoFollow", ZetaDia.Service.Hero.HeroId.ToString(), "Config.json");
            }
        }

        public static void WriteToTextFile(string path, string value)
        {
            Directory.CreateDirectory(Path.Combine(DemonBuddyPath, "Settings", ZetaDia.Service.Hero.BattleTagName, "AutoFollow", ZetaDia.Service.Hero.HeroId.ToString()));
            File.WriteAllText(path, value);
        }

        public static void AppendToLogFile(string name, string value)
        {
            Directory.CreateDirectory(LogPath);
            var path = Path.Combine(LogPath, name + ".log");
            File.AppendAllLines(path, new[] { value });
        }

        public static string ReadFromTextFile(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (Exception ex)
            {
                Log.Debug("[ReadFromTextFile] " + ex.Message + Environment.NewLine + ex.StackTrace);
                return null;
            }
        }


    }
}
