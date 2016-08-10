using System.Threading;
using AutoFollow.Resources;
using Zeta.Bot;

namespace AutoFollow.Networking
{
    public class CommunicationThread
    {
        static CommunicationThread()
        {
            BotMain.OnStop += ibot => ThreadShutdown();
        }

        private static Thread _communicationThread;

        public static bool ThreadIsRunning => _communicationThread != null && _communicationThread.IsAlive;

        public static void ThreadStart()
        {
            if (ThreadIsRunning)
                return;

            _communicationThread = new Thread(Service.Communicate)
            {
                Name = "AutoFollow Communication",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };

            Log.Debug("Starting Communication Thread Id={0}", _communicationThread.ManagedThreadId);

            _communicationThread.Start();
        }

        public static void ThreadShutdown()
        {
            Log.Debug("Shutting down thread");

            if (!ThreadIsRunning)
                return;

            _communicationThread.Abort();
            _communicationThread.Join();
        }
    }
}
