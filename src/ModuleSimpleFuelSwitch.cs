namespace SimpleFuelSwitch
{
    /// <summary>
    /// This PartModule, when provided, allows the part to switch resource contents in the vehicle editor.
    /// The PartModule config will list one or more RESOURCE_OPTION sections, each of which has syntax
    /// similar to a stock RESOURCE block (i.e. you define name, amount, maxAmount).
    /// </summary>
    public class ModuleSimpleFuelSwitch : PartModule
    {
        // Placeholder for when config doesn't provide an (optional) string value.
        private const string DEFAULT_FLAG = "_default";

        // The set of available resources on the part.  Will be null if the part has no switchable
        // resources, in which case this whole part module basically does nothing. We normally don't
        // expect that to be a common occurrence, because there's no reason to add a
        // ModuleSimpleFuelSwitch to a part unless the part has resources to switch. However, we still
        // need to allow for the possibility, since there's nothing stopping someone from putting in
        // config that adds a ModuleSimpleFuelSwitch without adding the needed resource hooks.
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
            if (availableResources == null) return;
            currentResourcesId = availableResources.NextResourcesId(currentResourcesId);
            Logging.Log("Switched resources on " + part.name + " " + part.persistentId + " to " + availableResources[currentResourcesId].displayName);

            UpdateSelectedResources(true);
            OnResourcesSwitched();
        }

        private BaseEvent SwitchResourcesEvent { get { return Events["DoSwitchResourcesEvent"]; } }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!InitializeAvailableResources())
            {
                LogInactiveWarning();
                return;
            }

            // If there's a linked variant, we don't want a PAW button for switching
            // resources, since that'll be done by choosing a variant instead.
            if (availableResources.HasAnyLinkedVariants)
            {
                SwitchResourcesEvent.guiActiveEditor = false;
            }
        }

        /// <summary>
        /// Here when a part is initially spawned by clicking on its button on the parts
        /// panel in the editor. Not called when created otherwise, e.g. by alt-clicking
        /// to copy a part, or being instantiated as a symmetry counterpart.
        /// </summary>
        internal void OnPartCreated()
        {
            UpdateSelectedResources(false);
        }

        /// <summary>
        /// Here when any part is attached in the editor. Only called for the actually attached
        /// part, not for any child parts or symmetry counterparts.
        /// </summary>
        /// <param name="part"></param>
        internal static void OnPartAttached(Part part)
        {
            if (OnTreeAttached(part))
            {
                for (int i = 0; i < part.symmetryCounterparts.Count; ++i)
                {
                    OnTreeAttached(part.symmetryCounterparts[i]);
                }
            }
        }

        /// <summary>
        /// Here when a ship loads in the editor or rolling out to the launchpad.
        /// </summary>
        /// <param name="ship"></param>
        internal static void OnShipLoaded(ShipConstruct ship)
        {
            for (int i = 0; i < ship.parts.Count; ++i)
            {
                Part part = ship.parts[i];
                ModuleSimpleFuelSwitch module = TryFind(part);
                if (module != null) module.OnShipLoaded();
            }
        }

        /// <summary>
        /// Here when a vessel is loaded in flight.
        /// </summary>
        /// <param name="vessel"></param>
        internal static void OnVesselLoaded(Vessel vessel)
        {
            for (int i = 0; i < vessel.parts.Count; ++i)
            {
                Part part = vessel.parts[i];
                ModuleSimpleFuelSwitch module = TryFind(part);
                if (module != null) module.OnShipLoaded();
            }
        }

        /// <summary>
        /// Here when the part we're on was loaded on a ship in the editor, on initial vessel
        /// rollout, or when switching to a vessel.
        /// </summary>
        private void OnShipLoaded()
        {
            // This is needed because in certain circumstances (it's complicated, depends on the
            // precise sequence of building, saving, loading, launching, etc.), then when the ship
            // is loaded, it adds all the "missing" default resources even though the saved ship
            // doesn't actually contain them. So we need to do a scan through all the parts on the
            // ship, and for any that have ModuleSimpleFuelSwitch on them, check the resources
            // and remove any that don't belong based on the currently selected resource option.

            // To reproduce the case that needs this:
            // 1. create a ship in the editor
            // 2. switch some part so that some of the default resources aren't there anymore
            //    (e.g. a normally LFO part switches to have, say, LF-only)
            // 3. save the ship
            // 4. new
            // 5. load the ship.  Bingo, it has the extra resources in it.
            // 6. launch the ship. Bingo, it has the extra resources in it.

            // This function finds and strips out those unwanted "extra" resources.
            if (!InitializeAvailableResources())
            {
                LogInactiveWarning();
                return;
            }
            if (currentResourcesId == DEFAULT_FLAG)
            {
                currentResourcesId = availableResources.DefaultResourcesId;
            }

            SwitchableResourceSet.UpdatePartResourceList(part, currentResourcesId, false);
        }

        /// <summary>
        /// Recursively process every part in a tree, when it's attached. (This gets called
        /// on symmetry counterparts as well.) Returns true if the tree contains a
        /// ModuleSimpleFuelSwitch anywhere, false otherwise.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        private static bool OnTreeAttached(Part part)
        {
            bool result = SanitizeResources(part);
            if (part.children == null) return result;
            for (int i = 0; i < part.children.Count; ++i)
            {
                result |= OnTreeAttached(part.children[i]);
            }
            return result;
        }

        /// <summary>
        /// Here when a variant is applied to the part that this module is on.
        /// </summary>
        /// <param name="variant"></param>
        internal void OnVariantApplied(PartVariant variant)
        {
            if (!InitializeAvailableResources()) return;

            // Does this variant have any linked resource selection?
            SwitchableResourceSet.Selection selection = availableResources.TryFindLinkedVariant(variant.Name);
            if (selection == null) return; // nope, nobody cares

            // Found one.  Is that one already selected?
            if (selection.resourcesId == currentResourcesId) return; // Already selected, so nothing to do.

            // Okay, we need to switch to the new selection.
            currentResourcesId = selection.resourcesId;
            Logging.Log(
                "Changed variant on " + part.name + " " + part.persistentId + " to " + variant.Name
                + ", switching resources to " + availableResources[currentResourcesId].displayName);

            UpdateSelectedResources(true);
            OnResourcesSwitched();
        }

        /// <summary>
        /// Here when resources have been switched (either from clicking the button, or picking
        /// a linked variant).
        /// </summary>
        private void OnResourcesSwitched()
        {
            // Setting the part resources munges the PAW but doesn't flag it as
            // dirty, so we need to do that ourselves so that it'll redraw correctly
            // with the revised set of resources available.
            UIPartActionWindow window = UIPartActionController.Instance.GetItem(part);
            if (window != null)
            {
                window.CreatePartList(true);
                window.displayDirty = true;
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
            if (!InitializeAvailableResources())
            {
                LogInactiveWarning();
                return;
            }

            if (currentResourcesId == DEFAULT_FLAG)
            {
                currentResourcesId = availableResources.DefaultResourcesId;
            }
            SwitchableResourceSet.Selection selection = SwitchableResourceSet.UpdatePartResourceList(part, currentResourcesId, true);

            if (selection == null) return;

            // Set the name displayed in the PAW.
            SwitchResourcesEvent.guiName = string.Format(
                "{0}: {1}",
                availableResources.selectorFieldName,
                selection.displayName);

            // Also adjust any symmetry counterparts.
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

        /// <summary>
        /// Set up the available resources on the part. Returns true if they're set up.
        /// Returns false if the part has no available resources for switching.
        /// </summary>
        /// <returns></returns>
        internal bool InitializeAvailableResources()
        {
            if (availableResources == null)
            {
                availableResources = SwitchableResourceSet.ForPart(part.name);
                if (availableResources == null)
                {
                    // Part has no resources, don't enable the event.
                    SwitchResourcesEvent.guiActiveEditor = false;
                }
            }

            return availableResources != null;
        }

        /// <summary>
        /// Log a warning message when the module is inactive due to no switchable resources being configured.
        /// This generally indicates a configuration problem somewhere, since normally one wouldn't add
        /// a ModuleSimpleFuelSwitch to a part unless there were also switchable resources for the part.
        /// </summary>
        private void LogInactiveWarning()
        {
            Logging.Warn("Fuel switching disabled for " + part.name + " " + part.persistentId
                + " (no switchable resources configured");
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

        /// <summary>
        /// Here when the part's PAW is popped up.
        /// </summary>
        /// <param name="window"></param>
        internal void OnPartActionUIShown(UIPartActionWindow window)
        {
            // Update the PAW so that it's correctly configured.  That's because if we just
            // loaded the ship in the editor, it won't have been set up yet.
            OnResourcesSwitched();
        }

        /// <summary>
        /// If the part has a ModuleSimpleFuelSwitch, sanitize the resources and return true.
        /// Otherwise, do nothing and return false.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        private static bool SanitizeResources(Part part)
        {
            if (TryFind(part) == null) return false;
            // KSP 1.6 has a bug in Part.OnCopy that causes PartResourceList to get
            // corrupted with mismatched dictionary keys. To work around this, we have
            // to clear out and re-insert all the resources.
            //
            // Hopefully a future KSP update will fix the bug and this hack can be removed.
            ConfigNode[] nodes = new ConfigNode[part.Resources.Count];
            for (int i = 0; i < nodes.Length; ++i)
            {
                PartResource resource = part.Resources[i];
                nodes[i] = SwitchableResource.CreateResourceNode(
                    resource.resourceName,
                    resource.amount,
                    resource.maxAmount);
                nodes[i].AddValue("flowState", resource.flowState);
            }
            part.Resources.Clear();
            part.SimulationResources.Clear();
            for (int i = 0; i < nodes.Length; ++i)
            {
                part.Resources.Add(nodes[i]);
            }
            part.ResetSimulation();
            return true;
        }
    }
}
