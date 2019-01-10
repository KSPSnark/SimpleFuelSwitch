using System;
using UnityEngine;

namespace SimpleFuelSwitch
{
    /// <summary>
    /// Utility wrapper for logging messages.
    /// </summary>
    static class Logging
    {
        private const string LOG_PREFIX = "[SimpleFuelSwitch] ";

        public static void Log(object message)
        {
            Debug.Log(LOG_PREFIX + message);
        }

        public static void Warn(object message)
        {
            Debug.LogWarning(LOG_PREFIX + message);
        }

        public static void Error(object message)
        {
            Debug.LogError(LOG_PREFIX + message);
        }

        public static void Exception(string message, Exception e)
        {
            Error(message + " (" + e.GetType().Name + ") " + e.Message + ": " + e.StackTrace);
        }

        public static void Exception(Exception e)
        {
            Error("(" + e.GetType().Name + ") " + e.Message + ": " + e.StackTrace);
        }

        public static string GetTitle(this Part part)
        {
            return (part == null) ? "<null part>" : ((part.partInfo == null) ? part.partName : part.partInfo.title);
        }
    }
}