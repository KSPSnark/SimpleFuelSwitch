using UnityEngine;

namespace SimpleFuelSwitch
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class FlightBehaviour : MonoBehaviour
    {
        public void Awake()
        {
            Logging.Log("Registering events");
            GameEvents.OnVesselRollout.Add(OnVesselRollout);
        }

        public void OnDestroy()
        {
            Logging.Log("Unregistering events");
            GameEvents.OnVesselRollout.Remove(OnVesselRollout);
        }

        /// <summary>
        /// Here when a vessel is rolled out to the launchpad ready for launch.
        /// </summary>
        /// <param name="data"></param>
        private void OnVesselRollout(ShipConstruct ship)
        {
            ModuleSimpleFuelSwitch.OnShipLoaded(ship);
        }
    }
}
