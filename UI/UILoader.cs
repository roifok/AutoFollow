using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using AutoFollow.Resources;
using AutoFollow.UI.Settings;
using Zeta.Bot;

namespace AutoFollow.UI
{
    class UILoader
    {
        public int ServerPort { get; set; }

        private static Window _configWindow;

        public static void CloseWindow()
        {
            _configWindow.Close();
        }

        public static Window GetSettingsWindow()
        {
            try
            {
                if (_configWindow == null)
                {
                    _configWindow = new Window();
                }

                var path = Path.Combine("Settings", "Settings.xaml");            
                var mainControl = GetControl<UserControl>(path);
                LoadResourceForWindow("Template.xaml", mainControl);

                _configWindow.DataContext = AutoFollowSettings.Instance;            
                _configWindow.Content = mainControl;
                _configWindow.Width = 300;
                _configWindow.Height = 380;
                _configWindow.MinWidth = 300;
                _configWindow.MinHeight = 380;
                _configWindow.ResizeMode = ResizeMode.CanResizeWithGrip;
                _configWindow.Title = "AutoFollow";

                _configWindow.Closed += SettingsWindow_Closed;
                Application.Current.Exit += SettingsWindow_Closed;

                return _configWindow;
            }
            catch (Exception ex)
            {
                Log.Error("Exception loading window {0}", ex);
            }
            return null;
        }

        public static T GetControl<T>(string relativePath)
        {
            var xamlPath = Path.Combine(GetUiPath(), relativePath);
            var xamlContent = File.ReadAllText(xamlPath);

            xamlContent = AppendAssembly(xamlContent, "controls", "AutoFollow.UI.Components.Controls");
            xamlContent = AppendAssembly(xamlContent, "converters", "AutoFollow.UI.Components.Converters");
            xamlContent = AppendAssembly(xamlContent, "behaviors", "AutoFollow.UI.Components.Behaviors");

            xamlContent = ReplaceResourceDictionary(xamlContent);

            return (T)XamlReader.Load(new MemoryStream(Encoding.UTF8.GetBytes(xamlContent)));
        }

        public static string GetUiPath()
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (assemblyPath == null)
                return null;

            return Path.Combine(assemblyPath, "Plugins", "AutoFollow", "UI");
        }

        private static void LoadResourceForWindow(string relativePath, UserControl control)
        {
            var resource = GetControl<ResourceDictionary>(relativePath);
            foreach (var res in resource.Cast<DictionaryEntry>().Where(res => !control.Resources.Contains(res.Key)))
            {
                control.Resources.Add(res.Key, res.Value);
            }
        }

        private static string AppendAssembly(string xamlContent, string token, string @namespace)
        {
            return xamlContent.Replace(
                "xmlns:"+ token + "=\"clr-namespace:" + @namespace + "\"",
                "xmlns:" + token + "=\"clr-namespace:" + @namespace + ";assembly=" + Assembly.GetExecutingAssembly().GetName().Name + "\"");
        }

        private static string ReplaceResourceDictionary(string xamlContent)
        {
            return Regex.Replace(xamlContent, "<ResourceDictionary.MergedDictionaries>.*</ResourceDictionary.MergedDictionaries>", 
                string.Empty, RegexOptions.Singleline | RegexOptions.Compiled);
        }

        static void SettingsWindow_Closed(object sender, System.EventArgs e)
        {
            AutoFollowSettings.Instance.Save();
            if (_configWindow != null)
            {
                _configWindow.Closed -= SettingsWindow_Closed;
                _configWindow = null;
            }

            if (OnWindowClosed != null)
                OnWindowClosed();
        }

        public delegate void LoaderEvent();
        public static event LoaderEvent OnWindowClosed = () => { };

        public static void OpenSettingsWindow()
        {
            if (_configWindow != null && _configWindow.IsVisible)
                return;

            GetSettingsWindow().Show();
        }
    }
}


