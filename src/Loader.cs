namespace SimpleFuelSwitch
{
    /// <summary>
    /// Runs once during game startup. Loads custom SimpleFuelSwitch config that's used in various places.
    /// </summary>
    internal static class Loader
    {
        private const string MASTER_NODE_NAME = "SimpleFuelSwitch";

        private delegate void ConfigLoader(ConfigNode node);

        private static bool isInitialized = false;

        /// <summary>
        /// Here when the script starts up.
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;

            Logging.Log("Loading config");
            UrlDir.UrlConfig[] configs = GameDatabase.Instance.GetConfigs(MASTER_NODE_NAME);
            if (configs.Length < 1)
            {
                Logging.Error("Can't find main " + MASTER_NODE_NAME + " config node! Some features will be inoperable.");
                return;
            }
            ConfigNode masterNode = configs[0].config;
            ProcessMasterNode(masterNode);
        }

        /// <summary>
        /// Process the main IndicatorLights config node.
        /// </summary>
        /// <param name="masterNode"></param>
        private static void ProcessMasterNode(ConfigNode masterNode)
        {
            TryProcessChildNode(masterNode, ResourceInfoFormatter.CONFIG_NODE_NAME, ResourceInfoFormatter.LoadConfig);
            TryProcessChildNode(masterNode, ResourcePrimaryInfoFormatter.CONFIG_NODE_NAME, ResourcePrimaryInfoFormatter.LoadConfig);
        }

        /// <summary>
        /// Looks for a child with the specified name, and delegates to it if found.
        /// </summary>
        /// <param name="masterNode"></param>
        /// <param name="childName"></param>
        /// <param name="loader"></param>
        private static void TryProcessChildNode(ConfigNode masterNode, string childName, ConfigLoader loader)
        {
            ConfigNode child = masterNode.nodes.GetNode(childName);
            if (child == null)
            {
                Logging.Warn("Child node " + childName + " of master config node " + MASTER_NODE_NAME + " not found, skipping");
            }
            else
            {
                Logging.Log("Loading " + masterNode.name + " config: " + child.name);
                loader(child);
            }
        }
    }
}
