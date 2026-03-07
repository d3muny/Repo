using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace SaccFlightAndVehicles
{
    public class SAV_DebugButton : UdonSharpBehaviour
    {
        public SAV_SlotManager_SingleDebug Manager;
        public int SlotId = 0;

        public override void Interact()
        {
            if (Manager != null)
            {
                Manager.ToggleActive(SlotId);
            }
        }
    }
}
