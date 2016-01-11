using System.Diagnostics;
using AutoFollow.UI.Settings;

namespace AutoFollow.Resources
{
    public static class Log
    {
        private static readonly log4net.ILog Logging = Zeta.Common.Logger.GetLoggerInstanceForType();

        private static string _lastLogMessage = "";

        public static void Info(string message, params object[] args)
        {
            var msg = ClassTag + string.Format(message, args);

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Info(msg);
        }

        public static void Info(string message)
        {
            var msg = ClassTag + message;

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Info(msg);
        }

        public static void Raw(string message)
        {
            Logging.Info(message);
        }

        public static void Raw(string message, params object[] args)
        {
            Logging.Info(string.Format(message, args));
        }

        public static void Warn(string message)
        {
            var msg = ClassTag + message;

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Warn(msg);
        }

        public static void Warn(string message, params object[] args)
        {
            var msg = ClassTag + (args.Length > 0 && message != null ? string.Format(message, args) : message);

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Warn(msg);
        }

        public static void Error(string message)
        {
            var msg = ClassTag + message;

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Error(msg);
        }

        public static void Error(string message, params object[] args)
        {
            var msg = ClassTag + string.Format(message, args);

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Error(msg);
        }

        public static void Verbose(string message, params object[] args)
        {
            if (!AutoFollowSettings.Instance.DebugLogging)
                return;

            var msg = ClassTag + string.Format(message, args);

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Debug(msg);
        }

        public static void Verbose(string message)
        {
            if (!AutoFollowSettings.Instance.DebugLogging)
                return;

            var msg = ClassTag + message;

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Debug(msg);
        }

        public static void Debug(string message, params object[] args)
        {
            if (!AutoFollowSettings.Instance.DebugLogging)
                return;

            var msg = ClassTag + string.Format(message, args);

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Debug(msg);
        }

        public static void Debug(string message)
        {
            if (!AutoFollowSettings.Instance.DebugLogging)
                return;

            var msg = ClassTag + message;

            if (_lastLogMessage == msg)
                return;

            _lastLogMessage = msg;
            Logging.Debug(msg);
        }

        private static string ClassTag
        {
            get
            {
                var frame = new StackFrame(2);
                var method = frame.GetMethod();
                var type = method.DeclaringType;

                if (type == null)
                    return "[AutoFollow] ";

                if (type.Namespace != null && type.Namespace.ToLowerInvariant().Contains("questtools"))
                    return "[AutoFollow][" + type.Name + "] ";

                if (type.Namespace == type.Name || type.Name.ToLowerInvariant().Contains("displayclass"))
                    return "[" + type.Namespace + "] ";

                return "[" + type.Namespace + "][" + type.Name + "] ";
            }
        }

    }
}
