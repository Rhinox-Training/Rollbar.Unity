using System;
using Rhinox.Perceptor;
using Rollbar;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rhinox.Rollbar.Unity
{
    public class RollbarLogTarget : BaseLogTarget, IDisposable
    {
        private readonly IRollbarLoggerConfig _config;
        private readonly IRollbar _logger;
        private readonly LogLevels _maxLogLevel;

        public RollbarLogTarget(IRollbarLoggerConfig config, LogLevels maxLogLevel = LogLevels.Info)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            _config = config;
            _logger = RollbarFactory.CreateNew(_config);
            _maxLogLevel = maxLogLevel;

            if (_logger != null)
                Application.logMessageReceived += OnMessageReceived;
        }

        public void Dispose()
        {
            _logger?.Dispose();

            Application.logMessageReceived -= OnMessageReceived;
        }

        protected override void OnLog(LogLevels level, string message, Object associatedObject = null)
        {
            if (_logger == null || level < _maxLogLevel || _maxLogLevel == LogLevels.None)
                return;

            try
            {
                switch (level)
                {
                    case LogLevels.Trace:
                    case LogLevels.Debug:
                        _logger.Debug(message);
                        break;
                    case LogLevels.Info:
                        _logger.Info(message);
                        break;
                    case LogLevels.Warn:
                        _logger.Warning(message);
                        break;
                    case LogLevels.Error:
                        _logger.Error(message);
                        break;
                    case LogLevels.Fatal:
                        _logger.Critical(message);
                        break;
                    case LogLevels.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(level), level, null);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not log '{message}', reason:\n{e.ToString()}");
            }
        }
        
        private void OnMessageReceived(string condition, string stacktrace, LogType type)
        {
            if (type == LogType.Exception && _maxLogLevel != LogLevels.None)
            {
                _logger.Critical(condition + "\n" + stacktrace);
            }
        }
    }
}