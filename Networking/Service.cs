using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceModel;
using System.Threading;
using AutoFollow.Resources;
using AutoFollow.UI.Settings;
using Zeta.Bot;
using Zeta.Game;

namespace AutoFollow.Networking
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class Service : IService
    {
        private static ConnectionMode _connectionMode;

        public static void Initialize()
        {
            Pulsator.OnPulse += (sender, args) => CommunicationThread.ThreadStart();
        }

        public MessageWrapper GetMessageFromServer()
        {
            try
            {
                var tw = new MessageWrapper
                {
                    PrimaryMessage = AutoFollow.ServerMessage,
                    OtherMessages = AutoFollow.ClientMessages.Values.ToList(),
                };

                return tw;
            }
            catch (Exception ex)
            {
                Log.Info("Exception in GetUpdate() {0}", ex);
                return new MessageWrapper
                {
                    PrimaryMessage = Player.Instance.Message,
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

        public static bool Connect(ConnectionMode mode = ConnectionMode.Client)
        {
            if (IsConnected)
                return true;

            if (Server.ServerStartAttempts > 20 && Client.ConnectionFailures > 20)
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

        public DateTime LastCommunicationTime = DateTime.MinValue;
        public static event ServiceDelegate OnUpdatePreview;
        public static event ServiceDelegate OnUpdated;

        public delegate void ServiceDelegate();

        internal static void Communicate()
        {
            var working = true;
            while (working)
            {
                try
                {
                    Thread.Sleep(Math.Max(250, AutoFollowSettings.Instance.UpdateInterval));

                    if (!AutoFollow.Enabled)
                        continue;

                    if (!BotMain.IsRunning || BotMain.IsPausedForStateExecution || ZetaDia.IsLoadingWorld || ZetaDia.IsPlayingCutscene)
                        continue;

                    if (!IsConnected)
                        Connect(ConnectionMode);

                    if (OnUpdatePreview != null)
                        OnUpdatePreview.Invoke();

                    if (ZetaDia.IsLoadingWorld)
                        continue;

                    if (ConnectionMode == ConnectionMode.Server)
                    {
                        Server.ServerUpdate();
                    }
                    else
                    {
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

        private static Dictionary<int, DateTime> BotsLastSeenTime = new Dictionary<int, DateTime>();

        /// <summary>
        /// Number of recently connected bots allowing for network issues.
        /// </summary>
        public static int GetSmoothedConnectedBotCount(List<Message> messages)
        {
            foreach (var message in messages)
            {
                if (!BotsLastSeenTime.ContainsKey(message.Id))
                    BotsLastSeenTime.Add(message.Id, DateTime.UtcNow);
                else
                    BotsLastSeenTime[message.Id] = DateTime.UtcNow;
            }

            return BotsLastSeenTime.Count(r => DateTime.UtcNow.Subtract(r.Value).TotalMilliseconds <= 10 && r.Key != Player.CurrentMessage.Id);
        }
    }
}