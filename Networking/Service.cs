using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using AutoFollow.Resources;
using Zeta.Bot;

namespace AutoFollow.Networking
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class Service : IService
    {
        public delegate void ServiceDelegate();

        private static ConnectionMode _connectionMode;
        private static readonly Dictionary<int, DateTime> BotsLastSeenTime = new Dictionary<int, DateTime>();
        public DateTime LastCommunicationTime = DateTime.MinValue;

        public static ConnectionMode ConnectionMode
        {
            get { return _connectionMode; }
            set
            {
                if (_connectionMode != value)
                {
                    Log.Warn("Switching to {0} Mode", value);
                    _connectionMode = value;
                }
            }
        }

        public static bool IsConnected
        {
            get
            {
                if (ConnectionMode == ConnectionMode.Server && Server.IsValid)
                    return true;

                if (ConnectionMode == ConnectionMode.Client && Client.IsValid)
                    return true;

                return Client.IsValid || Server.IsValid;
            }
        }

        public static bool ForceConnectionMode { get; set; }

        public MessageWrapper GetMessageFromServer()
        {
            try
            {
                var tw = new MessageWrapper
                {
                    PrimaryMessage = AutoFollow.ServerMessage,
                    OtherMessages = AutoFollow.ClientMessages.Values.ToList()
                };

                return tw;
            }
            catch (Exception ex)
            {
                Log.Info("Exception in GetUpdate() {0}", ex);
                return new MessageWrapper
                {
                    PrimaryMessage = Player.CurrentMessage,
                    OtherMessages = new List<Message>()
                };
            }
        }

        public void SendMessageToServer(MessageWrapper wrapper)
        {
            if (wrapper != null)
            {
                lock (Server.Inbox)
                {
                    Server.Inbox.Enqueue(wrapper.PrimaryMessage);
                }
            }
        }

        public static void Initialize()
        {
            Pulsator.OnPulse += (sender, args) => CommunicationThread.ThreadStart();
        }

        public static bool Connect(ConnectionMode mode = ConnectionMode.Client)
        {
            if (IsConnected)
                return true;

            var tooManyAutoAttempts = Server.ServerStartAttempts > 20 && Client.ConnectionAttempts > 20;
            var tooManyClientAttempts = ForceConnectionMode && ConnectionMode == ConnectionMode.Client && Client.ConnectionAttempts > 100;
            var tooManyServerAttempts = ForceConnectionMode && ConnectionMode == ConnectionMode.Server && Server.ServerStartAttempts > 40;
            if (tooManyAutoAttempts || tooManyClientAttempts || tooManyServerAttempts)
            {
                Log.Info("Failed to Connect too many times, Disabling Plugin");
                AutoFollow.DisablePlugin();
            }

            if (ConnectionMode == ConnectionMode.Client)
            {
                Client.ClientInitialize();
            }
            else
            {
                Server.ServerInitialize();
            }

            return IsConnected;
        }

        public static event ServiceDelegate OnUpdatePreview;
        public static event ServiceDelegate OnUpdated;

        internal static void Communicate()
        {
            var working = true;
            while (working)
            {
                try
                {
                    Thread.Sleep(Math.Max(25, Settings.Network.UpdateInterval));

                    if (!AutoFollow.Enabled)
                    {
                        Server.ShutdownServer();
                        Client.ShutdownClient();
                        continue;
                    }
                        
                    if (!BotMain.IsRunning || BotMain.IsPausedForStateExecution)
                        continue;

                    if (!IsConnected)
                        Connect(ConnectionMode);

                    if (OnUpdatePreview != null)
                        OnUpdatePreview.Invoke();

                    if (ConnectionMode == ConnectionMode.Server)
                    {
                        Server.ServerUpdate();
                    }
                    else
                    {
                        if (Server.IsValid && Server.IsInitialized)
                        {
                            Server.ShutdownServer();
                        }

                        Client.ClientUpdate();
                    }

                    if (OnUpdated != null)
                        OnUpdated.Invoke();
                }
                catch (ThreadAbortException e)
                {
                    Thread.ResetAbort();
                    working = false;
                }
                catch (Exception ex)
                {
                    Log.Info("Error in Communicate Thread: {0}", ex);
                }
            }
        }

        /// <summary>
        /// The number of recently connected bots allowing for network issues.
        /// </summary>
        public static int GetSmoothedConnectedBotCount(List<Message> messages)
        {
            foreach (var message in messages)
            {
                if (!BotsLastSeenTime.ContainsKey(message.OwnerId))
                    BotsLastSeenTime.Add(message.OwnerId, DateTime.UtcNow);
                else
                    BotsLastSeenTime[message.OwnerId] = DateTime.UtcNow;
            }

            return
                BotsLastSeenTime.Count(
                    r =>
                        DateTime.UtcNow.Subtract(r.Value).TotalMilliseconds <= 10 &&
                        r.Key != Player.CurrentMessage.OwnerId);
        }

        /// <summary>
        /// Restarts client/server with an using an ServerURI from the Settings.
        /// </summary>
        public static void UpdateUri()
        {
            try
            {
                Server.ServerUri = new Uri("http://" + Settings.Network.BindAddress + ":" + Settings.Network.ServerPort);

                Log.Info("Networking address set to: {0}", Server.ServerUri);

                if (Server.ServiceHost != null && Server.ServiceHost.State != CommunicationState.Closed)
                    Server.ShutdownServer();

                if (ConnectionMode == ConnectionMode.Client)
                    Client.ShutdownClient();
            }
            catch (Exception ex)
            {
                Log.Info("Error in UpdateUri: {0}", ex);
            }
        }
    }
}