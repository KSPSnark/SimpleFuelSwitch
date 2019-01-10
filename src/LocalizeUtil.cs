using KSP.Localization;
using System.Collections.Generic;

namespace SimpleFuelSwitch
{
    static class LocalizeUtil
    {
        private static readonly Dictionary<string, string> formats = new Dictionary<string, string>();

        public static string GetString(string tag)
        {
            string localizedString = null;
            if (!Localizer.TryGetStringByTag(tag, out localizedString)) return null;
            return localizedString;
        }

        /// <summary>
        /// Format a string, given the tag for the format string and a list of insertion arguments.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string Format(string tag, params object[] args)
        {
            return string.Format(GetFormatString(tag), args);
        }

        /// <summary>
        /// Gets the localization string with the specified tag, and "decodes it"
        /// by replacing [] with {}.  The latter step is needed because string.Format()
        /// wants {} for insertion parameters, but the KSP config parser treats {} as
        /// special delimiters and hilarity ensues. So as a workaround, we use []
        /// instead of {} in the localization config, and swap out the delimiters here.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private static string GetFormatString(string tag)
        {
            string format = null;
            if (formats.TryGetValue(tag, out format)) return format;
            string localizedString = GetString(tag);
            if (localizedString != null)
            {
                localizedString = localizedString.Replace('[', '{').Replace(']', '}');
            }
            formats.Add(tag, localizedString);
            return localizedString;
        }
    }
}
