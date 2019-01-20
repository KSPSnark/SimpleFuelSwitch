using System;
using System.Text;

namespace SimpleFuelSwitch
{
    /// <summary>
    /// Represents a resource that's part of a set of resources defined on a ModuleSwitchableResoruces.
    /// It's defined in a block of config that looks just like a RESOURCE block on a Part config, except that
    /// it's defined inside a PartModule config.
    /// </summary>
    class SwitchableResource
    {
        private const string RESOURCE_TAG = "RESOURCE";
        private const string NAME_TAG = "name";
        private const string AMOUNT_TAG = "amount";
        private const string MAX_AMOUNT_TAG = "maxAmount";

        public readonly PartResourceDefinition definition;
        public readonly double amount;
        public readonly double maxAmount;

        private static readonly SwitchableResource[] EMPTY = new SwitchableResource[0];

        /// <summary>
        /// Given the root config node, load all the switchable resources in it.
        /// </summary>
        /// <param name="rootNode"></param>
        /// <returns></returns>
        public static SwitchableResource[] Load(ConfigNode rootNode)
        {
            if (rootNode.nodes == null) return EMPTY;
            ConfigNode[] nodes = rootNode.nodes.GetNodes(RESOURCE_TAG);
            if (nodes.Length < 1) return EMPTY;

            try
            {
                SwitchableResource[] resources = new SwitchableResource[nodes.Length];
                for (int i = 0; i < resources.Length; ++i)
                {
                    resources[i] = new SwitchableResource(nodes[i]);
                    for (int j = 0; j < i; ++j)
                    {
                        if (resources[i].definition.name.Equals(resources[j].definition.name))
                        {
                            throw new ArgumentException(rootNode.name + ": Contains duplicate definitions for resource '" + resources[i].definition.name + "'");
                        }
                    }
                }
                return resources;
            }
            catch (ArgumentException e)
            {
                Logging.Error(rootNode.name + ": Invalid " + RESOURCE_TAG + " found: " + e.Message);
                return EMPTY;
            }
        }

        /// <summary>
        /// Create a resource node representing this member.
        /// </summary>
        /// <returns></returns>
        public ConfigNode CreateResourceNode()
        {
            return CreateResourceNode(definition.name, amount, maxAmount);
        }

        /// <summary>
        /// Create a resource node with the specified name and amounts.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="amount"></param>
        /// <param name="maxAmount"></param>
        /// <returns></returns>
        public static ConfigNode CreateResourceNode(string resourceName, double amount, double maxAmount)
        {
            ConfigNode node = new ConfigNode(RESOURCE_TAG);
            node.AddValue(NAME_TAG, resourceName);
            node.AddValue(AMOUNT_TAG, amount);
            node.AddValue(MAX_AMOUNT_TAG, maxAmount);
            return node;
        }

        /// <summary>
        /// Given an array of SwitchableResource, get a long title to describe them.
        /// </summary>
        /// <param name="resources"></param>
        /// <returns></returns>
        public static string LongTitleOf(SwitchableResource[] resources)
        {
            if (resources.Length == 0) return LocalizeUtil.GetString("#SimpleFuelSwitch_noResources");
            StringBuilder builder = new StringBuilder(resources[0].definition.displayName);
            for (int i = 1; i < resources.Length; ++i)
            {
                builder.Append(" + ");
                builder.Append(resources[i].definition.displayName);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Construct a ResourceSetMember, given the config node containing it.
        /// </summary>
        /// <param name="node"></param>
        private SwitchableResource(ConfigNode node)
        {
            string name = GetRequiredString(node, NAME_TAG);
            definition = PartResourceLibrary.Instance.GetDefinition(name);
            if (definition == null)
            {
                throw new ArgumentException("No such resource '" + name + "' exists");
            }
            amount = GetRequiredDouble(node, AMOUNT_TAG);
            if (amount < 0)
            {
                throw new ArgumentException(AMOUNT_TAG + " can't be a negative number");
            }
            maxAmount = GetRequiredDouble(node, MAX_AMOUNT_TAG);
            if (maxAmount < amount)
            {
                throw new ArgumentException(MAX_AMOUNT_TAG + " must be greater than or equal to " + AMOUNT_TAG);
            }
        }

        private static String GetRequiredString(ConfigNode node, String tag)
        {
            String value = node.GetValue(tag);
            if (value == null)
            {
                throw new ArgumentException(node.name + " node is missing required value '" + tag + "'");
            }
            return value;
        }

        private static double GetRequiredDouble(ConfigNode node, String tag)
        {
            String strValue = GetRequiredString(node, tag);
            try
            {
                return double.Parse(strValue);
            }
            catch (FormatException e)
            {
                throw new ArgumentException(node.name + ": '" + tag + "' value must be a number", e);
            }
        }
    }
}
