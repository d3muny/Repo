using UdonSharp;
using UnityEngine;

namespace SaccFlightAndVehicles
{
    public class SAV_AllRespawnButton : UdonSharpBehaviour
    {
        public SAV_SlotManager_SingleDebug Manager;

        public override void Interact()
        {
            if (Manager != null)
            {
                Manager.AllRespawn();
            }
        }
    }
}
