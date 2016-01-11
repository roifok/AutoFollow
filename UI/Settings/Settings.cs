using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using AutoFollow.Resources;
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
        private bool _server;
        private int _serverPort;
        private int _updateInterval;

        public AutoFollowSettings() : base(StoragePath)
        {
        }

        public static AutoFollowSettings Instance
        {
            get { return _instance ?? (_instance = new AutoFollowSettings()); }
        }

        [XmlElement("ServerPort")]
        [DefaultValue(10920)]
        [Setting]
        public int ServerPort
        {
            get { return _serverPort; }
            set
            {
                _serverPort = value;
                OnPropertyChanged("ServerPort");
            }
        }

        [XmlElement("BindAddress")]
        [DefaultValue("localhost")]
        [Setting]
        public string BindAddress
        {
            get { return _bindAddress; }
            set
            {
                _bindAddress = value;
                OnPropertyChanged("BindAddress");
            }
        }

        [XmlElement("DebugLogging")]
        [DefaultValue(true)]
        [Setting]
        public bool DebugLogging
        {
            get { return _debugLogging; }
            set
            {
                _debugLogging = value;
                OnPropertyChanged("DebugLogging");
            }
        }

        [XmlElement("UpdateInterval")]
        [DefaultValue(300)]
        [Setting]
        public int UpdateInterval
        {
            get { return _updateInterval; }
            set
            {
                if (value >= 0)
                {
                    _updateInterval = value;
                    OnPropertyChanged("UpdateInterval");
                }
            }
        }



        //[XmlElement("Server")]
        //[DefaultValue(false)]
        //[Setting]
        //public bool Server
        //{
        //    get { return _server; }
        //    set
        //    {
        //        _server = value;
        //        OnPropertyChanged("Server");
        //    }
        //}
    }
}