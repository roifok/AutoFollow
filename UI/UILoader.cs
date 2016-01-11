using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using AutoFollow.UI.Settings;

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
            if (_configWindow == null)
            {
                _configWindow = new Window();
            }

            var path = Path.Combine("Settings", "Settings.xaml");            
            var mainControl = GetControl(path);

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

        public static UserControl GetControl(string relativePath)
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (assemblyPath == null)
                return null;

            var xamlPath = Path.Combine(assemblyPath, "Plugins", "AutoFollow", "UI", relativePath);
            var xamlContent = File.ReadAllText(xamlPath);

            return (UserControl)XamlReader.Load(new MemoryStream(Encoding.UTF8.GetBytes(xamlContent)));
        }

        static void SettingsWindow_Closed(object sender, System.EventArgs e)
        {
            AutoFollowSettings.Instance.Save();
            if (_configWindow != null)
            {
                _configWindow.Closed -= SettingsWindow_Closed;
                _configWindow = null;
            }
        }

        public static void OpenSettingsWindow()
        {
            if (_configWindow != null && _configWindow.IsVisible)
                return;

            GetSettingsWindow().Show();
        }
    }
}

