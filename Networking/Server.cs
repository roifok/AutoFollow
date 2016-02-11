using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceModel;
using AutoFollow.Events;
using AutoFollow.Resources;
using Zeta.Common;

namespace AutoFollow.Networking
{
    public class Server
    {
        public delegate void ServerMessageDelegate(Dictionary<int, Message> followers);

        public delegate void ServiceDelegate();

        public static int _basePort = 10920;

        public static Uri ServerUri = new Uri("http://" + Settings.Network.BindAddress + ":" + Settings.Network.ServerPort);

        public static ServiceHost ServiceHost;
        public static DateTime LastServerUpdate = DateTime.MinValue;
        internal static ConcurrentQueue<Message> Inbox = new ConcurrentQueue<Message>();
        public static DateTime LastFailTime = DateTime.MinValue;
        public static bool IsInitialized { get; private set; }
        public static int ServerStartAttempts { get; set; }

        public static void ShutdownServer()
        {
            try
            {
                if (Server.ServiceHost == null)
                    return;

                if (Server.ServiceHost.State == CommunicationState.Faulted)
                {
                    Log.Info("Aborting Faulted Server Service");
                    Server.ServiceHost.Abort();
                    return;
                }

                Log.Info("Closing Server Service");
                Server.ServiceHost.Close();
            }
            catch (CommunicationObjectFaultedException ex)
            {
                Log.Debug("Client tried to close server connection but it is faulted.");
            }
            finally
            {
                Server.ServiceHost = null;
            }
        }

        public static bool IsValid
        {
            get
            {
                var result = ServiceHost != null && ServiceHost.State == CommunicationState.Opened;
                if (result)
                    ServerStartAttempts = 0;
                return result;
            }
        }

        public static event ServiceDelegate OnServerInitialized;
        public static event ServerMessageDelegate OnServerUpdated;

        /// <summary>
        /// Initializes the Server to receive follower connections
        /// </summary>
        internal static void ServerInitialize()
        {
            if (!AutoFollow.Enabled)
                return;

            if (Service.ConnectionMode != ConnectionMode.Server)
                return;

            if (DateTime.UtcNow < DontAttemptServerModeUntil)
                return;

            if (DateTime.UtcNow.Subtract(LastFailTime).TotalSeconds < 5)
                return;

            ServerStartAttempts++;

            try
            {
                var portIsTaken = true;
                ServerUri = new Uri(ServerUri.AbsoluteUri.Replace(_basePort.ToString(), Settings.Network.ServerPort.ToString()));
                _basePort = Settings.Network.ServerPort;
                var serverPort = _basePort;

                while (portIsTaken && AutoFollow.Enabled)
                {
                    var ipgp = IPGlobalProperties.GetIPGlobalProperties();
                    var tcpListeners = ipgp.GetActiveTcpListeners();

                    if (tcpListeners.Any(listner => listner.Port == serverPort))
                    {
                        portIsTaken = false;
                        serverPort += 1;
                    }
                    else
                        portIsTaken = false;
                }

                Log.Info("Initializing Server Service @ {0} Attempt={1}", ServerUri.AbsoluteUri, ServerStartAttempts);
                ServiceHost = new ServiceHost(typeof (Service), ServerUri);
                ServiceHost.AddServiceEndpoint(typeof (IService), new BasicHttpBinding(), "Follow");
                ServiceHost.Open();

                IsInitialized = true;

                if (OnServerInitialized != null)
                    OnServerInitialized.Invoke();
            }
            catch (AddressAlreadyInUseException ex)
            {
                Log.Verbose("Address already in use. Waiting for 30 seconds. Attempt={0}", ServerStartAttempts);
                Log.Debug(ex.ToString());
                DontAttemptServerModeUntil = DateTime.UtcNow.Add(TimeSpan.FromSeconds(30));
                LastFailTime = DateTime.UtcNow;
                Server.ShutdownServer();
                Client.ShutdownClient();

                if (!Service.ForceConnectionMode)
                {
                    Service.ConnectionMode = ConnectionMode.Client;
                }
            }
            catch (Exception ex)
            {
                Log.Verbose("Could not initialize service. Do you already have a leader started? Attempt={0}", ServerStartAttempts);
                Log.Debug(ex.ToString());
                LastFailTime = DateTime.UtcNow;
            }

            if (!Service.ForceConnectionMode && ServerStartAttempts > 5 && !IsValid)
            {
                Service.ConnectionMode = ConnectionMode.Client;
            }
        }

