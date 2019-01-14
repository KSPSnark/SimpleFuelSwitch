using System;
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
        }

        public void OnDestroy()
        {
            Logging.Log("Unregistering events");
            GameEvents.onEditorPartEvent.Remove(OnEditorPartEvent);
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
                default:
                    // don't care about anything else
                    break;
            }
        }

        private void OnPartCreated(Part part)
        {
            ModuleSimpleFuelSwitch module = ModuleSimpleFuelSwitch.TryFind(part);
            if (module != null) module.OnPartCreated();
        }
    }
}
