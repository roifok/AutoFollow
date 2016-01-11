#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AutoFollow.UI.Tab;
using Zeta.Bot;
using Zeta.Bot.Navigation;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using UIElement = Zeta.Game.Internals.UIElement;

#endregion

namespace AutoFollow.UI
{
    internal class TabUi
    {
        private static UniformGrid _tabGrid;
        private static TabItem _tabItem;

        public static HashSet<int> CrafingMaterialIds = new HashSet<int>
        {
            361985, //Type: Item, Name: Arcane Dust
            361984, //Type: Item, Name: Reusable Parts
            361986, //Type: Item, Name: Veiled Crystal
            364281, //Type: Item, Name: Caldeum Nightshade
            361989, //Type: Item, Name: Death's Breath
            364975, //Type: Item, Name: Westmarch Holy Water
            364290, //Type: Item, Name: Arreat War Tapestry
            364305, //Type: Item, Name: Corrupted Angel Flesh
            365020, //Type: Item, Name: Khanduran Rune
            361988 //Type: Item, Name: Forgotten Soul
        };

        private static DateTime LastStartedConvert = DateTime.UtcNow;

        internal static void InstallTab()
        {
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    var mainWindow = Application.Current.MainWindow;

                    _tabGrid = new UniformGrid
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Top,
                        Columns = 1,
                        //MaxHeight = 180
                    };

                    var path = Path.Combine("Tab", "Tab.xaml");
                    var mainControl = UILoader.GetControl(path);
                    ViewModel = new TabViewModel();
                    mainControl.DataContext = ViewModel;

                    _tabItem = new TabItem
                    {
                        Header = "AutoFollow",
                        //ToolTip = "",
                        Content = _tabGrid
                    };

                    var tabs = mainWindow.FindName("tabControlMain") as TabControl;
                    if (tabs == null)
                        return;

                    tabs.Items.Add(_tabItem);
                    _tabGrid.Children.Add(mainControl);
                }
                );
        }

        public static TabViewModel ViewModel { get; set; }

        internal static void RemoveTab()
        {
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    var mainWindow = Application.Current.MainWindow;
                    var tabs = mainWindow.FindName("tabControlMain") as TabControl;
                    if (tabs == null)
                        return;
                    tabs.Items.Remove(_tabItem);
                }
                );
        }

        private static void CreateButton(string buttonText, RoutedEventHandler clickHandler)
        {
            var button = new Button
            {
                Width = 120,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(3),
                Content = buttonText
            };
            button.Click += clickHandler;
            _tabGrid.Children.Add(button);
        }

 
    }
}