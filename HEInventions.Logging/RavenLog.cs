using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpRaven;
using SharpRaven.Data;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;
using System.IO;

namespace HEInventions.Logging
{
    /// <summary>
    /// This class is a wrapper for the SharpRaven module simplifying the configuration process and the 
    /// way in which Sentry events are sent up. The class follows a Singleton pattern allowing only one instance
    /// to be run at a time.
    /// </summary>
    public sealed class RavenLog
    {
        #region Properties and Fields
        /// <summary>
        /// Raven client for Sentry logging.
        /// </summary>
        private RavenClient _RavenClient;

        /// <summary>
        /// Only send sentry events that equal to, or are above, the defined threshold.
        /// </summary>
        private string _ErrorThreshold = "";

        /// <summary>
        /// Contains additional tags to send up with a Sentry event (e.g. the host_id of a machine).
        /// </summary>
        private Dictionary<string, string> _ExtraTags { get; set; }

        /// <summary>
        /// Triggered when a Sentry event is sent to the server.
        /// </summary>
        public event Action<SentryEvent> OnMessage;

        /// <summary>
        /// Stores the RavenLog instance state.
        /// </summary>
        private static RavenLog _Instance = null;

        /// <summary>
        /// Creates a RavenLog instance.
        /// </summary>
        public static RavenLog Instance
        {
            get
            {
                if (_Instance == null)
                    throw new InvalidOperationException("RavenLog must be configured before it can be used.");
                return _Instance;
            }
            private set
            {
                _Instance = value;
            }
        }
        #endregion

        #region RavenLog Configuration.
        /// <summary>
        /// Configures RavenLog and creates a new instance by setting the Sentry DSN, a severity threshold, and optional
        /// additional tags.
        /// </summary>
        /// <param name="sentryDSN">The Sentry server to post events to.</param>
        /// <param name="errorThreshold">The severity threshold - all errors equal to or above this will be logged.</param>
        /// <param name="extraTags">Additional tags that are useful to send up with a Sentry event.</param>
        public static void Configure(string sentryDSN, string errorThreshold, Dictionary<string, string> extraTags = null)
        {
            if (_Instance != null)
                throw new InvalidOperationException("RavenLog is already configured.");

            Instance = new RavenLog()
            {
                _RavenClient = new RavenClient(sentryDSN),
                _ErrorThreshold = errorThreshold,
                _ExtraTags = extraTags
            };
        }
        #endregion

        #region Sentry severity mapping methods
        /// <summary>
        /// Sentry event with severity level Error.
        /// </summary>
        /// <param name="msg">The error message.</param>
        /// <param name="exception">The associated exception object.</param>
        public void Error(String msg, Exception exception = null)
        {
            NewSentryEvent(msg, ErrorLevel.Error, exception);
        }

        /// <summary>
        /// Sentry event with severity level Warning.
        /// </summary>
        /// <param name="msg">The error message.</param>
        /// <param name="exception">The associated exception object.</param>
        public void Warn(String msg, Exception exception = null)
        {
            NewSentryEvent(msg, ErrorLevel.Warning, exception);
        }

        /// <summary>
        /// Sentry event with severity level Info.
        /// </summary>
        /// <param name="msg">The error message.</param>
        /// <param name="exception">The associated exception object.</param>
        public void Info(String msg, Exception exception = null)
        {
            NewSentryEvent(msg, ErrorLevel.Info, exception);
        }

        /// <summary>
        /// Sentry event with severity level Debug.
        /// </summary>
        /// <param name="msg">The error message.</param>
        /// <param name="exception">The associated exception object.</param>
        public void Debug(String msg, Exception exception = null)
        {
            NewSentryEvent(msg, ErrorLevel.Debug, exception);
        }

        /// <summary>
        /// Sentry event with severity level Fatal.
        /// </summary>
        /// <param name="msg">The error message.</param>
        /// <param name="exception">The associated exception object.</param>
        public void Fatal(String msg, Exception exception = null)
        {
            NewSentryEvent(msg, ErrorLevel.Fatal, exception);
        }
        #endregion

        #region Sentry event configuration
        /// <summary>
        /// Create a new Sentry event that includes a message, severity, and optional Exception data.
        /// SharpRaven's ErrorLevel enum goes from Fatal = 0 to Debug = 4.
        /// </summary>
        /// <param name="msg">The message that will appear on the Sentry log entries.</param>
        /// <param name="level">The error level attached to the Sentry event.</param>
        /// <param name="ex">The (optional) Exception object which is added to the Sentry event.</param>
        private void NewSentryEvent(String msg, ErrorLevel level, Exception ex)
        {
            if (_RavenClient == null)
                return;
            try
            {
                var threshold = Enum.Parse(typeof(ErrorLevel), _ErrorThreshold);
                if ((int)level > (int)threshold)
                    return;
            }
            catch (Exception e)
            {
                return;
            }

            SentryEvent sentryEvent;

            if (ex != null)
                sentryEvent = new SentryEvent(ex);
            else
                sentryEvent = new SentryEvent(msg);

            sentryEvent.Tags = _ExtraTags;
            sentryEvent.Message = msg;
            sentryEvent.Level = level;
            _RavenClient.Capture(sentryEvent);

            OnMessage?.Invoke(sentryEvent);
        }
        #endregion
    }
}
