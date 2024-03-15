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
        private class OccurenceTracker : IEquatable<OccurenceTracker>
        {
            public int OccurencesSinceLastLog;
            public DateTime LastLog;
            public string Condition;
            public string StackTrace;

            public void Reset()
            {
                LastLog = DateTime.Now;
                OccurencesSinceLastLog = 0;
            }
            
            public void Increment()
            {
                OccurencesSinceLastLog += 1;
            }
            
#region Equatable
            public bool Equals(OccurenceTracker other)
            {
                return Condition == other.Condition && StackTrace == other.StackTrace;
            }

            public override bool Equals(object obj)
            {
                return obj is OccurenceTracker other && Equals(other);
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
        private readonly float _secondsBeforeRelog;

        private HashSet<OccurenceTracker> _recurringLogs = new HashSet<OccurenceTracker>();

        public RollbarLogTarget(IRollbarLoggerConfig config, LogLevels maxLogLevel = LogLevels.Info, float secondsBeforeRelog = 0)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            _config = config;
            _logger = RollbarFactory.CreateNew(_config);
            _maxLogLevel = maxLogLevel;
            _secondsBeforeRelog = secondsBeforeRelog;

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
                OccurenceTracker data;
                switch (level)
                {
                    case LogLevels.Trace:
                    case LogLevels.Debug:
                        if (ShouldLog(message, out data))
                        {
                            var msg = GetLogMessage(data);
                            _logger.Debug(msg);
                            data.Reset();
                        }
                        break;
                    case LogLevels.Info:
                        if (ShouldLog(message, out data))
                        {
                            var msg = GetLogMessage(data);
                            _logger.Info(msg);
                            data.Reset();
                        }
                        break;
                    case LogLevels.Warn:
                        if (ShouldLog(message, out data))
                        {
                            var msg = GetLogMessage(data);
                            _logger.Warning(msg);
                            data.Reset();
                        }
                        break;
                    case LogLevels.Error:
                        if (ShouldLog(message, out data))
                        {
                            var msg = GetLogMessage(data);
                            _logger.Error(msg);
                            data.Reset();
                        }
                        break;
                    case LogLevels.Fatal:
                        if (ShouldLog(message, out data))
                        {
                            var msg = GetLogMessage(data);
                            _logger.Critical(msg);
                            data.Reset();
                        }
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
            // We only log exceptions from this point
            if (type != LogType.Exception || _maxLogLevel == LogLevels.None) return;
            
            if (ShouldLog(condition, stacktrace, out var data))
            {
                var msg = GetLogMessage(data);
                _logger.Critical(msg);
                data.Reset();
            }
        }

        private bool ShouldLog(string condition, out OccurenceTracker data)
            => ShouldLog(condition, null, out data);

        private bool ShouldLog(string condition, string stacktrace, out OccurenceTracker data)
        {
            data = new OccurenceTracker
            {
                Condition = condition,
                StackTrace = stacktrace,
                LastLog = DateTime.Now
            };

            // If we've seen it before
            if (_recurringLogs.TryGetValue(data, out var recurringData))
            {
                data = recurringData;
                // Check how long ago
                var timeBeforeRelog = TimeSpan.FromSeconds(_secondsBeforeRelog);
                if (DateTime.Now - recurringData.LastLog > timeBeforeRelog)
                    return true;
                
                recurringData.Increment();
                return false;
            }
            
            // If we have not seen this before, just register it and return it (as it should log)
            _recurringLogs.Add(data);
            return true;
        }

        private string GetLogMessage(OccurenceTracker o)
        {
            string msg = o.Condition;
            if (o.OccurencesSinceLastLog > 1)
                msg += $"; Occurrences since last appearance: {o.OccurencesSinceLastLog}";
            if (!string.IsNullOrWhiteSpace(o.StackTrace))
                msg += $"\n{o.StackTrace}";

            return msg;
        }
    }
}