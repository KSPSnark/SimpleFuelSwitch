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
            GameEvents.onEditorPodPicked.Add(OnPartAttached);
            // We route onEditorPodPicked to OnPartAttached, because when the initial root
            // part is placed, we *do* get onEditorPodPicked, but we *don't* get OnEditorPartEvent.
            // We want to treat it like an "attach" (i.e. "I just became part of the ship,
            // initialize me").
        }

        public void OnDestroy()
        {
            Logging.Log("Unregistering events");
            GameEvents.onEditorPartEvent.Remove(OnEditorPartEvent);
            GameEvents.onEditorPodPicked.Remove(OnPartAttached);
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
                case ConstructionEventType.PartAttached:
                    OnPartAttached(part);
                    break;
                case ConstructionEventType.PartCopied:
                    OnPartCopied(part);
                    break;
                default:
                    // don't care about anything else
                    break;
            }
        }

        /// <summary>
        /// Here when a part is copied.
        /// </summary>
        /// <param name="part"></param>
        private void OnPartCopied(Part newPartCopy)
        {
            ModuleSimpleFuelSwitch module = ModuleSimpleFuelSwitch.TryFind(newPartCopy);
            if (module != null) module.OnPartCopied();
        }

        /// <summary>
        /// Here when a part is attached. Is also called for the initial root part
        /// of a vessel.
        /// </summary>
        /// <param name="part"></param>
        private void OnPartAttached(Part part)
        {
            ModuleSimpleFuelSwitch module = ModuleSimpleFuelSwitch.TryFind(part);
            if (module != null) module.OnPartAttached();
        }
    }
}
