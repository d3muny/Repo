// SLMH_VehicleSlot_Base.cs
// Final goal: Provide shared VehicleSlot foundation (common checks and logs).
// Version: ver01
// Change: Initial base class for future ModeA/ModeB split.
// Updated: 2026-03-07 23:42
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

        [Header("Release conditions (optional)")]
        [Tooltip("Pilot seat (SaccVehicleSeat). If assigned, OFF is blocked while occupied.")]
        public SaccVehicleSeat PrimarySeat;

        [Tooltip("If assigned, OFF is blocked unless the active vehicle is inside this collider bounds.")]
        public Collider ReleaseZone;

        [Header("Debug")]
        public bool EnableLocalDebugLogs = false;

        protected bool CanReleaseByCommonRule(Vector3 activePosition, bool isActiveLocal)
        {
            if (!isActiveLocal) { return true; }

            if (PrimarySeat != null && PrimarySeat.SeatOccupied)
            {
                return false;
            }

            if (ReleaseZone != null && !ReleaseZone.bounds.Contains(activePosition))
            {
                return false;
            }

            return true;
        }

        protected void DLog(string msg)
        {
            int localId = Utilities.IsValid(Networking.LocalPlayer) ? Networking.LocalPlayer.playerId : -1;
            Debug.Log("[Slot " + SlotId + "] L=" + localId + " | " + msg);
        }
    }
}
