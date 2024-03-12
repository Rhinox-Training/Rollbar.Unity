using System;
using System.Collections.Generic;

using Rhinox.Perceptor;
using Rollbar;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rhinox.Rollbar.Unity
{
    public class RollbarLogTarget : BaseLogTarget, IDisposable
    {
        private class ExceptionOccurenceTracker : IEquatable<ExceptionOccurenceTracker>
        {
            public int OccurencesSinceLastLog;
            public DateTime LastLog;
            public string Condition;
            public string StackTrace;
            
#region Equatable
            public bool Equals(ExceptionOccurenceTracker other)
            {
                return Condition == other.Condition && StackTrace == other.StackTrace;
            }

            public override bool Equals(object obj)
            {
                return obj is ExceptionOccurenceTracker other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Condition, StackTrace);
            }
#endregion
        }
        
        private readonly IRollbarLoggerConfig _config;
        private readonly IRollbar _logger;
        private readonly LogLevels _maxLogLevel;

        private HashSet<ExceptionOccurenceTracker> _exceptionOccurences = new HashSet<ExceptionOccurenceTracker>();

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
                var data = new ExceptionOccurenceTracker
                {
                    Condition = condition,
                    StackTrace = stacktrace,
                    LastLog = DateTime.Now
                };
                
                if (_exceptionOccurences.TryGetValue(data, out data))
                {
                    var timeBeforeRelog = TimeSpan.FromSeconds(_config.RollbarUnityOptions.SecondsBeforeExceptionGetsRelogged);
                    if (DateTime.Now - data.LastLog > timeBeforeRelog)
                        LogException(data);
                    else
                        data.OccurencesSinceLastLog += 1;
                }
                else
                    LogException(data);
            }
        }

        private void LogException(ExceptionOccurenceTracker e)
        {
            string msg = e.Condition;
            if (e.OccurencesSinceLastLog > 0)
                msg += $"; Occurrences since last appearance: {e.OccurencesSinceLastLog}";
            msg += $"\n{e.StackTrace}";
            
            e.LastLog = DateTime.Now;
            e.OccurencesSinceLastLog = 0;
            _exceptionOccurences.Add(e);
            _logger.Critical(msg);
        }
    }
}