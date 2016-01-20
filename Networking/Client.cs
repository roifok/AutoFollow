using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using AutoFollow.Resources;
using AutoFollow.UI.Settings;
using EventManager = AutoFollow.Events.EventManager;

namespace AutoFollow.Networking
{
    public class Client
    {
        private static ChannelFactory<IService> _httpFactory;
        private static IService _httpProxy;
        public static bool ClientInitialized { get; private set; }
        public static DateTime LastUnexpectedException = DateTime.MinValue;
        public static int ConnectionFailures { get; set; }
        public static DateTime LastExpectedException = DateTime.MinValue;
        public static int ExpectedExceptionCount { get; set; }
        public static DateTime LastClientUpdate = DateTime.MinValue;
        public delegate void ClientMessageDelegate(Message message);        
        public static event Server.ServiceDelegate OnClientInitialized;
        public static event ClientMessageDelegate OnClientUpdated;
        public static bool _firstConnectionAttempt = true;

        /// <summary>
        /// Initializes the Client connection to the Server
        /// </summary>
        public static void ClientInitialize()
        {
            if (!AutoFollow.Enabled)
                return;

            if (DateTime.UtcNow.Subtract(LastUnexpectedException).TotalSeconds < 5)
                return;

            ConnectionFailures++;

            try
            {
                if (!ClientInitialized && AutoFollow.Enabled)
                {
                    var serverPort = AutoFollowSettings.Instance.ServerPort;

                    Server.ServerUri = new Uri(Server.ServerUri.AbsoluteUri.Replace(Server._basePort.ToString(), serverPort.ToString()));

                    Log.Info("Initializing Client Service connection to {0} Attempt={1}", Server.ServerUri.AbsoluteUri + "Follow", ConnectionFailures);

                    var binding = new BasicHttpBinding
                    {
                        OpenTimeout = TimeSpan.FromMilliseconds(5000),
                        SendTimeout = TimeSpan.FromMilliseconds(5000),
                        CloseTimeout = TimeSpan.FromMilliseconds(5000),
                        MaxBufferSize = int.MaxValue,
                        MaxReceivedMessageSize = int.MaxValue,
                        MaxBufferPoolSize = int.MaxValue,
                    };

                    binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;

                    var endPointAddress = new EndpointAddress(Server.ServerUri.AbsoluteUri + "Follow/?wsdl");

                    _httpFactory = new ChannelFactory<IService>(binding, endPointAddress);
                    _httpProxy = _httpFactory.CreateChannel();
                 

                    if (OnClientInitialized != null)
                        OnClientInitialized.Invoke();

                    ClientInitialized = true;
                }
            }
            catch (Exception ex)
            {
                Log.Info("Exception in ClientInitialize() {0} Attempt={1}", ex, ConnectionFailures);
                LastUnexpectedException = DateTime.UtcNow;
            }

            if (ConnectionFailures > 5 && !ClientInitialized)
            {
                Service.ConnectionMode = ConnectionMode.Server;
            }
        }

        public static void ShutdownClient()
        {
            try
            {
                _httpFactory.Abort();
                _httpProxy = null;
            }
            catch (Exception)
            {
                _httpFactory = null;
            }
            
        }

        public static bool IsValid
        {
            get { return ClientInitialized; }
        }

        /// <summary>
        /// Called by Client, Receives Server Message and Sends Client Message
        /// </summary>
        internal static void ClientUpdate()
        {
            if (!AutoFollow.Enabled)
                return;
            
            if (Server.ServiceHost != null && Server.ServiceHost.State != CommunicationState.Closed)
            {
                Server.ShutdownServer();
            }

            if(!IsValid)
                ClientInitialize();

            try
            {
                var elapsed = DateTime.UtcNow.Subtract(LastClientUpdate).TotalMilliseconds;
                if (elapsed < 250 || elapsed < AutoFollowSettings.Instance.UpdateInterval)
                    return;

                if (DateTime.UtcNow.Subtract(LastUnexpectedException).TotalSeconds < 5)
                    return;

                if (DateTime.UtcNow.Subtract(LastExpectedException).TotalSeconds < 2)
                    return;

                _httpProxy.SendMessageToServer(new MessageWrapper
                {
                    PrimaryMessage = Player.Instance.Message
                });

                var messageWrapper = _httpProxy.GetMessageFromServer();

                UpdateDataAsClient(messageWrapper);

                Log.Debug("Communicating with {0} other bots. ServerMessage={0}", AutoFollow.NumberOfConnectedBots, messageWrapper);

                foreach (var e in messageWrapper.PrimaryMessage.Events)
                {
                    if(!EventManager.HasFired(e))
                        Log.Verbose("Received new unfired event {0} from {1}", e.Type, e.OwnerHeroName);
                }

                if (OnClientUpdated != null)
                    OnClientUpdated.Invoke(AutoFollow.ServerMessage);

                ConnectionFailures = 0;
                ExpectedExceptionCount = 0;
            }
            catch (EndpointNotFoundException ex)
            {
                if(!_firstConnectionAttempt)
                    Log.Info("EndpointNotFoundException: Could not get an update from the leader using {0}. Is the leader running? ({1})", _httpFactory.Endpoint.Address.Uri.AbsoluteUri, ex.Message);

                ClientInitialized = false;
                Service.ConnectionMode = ConnectionMode.Server;
            }
            catch (CommunicationException ex)
            {
                Log.Debug("CommunicationException. Failed to Communicate with Server on {0} Message={1}", _httpFactory.Endpoint.Address.Uri.AbsoluteUri, ex.Message);
                LastExpectedException = DateTime.UtcNow;
                ExpectedExceptionCount++;
            }
            catch (TimeoutException ex)
            {
                Log.Debug("CommunicationException. Server failed to respond (Thread: {0})", Thread.CurrentThread.ManagedThreadId);
                //_httpFactory.Endpoint.Address.Uri.AbsoluteUri, ex.Message, 
                LastExpectedException = DateTime.UtcNow;
                ExpectedExceptionCount++;
            }
            catch (Exception ex)
            {
                Log.Info("Exception: Could not get an update from the leader using {0}. Is the leader running?", _httpFactory.Endpoint.Address.Uri.AbsoluteUri);
                ClientInitialized = false;
                LastUnexpectedException = DateTime.UtcNow;
                Log.Info(ex.ToString());
                ConnectionFailures++;
            }            

            if (ConnectionFailures > 3)
            {
                Service.ConnectionMode = ConnectionMode.Server;
            }

            if (ExpectedExceptionCount > 20)
            {
                Service.ConnectionMode = ConnectionMode.Server;
            }

            _firstConnectionAttempt = false;
        }

        private static void UpdateDataAsClient(MessageWrapper messageWrapper)
        {
            AutoFollow.CurrentParty = new List<Message>(messageWrapper.OtherMessages) {messageWrapper.PrimaryMessage};
            AutoFollow.CurrentFollowers = AutoFollow.CurrentParty.Where(o => o.IsFollower).ToList();
            var leader = AutoFollow.CurrentParty.FirstOrDefault(o => o.IsLeader);
            AutoFollow.CurrentLeader = leader ?? new Message();
            AutoFollow.NumberOfConnectedBots = Service.GetSmoothedConnectedBotCount(AutoFollow.CurrentParty);
            AutoFollow.ServerMessage = messageWrapper.PrimaryMessage;
            EventManager.Add(AutoFollow.ServerMessage.Events);
            AutoFollow.SelectBehavior();
            LastClientUpdate = DateTime.UtcNow;
        }
    }
}