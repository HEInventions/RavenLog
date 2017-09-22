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
        #region Constants
        /// <summary>
        /// If we cannot retrieve the machine settings, set a default value.
        /// </summary>
        public const string UndefinedSetting = "UNDEFINED";
        #endregion

        #region Properties and Fields
        /// <summary>
        /// Raven client for Sentry logging.
        /// </summary>
        private RavenClient ravenClient;

        /// <summary>
        /// Store the host ID from the data in the settings file.
        /// </summary>
        public string InstallationHostID = "";

        /// <summary>
        /// Only send sentry events that equal to, or are above, the defined threshold.
        /// </summary>
        public string ErrorThreshold = "";

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
        /// Configures RavenLog and creates a new instance by setting the Sentry DSN, a severity threshold, 
        /// specifying a settings file e.g. to configure the hostID, and which keys to look for in the settings file.
        /// </summary>
        /// <param name="sentryDSN">The Sentry server to post events to.</param>
        /// <param name="errorThreshold">The severity threshold - all errors equal to or above this will be logged.</param>
        /// <param name="settingsPath">Location of a settings file (typically JSON) which allows a hostID to be sent with 
        /// Sentry events.</param>
        /// <param name="configKeys">The keys to extract values from in the settings file.</param>
        public static void Configure(string sentryDSN, string errorThreshold, string settingsPath = "", StringCollection configKeys = null)
        {
            if (_Instance != null)
                throw new InvalidOperationException("RavenLog is already configured.");

            Instance = new RavenLog()
            {
                ravenClient = new RavenClient(sentryDSN),
                ErrorThreshold = errorThreshold,
                InstallationHostID = (settingsPath != "" && configKeys != null) ? 
                    GetHostnameFromSettingsFile(settingsPath, configKeys) : UndefinedSetting
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
            if (ravenClient == null)
                return;
            try
            {
                var threshold = Enum.Parse(typeof(ErrorLevel), ErrorThreshold);
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

            sentryEvent.Tags = new Dictionary<string, string> { { "host_id", InstallationHostID } };
            sentryEvent.Message = msg;
            sentryEvent.Level = level;
            ravenClient.Capture(sentryEvent);

            OnMessage?.Invoke(sentryEvent);
        }
        #endregion

        #region Parsing and utility methods
        /// <summary>
        /// Forms a hostname from the machine settings file (in JSON format) data.
        /// </summary>
        /// <param name="settingsFile">The machine settings JSON file.</param>
        /// <param name="configKeys">The keys to use to extract data from the file and 
        /// form the hostname string.</param>
        /// <returns></returns>
        private static string GetHostnameFromSettingsFile(String settingsFile, StringCollection configKeys)
        {
            try
            {
                string SettingsData, hostID = "";
                using (StreamReader reader = new StreamReader(settingsFile))
                {
                    SettingsData = reader.ReadToEnd();
                }
                JObject jsonResponse = JObject.Parse(SettingsData);
                foreach (var key in configKeys)
                {
                    hostID += $"{jsonResponse[key]}_";
                }
                return hostID.Remove(hostID.Length - 1);
            }
            catch (Exception ex)
            {
                return UndefinedSetting;
            }
        }
        #endregion
    }
}
