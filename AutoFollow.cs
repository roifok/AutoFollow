using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using AutoFollow.Behaviors;
using AutoFollow.Behaviors.Structures;
using AutoFollow.Coroutines.Resources;
using AutoFollow.Events;
using AutoFollow.Networking;
using AutoFollow.ProfileTags;
using AutoFollow.Resources;
using AutoFollow.UI;
using JetBrains.Annotations;
using Zeta.Bot;
using Zeta.Bot.Logic;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.Game;
using Zeta.TreeSharp;
using EventManager = AutoFollow.Events.EventManager;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace AutoFollow
{
    public class AutoFollow : ICommunicationEnabledPlugin
    {
        public AutoFollow()
        {
            // Find and load all IBehavior instances
            Behaviors = new InterfaceLoader<IBehavior>();
            Instance = this;
        }

        public static AutoFollow Instance { get; set; }

        public PluginCommunicationResponse Receive(IPlugin sender, string command, params object[] args)
        {
            return PluginCommunicator.Receive(sender, command, args);
        }

        public static InterfaceLoader<IBehavior> Behaviors;
        public static Version PluginVersion = new Version(1, 0, 14);
        internal static bool Enabled;
        internal static Message ServerMessage = new Message();
        internal static Dictionary<int, Message> ClientMessages = new Dictionary<int, Message>();
        internal static IBehavior LeaderBehavior = new Leader();
        internal static IBehavior FollowerBehavior = new FollowerCombat();
        internal static IBehavior DefaultBehavior = new BaseBehavior();
        internal static List<Message> CurrentParty = new List<Message>();
        internal static List<Message> CurrentFollowers = new List<Message>();
        internal static Message CurrentLeader = new Message();
        public static int NumberOfConnectedBots;
        public static BehaviorType CurrentBehaviorType;
        private static DateTime _lastSelectedBehavior = DateTime.MinValue;

        private static IBehavior _currentBehavior;
        public static IBehavior CurrentBehavior
        {
            get { return AutoFollow._currentBehavior; }
            set
            {
                if (value == null || AutoFollow._currentBehavior == value)
                    return;

                if (AutoFollow._currentBehavior != null)
                {
                    AutoFollow._currentBehavior.Deactivate();
                    Log.Warn("Changing behavior type from {0} to {1}", AutoFollow._currentBehavior.Name, value.Name);

                    // Important: need to restart profile or behavior hooks will still run even after being removed.
                    ProfileManager.Load(ProfileManager.CurrentProfile.Path);
                }

                AutoFollow._currentBehavior = value;
                AutoFollow._currentBehavior.Activate();
            }
        }

        /// <summary>
        /// Called by service communication thread.
        /// </summary>
        public void ServiceOnUpdatePreview()
        {

        }

        internal static void Pulse()
        {
            if (!Enabled)
                return;

            if (ZetaDia.IsLoadingWorld)
                return;

            if (!Service.IsConnected)
            {
                Service.Connect();
                CommunicationThread.ThreadStart();
            }

            if (ZetaDia.IsInGame)
            {
                InGamePulse();
            }
            else
            {
                OutOfGamePulse();
            }

            GameUI.SafeCheckClickButtons();
        }

        internal static void OutOfGamePulse()
        {
            ChangeMonitor.CheckForChanges();
            Player.UpdateOutOfGame();
            EventManager.Update();
            SelectBehavior();
        }

        internal static void InGamePulse()
        {
            ChangeMonitor.CheckForChanges();
            Player.Update();
            SelectBehavior();
        }

        /// <summary>
        /// Select the correct behavior.
        /// </summary>
        internal static void SelectBehavior()
        {
            if (!Service.IsConnected)
                return;

            if (DateTime.UtcNow.Subtract(_lastSelectedBehavior).TotalSeconds < 5)
                return;

            _lastSelectedBehavior = DateTime.UtcNow;

            if (ProfileManager.CurrentProfile == null)
            {
                CurrentBehavior = DefaultBehavior;
                return;
            }

            // Assign from tag that has been reached in profile.
            if (AutoFollowTag.Current != null && AutoFollowTag.Current.CurrentBehavior != null)
            {
                if (CurrentBehavior == AutoFollowTag.Current.CurrentBehavior)
                    return;

                CurrentBehavior = AutoFollowTag.Current.CurrentBehavior;
                return;
            }

            // Assign from tag within profile before tag has been reached.
            var profileTag = ProfileUtils.GetProfileTag("AutoFollow");
            if (profileTag != null)
            {
                var behaviorAttr = profileTag.Attribute("behavior");
                if (behaviorAttr != null && !string.IsNullOrEmpty(behaviorAttr.Value))
                {
                    AssignBehaviorByName(behaviorAttr.Value);
                    return;
                }

                CurrentBehavior = DefaultBehavior;
                return;
            }

            //// Check if our tag has been started within profile.
            //if (AutoFollowTag.Current != null && AutoFollowTag.Current.CurrentBehavior != null)
            //{
            //    if (CurrentBehavior == AutoFollowTag.Current.CurrentBehavior)
            //        return;

            //    CurrentBehavior = AutoFollowTag.Current.CurrentBehavior;
            //    return;
            //}

            // Check if profile contains our tag.       
            //if (ProfileHasTag("AutoFollow"))
            //{
            //    Log.Verbose("AutoFollow Tag was found in profile");
            //    CurrentBehavior = FollowerBehavior;
            //    return;
            //}

            //var profileTag = GetProfileTag("AutoFollow");
            //if (profileTag == null)
            //{
            //    if (profileTag.HasAttributes)
            //    {
            //        var behavior = profileTag.Attribute("behavior");
            //        if (behavior.Value.ToLower().Contains("leadermanual"));
            //    }
            //    var attr = 
            //}

            CurrentBehavior = LeaderBehavior;
        }

        /// <summary>
        /// Find a leader amongst the connected bots.
        /// Server is responsible for designating a leader, other bots will do as they are told, maybe.
        /// </summary>
        public static Message SelectLeader()
        {
            if (NumberOfConnectedBots <= 0)
                return ServerMessage;

            var leader = CurrentParty.FirstOrDefault(m => m.BehaviorType == BehaviorType.Lead);
            if (leader == null)
            {
                Log.Warn("Waiting for a leader...");
                return new Message();
            }

            if (CurrentLeader.OwnerId != leader.OwnerId)
            {
                Log.Warn("Selected new leader as {0} ({1})", leader.HeroAlias, leader.OwnerId);
            }

            return leader;
        }

        public static void AssignBehaviorByName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                IBehavior behavior;
                if (AutoFollow.Behaviors.Items.TryGetValue(name, out behavior) && CurrentBehavior != behavior)
                {
                    Log.Info("Loading behavior: {0}", name);
                    CurrentBehavior = behavior;
                }
            }
            else
            {
                Log.Info("Requested behavior '{0}' was not found", name);
                CurrentBehavior = AutoFollow.DefaultBehavior;
                BotMain.Stop();
            }
        }

        public static void DisablePlugin()
        {
            Enabled = false;
            var thisPlugin = PluginManager.Plugins.FirstOrDefault(p => p.Plugin.Name == "AutoFollow");
            if (thisPlugin != null)
                thisPlugin.Enabled = false;
        }

        #region IPlugin Members

        public Version Version
        {
            get { return AutoFollow.PluginVersion; }
        }

        public string Author
        {
            get { return "xzjv"; }
        }

        public string Description
        {
            get { return "Co-op made better"; }
        }

        public Window DisplayWindow
        {
            get { return UILoader.GetSettingsWindow(); }
        }

        public string Name
        {
            get { return "AutoFollow"; }
        }

        public void OnEnabled()
        {
            Enabled = true;
            Log.Info(" v{0} Enabled", Version);
            BotMain.OnStart += BotMain_OnStart;
            BotMain.OnStop += BotMain_OnStop;
            CurrentBehavior = DefaultBehavior;
            EventManager.Enable();
            BotHistory.Enable();
            TabUi.InstallTab();
            ChangeMonitor.Enable();

            Server.ServerStartAttempts = 0;
            Client.ConnectionAttempts = 0;

            Service.Connect();
            CommunicationThread.ThreadStart();

            // When start button is clicked, hooks are cleared,
            TreeHooks.Instance.OnHooksCleared += OnHooksCleared;
        }

        //private void OnProfileLoaded(object sender, EventArgs eventArgs)
        //{
        //    if (!Enabled) return;

        //    SelectBehavior();
        //}

        private void OnHooksCleared(object sender, EventArgs e)
        {
            // Need to activate current behavior to ensure its hooks are added before game is started.
            CurrentBehavior.Activate();
        }

        public void OnDisabled()
        {
            Enabled = false;
            Log.Info("Plugin disabled! ");

            if(CurrentBehavior != null)
                CurrentBehavior.Deactivate();

            BotMain.OnStart -= BotMain_OnStart;
            BotMain.OnStop -=  BotMain_OnStop;
            EventManager.Disable();
            EventManager.OnPulseOutOfGame += Pulse;
            Service.OnUpdatePreview -= ServiceOnUpdatePreview;
            BotHistory.Disable();
            TabUi.RemoveTab();
            ChangeMonitor.Disable();
        }

        public void OnPulse()
        {
            Pulse();
        }


        private void BotMain_OnStart(IBot bot)
        {
            if (!Service.IsConnected)
            {
                Service.Connect();
                CommunicationThread.ThreadStart();
            }

            SelectBehavior();
            CurrentBehavior.Activate();
        }

        private void BotMain_OnStop(IBot bot)
        {
            CurrentBehavior.Deactivate();
        }

        public void OnInitialize()
        {
            Conditions.Initialize();
            Service.Initialize();
        }

        public void OnShutdown()
        {
        }

        public bool Equals(IPlugin other)
        {
            return (other.Name == Name) && (other.Version == Version);
        }

        #endregion

        public static Message GetUpdatedMessage(Message message)
        {
            return CurrentParty.FirstOrDefault(p => p.HeroId == message.HeroId);
        }

        public static Vector3 GetUpdatedPosition(Message message)
        {
            var partyMember = GetUpdatedMessage(message);
            return partyMember != null ? partyMember.Position : Vector3.Zero;
        }

    }
}


