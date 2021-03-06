﻿using System.ComponentModel;
using System.Configuration;
using System.Runtime.Serialization;
using AutoFollow.Networking;
using AutoFollow.Resources;
using AutoFollow.UI;

namespace AutoFollow
{
    public class Settings
    {
        private static FileStore<NetworkSettings> _network;
        private static FileStore<CoordinationSettings> _coordination;
        private static FileStore<MiscSettings> _misc;
        private static FileStore<CombatSettings> _combat;

        public static NetworkSettings Network => _network.Source;
        public static CoordinationSettings Coordination => _coordination.Source;
        public static MiscSettings Misc => _misc.Source;
        public static CombatSettings Combat => _combat.Source;

        public static SettingsViewModel ViewModel { get; set; }

        static Settings()
        {
            Init();
        }

        private static void Init()
        {
            _network = new FileStore<NetworkSettings>();
            _coordination = new FileStore<CoordinationSettings>();
            _misc = new FileStore<MiscSettings>();
            _combat = new FileStore<CombatSettings>();

            ViewModel = new SettingsViewModel
            {
                Network = _network.Source,
                Coordination = _coordination.Source,
                Misc = _misc.Source,
                Combat = _combat.Source,
            };

            ApplyNetworkRoleSettings();

            UILoader.OnWindowClosed += SettingsWindow_Closed;
        }

        public static void SettingsWindow_Closed()
        {
            if (Network.ServerPort != Server.ServerUri.Port || Network.BindAddress != Server.ServerUri.OriginalString)
                Service.UpdateUri();

            ApplyNetworkRoleSettings();

            _network.Save();
            _coordination.Save();
            _misc.Save();
            _combat.Save();
        }

        public static void ApplyNetworkRoleSettings()
        {
            if (Network.Role == NetworkSettings.NetworkRole.Client)
            {
                Service.ConnectionMode = ConnectionMode.Client;
                Service.ForceConnectionMode = true;
            }
            else if (Network.Role == NetworkSettings.NetworkRole.Server)
            {
                Service.ConnectionMode = ConnectionMode.Server;
                Service.ForceConnectionMode = true;
            }
            else
            {
                Service.ForceConnectionMode = false;
            }
        }
    }

    public class SettingsViewModel
    {
        public NetworkSettings Network { get; set; }
        public CoordinationSettings Coordination { get; set; }
        public MiscSettings Misc { get; set; }
        public CombatSettings Combat { get; set; }
    }

    [DataContract]
    public class NetworkSettings : NotifyBase
    {
        private string _bindAddress;
        private int _serverPort;
        private int _updateInterval;
        private NetworkRole _role;

        [DataMember, Setting]
        [DefaultValue(10920)]
        public int ServerPort
        {
            get { return _serverPort; }
            set { SetField(ref _serverPort, value); }
        }

        [DataMember, Setting]
        [DefaultValue("localhost")]
        public string BindAddress
        {
            get { return _bindAddress; }
            set { SetField(ref _bindAddress, value); }
        }

        [DataMember, Setting]
        [DefaultValue(300)]
        public int UpdateInterval
        {
            get { return _updateInterval; }
            set { if (value >= 10) SetField(ref _updateInterval, value); }
        }

        public enum NetworkRole
        {
            None = 0,
            Auto,
            Client,
            Server
        }

        [DataMember, Setting]
        [DefaultValue(NetworkRole.Client)]
        public NetworkRole Role
        {
            get { return _role; }
            set
            {
                if (value == NetworkRole.None)
                    SetField(ref _role, NetworkRole.Client);
                else
                    SetField(ref _role, value);
            }
        }
    }


    [DataContract]
    public class CoordinationSettings : NotifyBase
    {
        private int _teleportDistance;
        private int _followDistance;
        private int _delayAfterJoinGame;
        private int _catchUpDistance;

        [DataMember, Setting]
        [DefaultValue(300)]
        public int TeleportDistance
        {
            get { return _teleportDistance; }
            set { if (value >= 50) SetField(ref _teleportDistance, value); }
        }

        [DataMember, Setting]
        [DefaultValue(10)]
        public int FollowDistance
        {
            get { return _followDistance; }
            set { if (value >= 3) SetField(ref _followDistance, value); }
        }

        [DataMember, Setting]
        [DefaultValue(40)]
        public int CatchUpDistance
        {
            get { return _catchUpDistance; }
            set { if (value >= 10f) SetField(ref _catchUpDistance, value); }
        }

        [DataMember, Setting]
        [DefaultValue(45)]
        public int DelayAfterJoinGame
        {
            get { return _delayAfterJoinGame; }
            set { SetField(ref _delayAfterJoinGame, value); }
        }

    }

    [DataContract]
    public class MiscSettings : NotifyBase
    {
        private bool _debugLogging;
        private bool _avoidUnknownPlayers;
        private bool _hideHeroName;
        private string _realId;
        private bool _isRealIdEnabled;
        private bool _inviteByParagon;
        private bool _alwaysAcceptInvites;
        private bool _alwaysEnablePlugin;

        [DataMember, Setting]
        [DefaultValue(false)]
        public bool DebugLogging
        {
            get { return _debugLogging; }
            set { SetField(ref _debugLogging, value); }
        }

        [DataMember, Setting]
        [DefaultValue(true)]
        public bool AvoidUnknownPlayers
        {
            get { return _avoidUnknownPlayers; }
            set { SetField(ref _avoidUnknownPlayers, value); }
        }

        [DataMember, Setting]
        [DefaultValue(true)]
        public bool HideHeroName
        {
            get { return _hideHeroName; }
            set { SetField(ref _hideHeroName, value); }
        }

        [DataMember, Setting]
        [DefaultValue("")]
        public string RealId
        {
            get { return _realId; }
            set { SetField(ref _realId, value); }
        }

        [DataMember, Setting]
        [DefaultValue(false)]
        public bool IsRealIdEnabled
        {
            get { return _isRealIdEnabled; }
            set { SetField(ref _isRealIdEnabled, value); }
        }

        [DataMember, Setting]
        [DefaultValue(false)]
        public bool InviteByParagon
        {
            get { return _inviteByParagon; }
            set { SetField(ref _inviteByParagon, value); }
        }

        [DataMember, Setting]
        [DefaultValue(false)]
        public bool AlwaysAcceptInvites
        {
            get { return _alwaysAcceptInvites; }
            set { SetField(ref _alwaysAcceptInvites, value); }
        }

        [DataMember, Setting]
        [DefaultValue(false)]
        public bool AlwaysEnablePlugin
        {
            get { return _alwaysEnablePlugin; }
            set { SetField(ref _alwaysEnablePlugin, value); }
        }


    }


    [DataContract]
    public class CombatSettings : NotifyBase
    {
        private bool _allowAvoidance;
        private bool _allowKiting;

        [DataMember, Setting]
        [DefaultValue(true)]
        public bool AllowAvoidance
        {
            get { return _allowAvoidance; }
            set { SetField(ref _allowAvoidance, value); }
        }

        [DataMember, Setting]
        [DefaultValue(true)]
        public bool AllowKiting
        {
            get { return _allowKiting; }
            set { SetField(ref _allowKiting, value); }
        }

    }

}
