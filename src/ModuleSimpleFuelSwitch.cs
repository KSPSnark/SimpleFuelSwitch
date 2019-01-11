using System;

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

        // The following two variables are a kludge hack to work around KSP's
        // irritating behavior when copying parts. They are normally null all
        // the time, but become briefly non-null during part-copying events.
        private PartResource[] temporaryPartResourceCache = null;
        private Part temporaryCopiedFromPart = null;

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
            Logging.Log("Switched resources on " + part.name + " " + part.craftID + " to " + availableResources.DisplayNameOf(currentResourcesId));
            UpdateSelectedResources(true);

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
        private BaseEvent SwitchResourcesEvent { get { return Events["DoSwitchResourcesEvent"]; } }


        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            InitializeAvailableResources();
        }

        /// <summary>
        /// Shenanigans, Step 1:
        /// Here when the part is about to be copied. In order for KSP not to barf, we need
        /// to be "clean" (no resources) before the copy happens. Therefore, copy all the
        /// resources to a temporary cache that KSP doesn't know about, then clear out
        /// the "real" ones to keep KSP happy.
        /// </summary>
        /// <param name="asSymCounterpart"></param>
        public override void OnWillBeCopied(bool asSymCounterpart)
        {
            base.OnWillBeCopied(asSymCounterpart);
            if (asSymCounterpart) return; // we don't care about this case

            if (temporaryPartResourceCache != null)
            {
                throw new InvalidOperationException("Can't copy module, leftover resources found");
            }
            temporaryPartResourceCache = new PartResource[part.Resources.Count];
            for (int i = 0; i < temporaryPartResourceCache.Length; ++i)
            {
                temporaryPartResourceCache[i] = part.Resources[i];
            }
            part.Resources.Clear();
        }

        /// <summary>
        /// Shenanigans, Step 2:
        /// Okay, we successfully cloned this part to another, stripping ourselves of resources
        /// in the process. Unfortunately, we can't yet restore our resources, because KSP is
        /// not yet finished copying all the stuff across from this one to the copy. So we
        /// have to tell the new copy to remember where it was copied *from*. Why? Because
        /// silly, that's why.  See notes below.
        /// </summary>
        /// <param name="copyPartModule"></param>
        /// <param name="asSymCounterpart"></param>
        public override void OnWasCopied(PartModule copyPartModule, bool asSymCounterpart)
        {
            base.OnWasCopied(copyPartModule, asSymCounterpart);
            if (asSymCounterpart) return; // we don't care about this case

            ModuleSimpleFuelSwitch clone = (ModuleSimpleFuelSwitch)copyPartModule;
            if (clone.temporaryCopiedFromPart != null)
            {
                throw new InvalidOperationException("Clone already had a parent set, something's wrong");
            }
            clone.temporaryCopiedFromPart = part;
        }

        /// <summary>
        /// Shenanigans, Step 3:
        /// Whew, the copy process is finally done! But the original part that we copied from had
        /// its resources nuked, and this is the first (and only) chance we have to restore them...
        /// but this method gets called ON THE CLONE,  not on the part that was copied *from*.
        /// And (here's the silly part)... when we receive the "part copied" event, it doesn't
        /// say WHAT PART IT WAS COPIED FROM. So that's where we use our temporary storage
        /// variable to find it and fix it.
        /// </summary>
        internal void OnPartCopied()
        {
            if (temporaryCopiedFromPart == null) return; // copied as a symmetry counterpart, or something
            ModuleSimpleFuelSwitch module = TryFind(temporaryCopiedFromPart);
            temporaryCopiedFromPart = null;
            if (module != null)
            {
                module.OnCompletelyFinishedCopyingThisPart();
            }
        }

        /// <summary>
        /// Here when we're completely finished copying this part, and we can restore the
        /// original resources on the part.
        /// </summary>
        private void OnCompletelyFinishedCopyingThisPart()
        {
            if (temporaryPartResourceCache == null)
            {
                Logging.Error("No temporary resource cache found! Can't restore");
            }
            part.Resources.Clear();
            for (int i = 0; i < temporaryPartResourceCache.Length; ++i)
            {
                part.Resources.Add(temporaryPartResourceCache[i]);
            }
            temporaryPartResourceCache = null;
        }

        /// <summary>
        /// Here when the part that this module is on is attached to a vessel in the editor.
        /// </summary>
        internal void OnPartAttached()
        {
            // Update the resources for the part, and also any symmetry counterparts.
            UpdateSelectedResources(true);
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

            // Set the name displayed in the PAW.
            SwitchResourcesEvent.guiName = string.Format(
                "{0}: {1}",
                availableResources.selectorFieldName,
                availableResources.DisplayNameOf(currentResourcesId));

            // Actually set up the resources on the part.
            SwitchableResource[] selectedResources = availableResources[currentResourcesId];
            part.Resources.Clear();
            for (int i = 0; i < selectedResources.Length; ++i)
            {
                part.Resources.Add(selectedResources[i].CreateResourceNode());
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
