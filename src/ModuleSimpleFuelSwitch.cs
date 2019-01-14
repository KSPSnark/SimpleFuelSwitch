namespace SimpleFuelSwitch
{
    /// <summary>
    /// This PartModule, when provided, allows the part to switch resource contents in the vehicle editor.
    /// The PartModule config will list one or more RESOURCE_OPTION sections, each of which has syntax
    /// similar to a stock RESOURCE block (i.e. you define name, amount, maxAmount).
    /// </summary>
    public class ModuleSimpleFuelSwitch : PartModule
    {
        private const string DEFAULT_FLAG = "_default";

        private SwitchableResourceSet availableResources = null;

        /// <summary>
        /// Stores the resources ID of the currently selected set of resources. Optional; if
        /// omitted, will be set to the first defined resource option.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string currentResourcesId = DEFAULT_FLAG;

        /// <summary>
        /// Right-click menu item for switching part resources in the vehicle editor.
        /// </summary>
        [KSPEvent(active = true, guiActiveEditor = true, guiName = "Switch Resources")]
        // note, the "Switch Resources" on the above line is a dummy that will get replaced at runtime,
        // so a user should never see it, which is why I haven't bothered to localize it.
        public void DoSwitchResourcesEvent()
        {
            currentResourcesId = availableResources.NextResourcesId(currentResourcesId);
            Logging.Log("Switched resources on " + part.name + " " + part.craftID + " to " + availableResources[currentResourcesId].displayName);

            UpdateSelectedResources(true);
            OnResourcesSwitched();
        }
        private BaseEvent SwitchResourcesEvent { get { return Events["DoSwitchResourcesEvent"]; } }

        /// <summary>
        /// Here when a part is initially spawned by clicking on its button on the parts
        /// panel in the editor. Not called when created otherwise, e.g. by alt-clicking
        /// to copy a part, or being instantiated as a symmetry counterpart.
        /// </summary>
        internal void OnPartCreated()
        {
            UpdateSelectedResources(false);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            InitializeAvailableResources();
        }

        /// <summary>
        /// Here when resources have been switched.
        /// </summary>
        private void OnResourcesSwitched()
        {
            // Setting the part resources munges the PAW but doesn't flag it as
            // dirty, so we need to do that ourselves so that it'll redraw correctly
            // with the revised set of resources available.
            foreach (UIPartActionWindow window in FindObjectsOfType(typeof(UIPartActionWindow)))
            {
                if (window.part == part)
                {
                    window.displayDirty = true;
                    break;
                }
            }

            // We also need to fire off the "on ship modified" event, so that the engineer
            // report and any other relevant pieces of KSP UI will update as needed.
            if (EditorLogic.fetch != null)
            {
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
            }
        }

        /// <summary>
        /// Here when we need to set up the resources on a part (i.e. when first initializing, or
        /// when the player changes it by clicking the button).
        /// </summary>
        /// <param name="affectSymCounterparts"></param>
        private void UpdateSelectedResources(bool affectSymCounterparts)
        {
            InitializeAvailableResources();

            if (currentResourcesId == DEFAULT_FLAG)
            {
                currentResourcesId = availableResources.DefaultResourcesId;
            }
            SwitchableResourceSet.Selection selection = availableResources[currentResourcesId];

            // Set the name displayed in the PAW.
            SwitchResourcesEvent.guiName = string.Format(
                "{0}: {1}",
                availableResources.selectorFieldName,
                selection.displayName);

            // Actually set up the resources on the part.
            part.Resources.Clear();
            part.SimulationResources.Clear();
            for (int i = 0; i < selection.resources.Length; ++i)
            {
                PartResource added = part.Resources.Add(selection.resources[i].CreateResourceNode());
                part.SimulationResources.Add(added);
            }

            if (affectSymCounterparts)
            {
                for (int i = 0; i < part.symmetryCounterparts.Count; ++i)
                {
                    Part symCounterpart = part.symmetryCounterparts[i];
                    ModuleSimpleFuelSwitch switchModule = TryFind(symCounterpart);
                    if (switchModule != null)
                    {
                        switchModule.currentResourcesId = currentResourcesId;
                        switchModule.UpdateSelectedResources(false);
                    }
                }
            }
        }

        internal void InitializeAvailableResources()
        {
            if (availableResources == null)
            {
                availableResources = SwitchableResourceSet.ForPart(part.name);
            }
        }

        /// <summary>
        /// Try to find the first ModuleSimpleFuelSwitch on the part, or null if not found.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        internal static ModuleSimpleFuelSwitch TryFind(Part part)
        {
            if (part == null) return null;
            for (int i = 0; i < part.Modules.Count; ++i)
            {
                ModuleSimpleFuelSwitch module = part.Modules[i] as ModuleSimpleFuelSwitch;
                if (module != null) return module;
            }
            return null; // not found
        }
    }
}
