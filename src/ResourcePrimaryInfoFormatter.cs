using System;
using System.Collections.Generic;

namespace SimpleFuelSwitch
{
    /// <summary>
    /// Handles the formatting of "primary info" about a selectable resource option,
    /// as displayed in the main center pane of the part's editor popup window.
    /// </summary>
    static class ResourcePrimaryInfoFormatter
    {
        /// <summary>
        /// The name of the config node where the settings for the formatter are stored.
        /// </summary>
        public const string CONFIG_NODE_NAME = "PrimaryInfoFormat";

        /// <summary>
        /// The reserved name to use for the config entry that specifies what format to use
        /// unless otherwise specified for a particular resourcesId.
        /// </summary>
        private const string DEFAULT_ID = ResourceInfoFormatter.DEFAULT_ID;
        private const int SUM_INDEX = -1;

        private const string TOTAL_UNITS_TAG = ResourceInfoFormatter.UNITS_TAG;
        private const string TOTAL_MASS_TAG = ResourceInfoFormatter.MASS_TAG;
        private const string TOTAL_QUANTITY_TAG = ResourceInfoFormatter.QUANTITY_TAG;

        private delegate string FormatFunction(SwitchableResource[] resources, int index);

        private static readonly Dictionary<string, string> formatByResourcesId;
        private static readonly List<Formatter> formatters;

        static ResourcePrimaryInfoFormatter()
        {
            formatByResourcesId = new Dictionary<string, string>();
            formatByResourcesId[DEFAULT_ID] = TOTAL_QUANTITY_TAG;
            // note that we populate the default node here, so that the code will
            // work even if someone has done something silly like deleting
            // SimpleFuelSwitch.cfg.

            formatters = new List<Formatter>();
            formatters.Add(new Formatter(TOTAL_UNITS_TAG, SUM_INDEX, UnitsOf));
            formatters.Add(new Formatter(TOTAL_MASS_TAG, SUM_INDEX, MassOf));
            formatters.Add(new Formatter(TOTAL_QUANTITY_TAG, SUM_INDEX, QuantityOf));
            for (int i = 0; i < 20; ++i)
            {
                int tagIndex = i + 1;
                formatters.Add(new Formatter(TOTAL_UNITS_TAG + tagIndex, i, UnitsOf));
                formatters.Add(new Formatter(TOTAL_MASS_TAG + tagIndex, i, MassOf));
                formatters.Add(new Formatter(TOTAL_QUANTITY_TAG + tagIndex, i, QuantityOf));
            }
            formatters.Sort();
        }

        /// <summary>
        /// Given a resourcesId and a set of resources, format a description thereof.
        /// </summary>
        /// <param name="resourcesId"></param>
        /// <param name="resources"></param>
        /// <returns></returns>
        public static string Format(string resourcesId, SwitchableResource[] resources)
        {
            string text = FormatOf(resourcesId);
            for (int i = 0; i < formatters.Count; ++i)
            {
                text = formatters[i].Format(text, resources);
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
                Logging.Log("Primary info format: " + entry.name + " -> " + entry.value);
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
        /// Display the total number of units of a resource.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static string UnitsOf(SwitchableResource[] resources, int index)
        {
            double units = 0.0;
            if (index == SUM_INDEX)
            {
                for (int i = 0; i < resources.Length; ++i)
                {
                    units += resources[i].maxAmount;
                }
            }
            else
            {
                units = resources[index].maxAmount;
            }
            return string.Format("{0:0.#}", units);
        }

        /// <summary>
        /// Display the total mass of a resource, in tons.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static string MassOf(SwitchableResource[] resources, int index)
        {
            double mass = 0.0;
            if (index == SUM_INDEX)
            {
                for (int i = 0; i < resources.Length; ++i)
                {
                    mass += resources[i].maxAmount * resources[i].definition.density;
                }
            }
            else
            {
                mass = resources[index].maxAmount * resources[index].definition.density;
            }
            return LocalizeUtil.Format("#SimpleFuelSwitch_massTonsFormat", mass);
        }

        /// <summary>
        /// Display total units of a resource if it's massless, total mass in tons if it isn't.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static string QuantityOf(SwitchableResource[] resources, int index)
        {
            if (index == SUM_INDEX)
            {
                for (int i = 0; i < resources.Length; ++i)
                {
                    if (resources[i].definition.density > 0) return MassOf(resources, index);
                }
                return UnitsOf(resources, index);
            }
            else
            {
                return (resources[index].definition.density > 0) ? MassOf(resources, index) : UnitsOf(resources, index);
            }
        }

        #region Formatter
        private class Formatter : IComparable
        {
            private readonly string tag;
            private readonly int index; // -1 means "sum"
            private readonly FormatFunction function;

            public Formatter(string tag, int index, FormatFunction function)
            {
                this.tag = tag;
                this.index = index;
                this.function = function;
            }

            public string Format(string text, SwitchableResource[] resources)
            {
                if (text.Contains(tag))
                {
                    return text.Replace(tag, function(resources, index));
                }
                else
                {
                    return text;
                }
            }

            public int CompareTo(object obj)
            {
                Formatter other = (Formatter)obj;
                return -tag.CompareTo(other.tag);
            }
        }
        #endregion // Formatter
    }
}
