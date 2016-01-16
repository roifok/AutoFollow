using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using AutoFollow.Resources;
using JetBrains.Annotations;
using Zeta.Common.Xml;
using Zeta.XmlEngine;

namespace AutoFollow.UI.Settings
{
    [XmlElement("AutoFollowSettings")]
    internal class AutoFollowSettings : XmlSettings
    {
        private static AutoFollowSettings _instance;

        private static readonly string StoragePath = Path.Combine(SettingsDirectory, "AutoFollow",
            Player.BattleTagHash.ToString(CultureInfo.InvariantCulture), "AutoFollowSettings.xml");

        private string _bindAddress;
        private bool _debugLogging;
        private int _serverPort;
        private int _updateInterval;
        private bool _avoidUnknownPlayers;
        private int _teleportDistance;
        private int _followDistance;

        public AutoFollowSettings() : base(StoragePath)
        {
        }

        public static AutoFollowSettings Instance
        {
            get { return _instance ?? (_instance = new AutoFollowSettings()); }
        }

        [XmlElement("ServerPort")]
        [Setting, DefaultValue(10920)]
        public int ServerPort
        {
            get { return _serverPort; }
            set { SetField(ref _serverPort, value); }
        }

        [XmlElement("BindAddress")]
        [Setting, DefaultValue("localhost")]
        public string BindAddress
        {
            get { return _bindAddress; }
            set { SetField(ref _bindAddress, value); }
        }

        [XmlElement("DebugLogging")]
        [Setting, DefaultValue(true)]
        public bool DebugLogging
        {
            get { return _debugLogging; }
            set { SetField(ref _debugLogging, value); }
        }

        [XmlElement("UpdateInterval")]
        [Setting, DefaultValue(300)]
        public int UpdateInterval
        {
            get { return _updateInterval; }
            set { if (value >= 10) SetField(ref _updateInterval, value); }
        }

        [XmlElement("AvoidUnknownPlayers")]
        [Setting, DefaultValue(true)]
        public bool AvoidUnknownPlayers
        {
            get { return _avoidUnknownPlayers; }
            set { SetField(ref _avoidUnknownPlayers, value); }
        }

        [XmlElement("TeleportDistance")]
        [Setting, DefaultValue(300)]
        public int TeleportDistance
        {
            get { return _teleportDistance; }
            set { if (value >= 50) SetField(ref _teleportDistance, value); }
        }

        [XmlElement("FollowDistance")]
        [Setting, DefaultValue(4)]
        public int FollowDistance
        {
            get { return _followDistance; }
            set { if (value >= 3) SetField(ref _followDistance, value); }
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}