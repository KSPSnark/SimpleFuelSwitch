using KSP.UI.Screens;
using UnityEngine;

namespace SimpleFuelSwitch
{
    /// <summary>
    /// Tracks part events in the editor, and when a part is attached that has a
    /// ModuleSimpleFuelSwitch on it, sends a notification to that module.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class EditorBehaviour : MonoBehaviour
    {
        public void Awake()
        {
            Logging.Log("Registering events");
            GameEvents.onEditorPartEvent.Add(OnEditorPartEvent);
            GameEvents.onEditorVariantApplied.Add(OnEditorVariantApplied);
            GameEvents.onEditorLoad.Add(OnEditorLoad);
        }

        public void OnDestroy()
        {
            Logging.Log("Unregistering events");
            GameEvents.onEditorPartEvent.Remove(OnEditorPartEvent);
            GameEvents.onEditorVariantApplied.Remove(OnEditorVariantApplied);
            GameEvents.onEditorLoad.Remove(OnEditorLoad);
        }

        /// <summary>
        /// Here when an event happens to the part in the editor.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="part"></param>
        private void OnEditorPartEvent(ConstructionEventType eventType, Part part)
        {
            switch (eventType)
            {
                case ConstructionEventType.PartCreated:
                    OnPartCreated(part);
                    break;
                case ConstructionEventType.PartAttached:
                    ModuleSimpleFuelSwitch.OnPartAttached(part);
                    break;
                default:
                    // don't care about anything else
                    break;
            }
        }

        /// <summary>
        /// Here when a part is initially spawned by clicking on its button on the parts
        /// panel in the editor. Not called when created otherwise, e.g. by alt-clicking
        /// to copy a part, or being instantiated as a symmetry counterpart.
        /// </summary>
        /// <param name="part"></param>
        private void OnPartCreated(Part part)
        {
            ModuleSimpleFuelSwitch module = ModuleSimpleFuelSwitch.TryFind(part);
            if (module != null) module.OnPartCreated();
        }

        /// <summary>
        /// Here when a variant is applied in the editor.
        /// </summary>
        /// <param name="part"></param>
        /// <param name="variant"></param>
        private void OnEditorVariantApplied(Part part, PartVariant variant)
        {
            ModuleSimpleFuelSwitch module = ModuleSimpleFuelSwitch.TryFind(part);
            if (module != null) module.OnVariantApplied(variant);
        }

        /// <summary>
        /// Here when a ship loads in the editor.
        /// </summary>
        /// <param name="ship"></param>
        /// <param name="loadType"></param>
        private void OnEditorLoad(ShipConstruct ship, CraftBrowserDialog.LoadType loadType)
        {
            ModuleSimpleFuelSwitch.OnShipLoaded(ship);
        }
    }
}