        public static DateTime DontAttemptServerModeUntil = DateTime.MinValue;

        /// <summary>
        /// Updates the Server message
        /// </summary>
        internal static void ServerUpdate()
        {
            if (!AutoFollow.Enabled)
                return;

            if (!IsValid)
            {
                Log.Info("Server is Invalid", Service.ConnectionMode);
                ShutdownServer();
                ServerInitialize();
                return;
            }

            if (DateTime.UtcNow.Subtract(LastServerUpdate).TotalMilliseconds < Settings.Network.UpdateInterval)
                return;

            ProcessClientMessages();
            UpdateDataAsServer();

            Log.Debug("Communicating with {0} other bots:", AutoFollow.NumberOfConnectedBots);

            if (DateTime.UtcNow.Subtract(LastSummaryTime).TotalSeconds > 2)
            {                
                Log.Debug("{0}", AutoFollow.ServerMessage.ShortSummary);

                foreach (var otherMessage in AutoFollow.ClientMessages.Values)
                {
                    Log.Debug("{0}", otherMessage.ShortSummary);
                }
                LastSummaryTime = DateTime.UtcNow;
            }

            if (OnServerUpdated != null)
                OnServerUpdated.Invoke(AutoFollow.ClientMessages);

            LastServerUpdate = DateTime.UtcNow;
        }

        public static DateTime LastSummaryTime { get; set; }

        private static void UpdateDataAsServer()
        {
            var serverMessage = Player.CurrentMessage;
            serverMessage.LeaderId = AutoFollow.SelectLeader().OwnerId;
            AutoFollow.ClientMessages.ForEach(cm => cm.Value.LeaderId = serverMessage.LeaderId);
            AutoFollow.CurrentParty = new List<Message>(AutoFollow.ClientMessages.Values) {serverMessage};
            AutoFollow.CurrentFollowers = AutoFollow.CurrentParty.Where(o => o.IsFollower).ToList();
            var leader = AutoFollow.CurrentParty.FirstOrDefault(o => o.IsLeader);
            AutoFollow.CurrentLeader = leader ?? new Message();
            AutoFollow.NumberOfConnectedBots = Service.GetSmoothedConnectedBotCount(AutoFollow.CurrentParty);
            AutoFollow.ServerMessage = serverMessage;
        }

        internal static void Start()
        {
            if (!CommunicationThread.ThreadIsRunning)
                CommunicationThread.ThreadStart();
        }

        internal static void Stop()
        {
            if (ServiceHost != null)
            {
                Log.Info("Stopped Server on '{0}' ", ServerUri.AbsoluteUri + "Follow");
                ServiceHost.Close();
                ServiceHost = null;
            }

            CommunicationThread.ThreadShutdown();
            IsInitialized = false;
        }

        private static void ProcessClientMessages()
        {
            var messages = new Queue<Message>();

            try
            {
                lock (Inbox)
                {
                    while (Inbox.ToList().Any())
                    {
                        Message msg;
                        if (Inbox.TryDequeue(out msg) && msg != null)
                            messages.Enqueue(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Info(ex.ToString());
            }

            Log.Verbose("Processing {0} Client Messages", messages.Count);

            while (messages.ToList().Any())
            {
                var message = messages.Dequeue();
                if (message == null)
                    continue;

                try
                {
                    if (message.OwnerId != 0)
                    {
                        if (AutoFollow.ClientMessages.ContainsKey(message.OwnerId))
                            AutoFollow.ClientMessages[message.OwnerId] = message;
                        else
                            AutoFollow.ClientMessages.Add(message.OwnerId, message);
                    }

                    EventManager.Add(message.Events);
                }
                catch (Exception ex)
                {
                    Log.Info("Exception receiving update from client!");
                    Log.Info(ex.ToString());
                    return;
                }
            }

            // Clean up old messages
            var toRemove = (from message in AutoFollow.ClientMessages
                            where DateTime.UtcNow.Subtract(message.Value.LastUpdated).TotalSeconds >= 5
                            select message.Key).ToList();

            toRemove.ForEach(key => AutoFollow.ClientMessages.Remove(key));

        }
    }
}