using System.Collections.Generic;

namespace SimpleFuelSwitch
{
    /// <summary>
    /// Handles the formatting of resource info that's displayed in the right-hand pane
    /// for switchable resource options.
    /// </summary>
    static class ResourceInfoFormatter
    {
        /// <summary>
        /// The name of the config node where the settings for the formatter are stored.
        /// </summary>
        public const string CONFIG_NODE_NAME = "InfoFormat";

        /// <summary>
        /// The reserved name to use for the config entry that specifies what format to use
        /// unless otherwise specified for a particular resourcesId.
        /// </summary>
        internal const string DEFAULT_ID = "default";

        internal const string UNITS_TAG = "%u";
        internal const string MASS_TAG = "%m";
        internal const string QUANTITY_TAG = "%q";

        private delegate string FormatFunction(SwitchableResource resource);

        private static readonly Dictionary<string, string> formatByResourcesId;
        private static readonly List<KeyValuePair<string, FormatFunction>> formatters;

        static ResourceInfoFormatter()
        {
            formatByResourcesId = new Dictionary<string, string>();
            formatByResourcesId[DEFAULT_ID] = QUANTITY_TAG;
            // note that we populate the default node here, so that the code will
            // work even if someone has done something silly like deleting
            // SimpleFuelSwitch.cfg.

            formatters = new List<KeyValuePair<string, FormatFunction>>();
            formatters.Add(new KeyValuePair<string, FormatFunction>(UNITS_TAG, UnitsOf));
            formatters.Add(new KeyValuePair<string, FormatFunction>(MASS_TAG, MassOf));
            formatters.Add(new KeyValuePair<string, FormatFunction>(QUANTITY_TAG, QuantityOf));
        }

        /// <summary>
        /// Given a resourcesId and a resource, produce a string representation.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static string Format(string resourcesId, SwitchableResource resource)
        {
            string text = FormatOf(resourcesId);
            for (int i = 0; i < formatters.Count; ++i)
            {
                if (text.Contains(formatters[i].Key))
                {
                    text = text.Replace(formatters[i].Key, formatters[i].Value(resource));
                }
            }
            return text;
        }

        /// <summary>
        /// Here when the game is loading on startup, assuming that the config node is
        /// actually present.
        /// </summary>
        /// <param name="config"></param>
        public static void LoadConfig(ConfigNode config)
        {
            for (int i = 0; i < config.values.Count; ++i)
            {
                ConfigNode.Value entry = config.values[i];
                Logging.Log("Info format: " + entry.name + " -> " + entry.value);
                formatByResourcesId[entry.name] = entry.value;
            }
        }

        /// <summary>
        /// Given a resourcesId, find the format string to use with it.
        /// </summary>
        /// <param name="resourcesId"></param>
        /// <returns></returns>
        private static string FormatOf(string resourcesId)
        {
            string format = null;
            if (formatByResourcesId.TryGetValue(resourcesId, out format))
            {
                return format;
            }
            else
            {
                return formatByResourcesId[DEFAULT_ID];
            }
        }

        /// <summary>
        /// Display the number of units of a resource.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        private static string UnitsOf(SwitchableResource resource)
        {
            return string.Format("{0:0.#}", resource.maxAmount);
        }

        /// <summary>
        /// Display the mass of a resource, in tons.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        private static string MassOf(SwitchableResource resource)
        {
            return LocalizeUtil.Format("#SimpleFuelSwitch_massTonsFormat", resource.maxAmount * resource.definition.density);
        }

        /// <summary>
        /// Display units of a resource if it's massless, mass in tons if it isn't.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        private static string QuantityOf(SwitchableResource resource)
        {
            return (resource.definition.density > 0) ? MassOf(resource) : UnitsOf(resource);
        }
    }
}
