using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using AutoFollow.Behaviors.Structures;
using AutoFollow.Networking;
using AutoFollow.Resources;
using AutoFollow.UI.Components.Controls;
using Buddy.Overlay.Commands;
using JetBrains.Annotations;
using Zeta.Bot;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Message = AutoFollow.Networking.Message;

namespace AutoFollow.UI.Tab 
{
    public class TabViewModel : INotifyPropertyChanged
    {
        private string _behaviorName;
        private int _connectedBots;
        private ConnectionMode _connectionMode;
        private Uri _serverUri;
        private int _updateInterval;
        private bool _isConnected;

        public TabViewModel()
        {
            Service.OnUpdated += Service_OnUpdated;
            Service_OnUpdated();
        }

        private DateTime _lastUpdate = DateTime.MinValue;
        private double _lastUpdateMs;

        private void Service_OnUpdated()
        {
            BehaviorName = AutoFollow.CurrentBehavior.Name;
            ConnectionMode = Service.ConnectionMode;
            IsConnected = Service.IsConnected;
            ConnectedBots = AutoFollow.NumberOfConnectedBots;
            UpdateInterval = Settings.Network.UpdateInterval;
            LastUpdateMs = DateTime.UtcNow.Subtract(_lastUpdate).TotalMilliseconds;
            ServerURI = Server.ServerUri;
            _lastUpdate = DateTime.UtcNow;
        }

        public double LastUpdateMs
        {
            get { return _lastUpdateMs; }
            set { SetField(ref _lastUpdateMs, value); }
        }

        public Uri ServerURI
        {
            get { return _serverUri; }
            set { SetField(ref _serverUri, value); }
        }

        public int UpdateInterval
        {
            get { return _updateInterval; }
            set { SetField(ref _updateInterval, value); }
        }

        public bool IsConnected
        {
            get { return _isConnected; }
            set { SetField(ref _isConnected, value); }
        }

        public int ConnectedBots
        {
            get { return _connectedBots; }
            set { SetField(ref _connectedBots, value); }
        }

        public ConnectionMode ConnectionMode
        {
            get { return _connectionMode; }
            set { SetField(ref _connectionMode, value); }
        }

        public string BehaviorName
        {
            get { return _behaviorName; }
            set { SetField(ref _behaviorName, value); }
        }

        public ICommand OpenSettingsCommand
        {
            get
            {
                return new RelayCommand(param =>
                {                    
                    try
                    {
                        UILoader.OpenSettingsWindow();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("{0}", ex);
                    }
                });
            }
        }

        public ICommand InvitePlayerCommand
        {
            get
            {
                return new RelayCommand(param =>
                {
                    //if (!BotMain.IsRunning || BotMain.IsPausedForStateExecution)
                    //{
                    //    Log.Error("Bot must be running to invite someone");
                    //    return;
                    //}

                    //var mbox = InputBox.Show("Enter Battle>net", "Adventurer", string.Empty);
                    //if (mbox.ReturnCode == DialogResult.Cancel)
                    //{
                    //    return;
                    //}
                    //if (string.IsNullOrWhiteSpace(mbox.Text))
                    //{
                    //    Logger.Info("Enter an actorId");
                    //    return;
                    //}

                    //int actorId;
                    //if (!int.TryParse(mbox.Text, out actorId))
                    //{
                    //    Logger.Info("Invalid actorId");
                    //    return;
                    //}

                    //if (!ZetaDia.IsInGame)
                    //    return;

                    //using (ZetaDia.Memory.AcquireFrame(true))
                    //{

                    //    if (ZetaDia.Me == null)
                    //        return;
                    //    if (!ZetaDia.Me.IsValid)
                    //        return;

                    //    ZetaDia.Actors.Update();


                    //}

                });
            }           
        }


