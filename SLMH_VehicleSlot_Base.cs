// SLMH_VehicleSlot_Base.cs
// Final goal: Provide shared VehicleSlot foundation (common checks and logs).
// Version: ver02-01
// Change: Baseline unified to ver02-01 (stable snapshot).
// Updated: 2026-03-08 13:26
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SLMH_VehicleSlot_Base : UdonSharpBehaviour
    {
        [Header("Identity")]
        [Range(0, 31)] public int SlotId = 0;

        [Header("Debug")]
        public bool EnableLocalDebugLogs = false;

        protected void DLog(string msg)
        {
            int localId = Utilities.IsValid(Networking.LocalPlayer) ? Networking.LocalPlayer.playerId : -1;
            Debug.Log("[Slot " + SlotId + "] L=" + localId + " | " + msg);
        }
    }
}