        public ICommand TestCommand
        {
            get
            {
                return new RelayCommand(param =>
                {
                    try
                    {

                        Log.Info("Test button clicked!");

                        ////Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Slot4
                        //var slot4 = UIElement.FromName("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Slot4");
                        //if (slot4.IsVisible)
                        //{
                        //    Log.Info("Portait 4 Found!");

                        //    var el = slot4.FindDecedentsWithText("sparkle").FirstOrDefault();
                        //    if (el != null)
                        //    {
                        //        //Log.Info("Portait 4 Name found on element! {0}", );
                        //        el.LogElement();
                        //    }

                        //}

                        var contacts = new List<StackPanelReader.StackPanelItem>();
                        contacts.AddRange(GameUI.SocialFriendsListStackPanel.GetStackPanelItems());
                        contacts.AddRange(GameUI.SocialLocalPlayersStackPanel.GetStackPanelItems());
                        contacts.AddRange(GameUI.SocialRecentPlayersStackPanel.GetStackPanelItems());

                        if (!contacts.Any())
                        {
                            Log.Info("No friends, local or recent players were found!");
                            return;
                        }

                        foreach (var item in contacts)
                        {
                            var rawName = item.TextElement.Text;                            
                            var name = Common.CleanString(item.TextElement.Text);
                            var presenceElement = item.TextElement.GetSiblingByName("Presence");                        
                            var matches = Common.SocialPanelEntryPresenceRegex.GroupMatches(presenceElement.Text);
                            var activity = matches.StringByGroupName["Activity"].FirstOrDefault();
                            var level = matches.NumberByGroupName["Level"].FirstOrDefault();
                            var paragon = matches.NumberByGroupName["Paragon"].FirstOrDefault();
                            var actorClass = matches.StringByGroupName["Class"].FirstOrDefault();

                            
         
                            //}\((\d+)\){\/c}\s(\w+)

                            //var isBattleTag = Message.IsBattleTag(name, follower.BattleTagEncrypted);
                            //if (isBattleTag)
                            //{


                            Log.Info($"Found Name={item.TextElement.Text} CleanName={name} Level={level?.NumberValue} Paragon={paragon?.NumberValue} Class={actorClass?.StringValue} Activity={activity?.StringValue}");

                            ////foreach (var sibling in item.ItemElement.GetSiblings())
                            ////{
                            ////    sibling.LogElement();

                            ////    foreach (var child in sibling.GetChildren())
                            ////    {
                            ////        child.LogElement();

                            ////        foreach (var child2 in child.GetChildren())
                            ////        {
                            ////            child.LogElement();
                            ////        }
                            ////    }
                            ////}

                            //item.ItemElement.Click();
                            //item.TextElement.Click();

                            //var nodes = UIElement.UIMap.Where(n => n.Text.Contains("name"));
                            //foreach (var node in nodes)
                            //{
                            //    node.LogElement();
                            //}

                            //var inviteButton = item.TextElement.GetSiblingByName("PartyInviteButton");
                            //inviteButton.Click();
                            //}
                        }

                        //if (GameUI.JoinRiftButton.IsValid && GameUI.JoinRiftButton.IsVisible)
                        //{
                        //    Log.Info("Rift Dialog Visible");

                        //    if (GameUI.EmpoweredRiftToggle.IsValid && GameUI.EmpoweredRiftToggle.IsVisible)
                        //    {
                        //        Log.Info("Empowered Toggle Visible");

                        //        //[211CCE20] Last clicked: 0xB79271A663726153, Name: Root.NormalLayer.rift_join_party_main.stack.wrapper.Cancel
                        //        //[211C9B00] Mouseover: 0x6CC852143ECBE40F, Name: Root.NormalLayer.rift_join_party_main.stack.EmpoweredRiftToggle

                        //        GameUI.EmpoweredRiftToggle.Click();

                        //        GameUI.EmpoweredRiftToggle.DebugTestClickChildren();

                        //        //GameUI.EmpoweredRiftToggle.DebugTestClickSiblings();

                        //        GameUI.EmpoweredRiftToggle.GetChildren();

                        //        //GameUI.EmpoweredRiftToggle.LogElement();
                        //        //Thread.Sleep(500);
                        //    }
                        //}

                        //var s1 = UIElement.FromName("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Slot1.LayoutRoot.Portrait.text");
                        //s1.LogElement();
                        //var s2 = UIElement.FromName("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Slot2.LayoutRoot.Portrait.text");
                        //s2.LogElement();
                        //var s3 = UIElement.FromName("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Slot3.LayoutRoot.Portrait.text");
                        //s3.LogElement();
                        //var s4 = UIElement.FromName("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Slot4.LayoutRoot.Portrait.text");
                        //s4.LogElement();


                        //var message = AutoFollow.CurrentLeader;

                        //if (!ZetaDia.Service.IsInGame && ZetaDia.Service.Party.NumPartyMembers > 1)
                        //{
                        //    var slot1NameElement = GameUI.PartySlot1Name;
                        //    if (slot1NameElement.IsVisible && slot1NameElement.HasText && Message.IsBattleTag(Common.CleanString(slot1NameElement.Text), message.BattleTagEncrypted))
                        //    {
                        //        Log.Warn("Leader is in slot 1 of our party");
                        //    }
                        //    var slot2NameElement = GameUI.PartySlot1Name;
                        //    if (slot2NameElement.IsVisible && slot2NameElement.HasText && Message.IsBattleTag(Common.CleanString(slot2NameElement.Text), message.BattleTagEncrypted))
                        //    {
                        //        Log.Warn("Leader is in slot 2 of our party");
                        //    }
                        //    var slot3NameElement = GameUI.PartySlot1Name;
                        //    if (slot3NameElement.IsVisible && slot3NameElement.HasText && Message.IsBattleTag(Common.CleanString(slot3NameElement.Text), message.BattleTagEncrypted))
                        //    {
                        //        Log.Warn("Leader is in slot 3 of our party");
                        //    }
                        //    var slot4NameElement = GameUI.PartySlot1Name;
                        //    if (slot4NameElement.IsVisible && slot4NameElement.HasText && Message.IsBattleTag(Common.CleanString(slot4NameElement.Text), message.BattleTagEncrypted))
                        //    {
                        //        Log.Warn("Leader is in slot 4 of our party");
                        //    }
                        //}


                        //var partyInvite = GameUI.PartyInviteDialog;
                        //if (partyInvite.IsVisible)
                        //{
                        //    Log.Info("We have a pending request dialog!");

                        //    var el = partyInvite.FindDecedentsWithText("").FirstOrDefault();
                        //    if (el != null)
                        //    {
                        //        Log.Info("Element found {0} {1} {2}", el.Text, el.Hash, el.Name);
                        //    }

                        //    //var name = UIElement.FromName("Root.TopLayer.BattleNetNotifications_main.Invite To Party Notification.PartyPlayer");                            
                        //    var name = UIElement.FromHash(12614685561186959926);
                        //    if (name != null)
                        //    {
                        //        Log.Info("PlayerName is {0}", name.Text);
                        //    }                            
                        //}

                        //if (!GameUI.FriendsListContent.IsVisible)
                        //{
                        //    Log.Info("Opening Social Panel");
                        //    GameUI.SocialFlyoutButton.Click();
                        //    Thread.Sleep(250);
                        //}

                        //var stackPanel = GameUI.SocialFriendsListStackPanel;
                        //var stackPanelItems = stackPanel.GetStackPanelItems();

                        //if (!stackPanelItems.Any())
                        //{
                        //    Log.Info("You have no friends!");
                        //    return;
                        //}
                        //foreach (var item in stackPanelItems)
                        //{
                        //    var text = item.TextElement.Text;

                        //    //if (Message.IsBattleTag(Common.CleanString(text), follower.BattleTagEncrypted))
                        //    //{
                        //        Log.Info("Found follower on friends list!");
                        //        var inviteButton = item.TextElement.GetSiblingByName("PartyInviteButton");
                        //        inviteButton.SafeClick();
                        //    //}
                        //}

                        //[20527430] Last clicked: 0xA41B846275DC7A09, Name: Root.TopLayer.BattleNetNotifications_main.Invite To Party Notification
                        //[20527430] Mouseover: 0xA41B846275DC7A09, Name: Root.TopLayer.BattleNetNotifications_main.Invite To Party Notification

                        //_lastInviteAttempt = DateTime.UtcNow;
                        //return true;

                        //AutoFollow.CurrentParty.ForEach(p => p.is)
                        //var stackPanel = GameUI.SocialFriendsListStackPanel;
                        //var stackPanelItems = stackPanel.GetStackPanelItems();

                        //if (!stackPanelItems.Any())
                        //{
                        //    Log.Info("You have no friends!");
                        //}

                        ////if (!stackPanel.IsVisible)
                        ////{
                        ////    Log.Info("Opening Friends List");

                        ////    //Root.NormalLayer.BattleNetFriendsList_main.LayoutRoot.OverlayContainer.FriendsListContent.SocialListContainer.SocialList.FriendsListContainer.FriendsGroupHeader
                        ////    var friendsheader = UIElement.FromName("Root.NormalLayer.BattleNetFriendsList_main.LayoutRoot.OverlayContainer.FriendsListContent.SocialListContainer.SocialList.FriendsListContainer.FriendsGroupHeader");
                        ////    friendsheader.DebugTestClickChildren();
                        ////}

                        //foreach (var item in stackPanelItems)
                        //{
                        //    var text = item.TextElement.Text;

                        //    Log.Info("Element found with text: {0} TextVisible={1}", text, item.TextElement.IsVisible);

                        //    //GameUI.SocialFilterInputBox.SetText(text);
                        //    //Thread.Sleep(150);

                        //    //var siblings = item.TextElement.GetSiblings();
                        //    var inviteButton = item.TextElement.GetSiblingByName("PartyInviteButton");
                        //    inviteButton.SafeClick();

                        //}

                        ////GameUI.RequestToJoinPartyButton.Click();


                    }
                    catch (Exception ex)
                    {
                        Log.Error("{0}", ex);
                    }
                });
            }
        }

        #region INotifyPropertyChanged

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
