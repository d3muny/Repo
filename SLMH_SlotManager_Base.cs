// SLMH_SlotManager_Base.cs
// Final goal: Provide shared SlotManager foundation (refs, logs, common helpers).
// Version: ver01
// Change: Initial base class for future ModeA/ModeB split.
// Updated: 2026-03-07 23:42
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SLMH_SlotManager_Base : UdonSharpBehaviour
    {
        [Header("Debug")]
        public bool EnableDebugLogs = true;

        [Header("Slots")]
        public SLMH_VehicleSlot_SingleDebug[] Slots;

        [Header("LateJoin Bridge (child Udon)")]
        public SLMH_LateJoinSyncBridge LateJoinBridge;

        protected SLMH_VehicleSlot_SingleDebug GetSlotById(int slotId)
        {
            int count = (Slots != null) ? Slots.Length : 0;
            for (int i = 0; i < count; i++)
            {
                if (Slots[i] != null && Slots[i].SlotId == slotId)
                {
                    return Slots[i];
                }
            }
            return null;
        }

        protected string SafeName(VRCPlayerApi p)
        {
            return (p != null) ? p.displayName : "null";
        }

        protected void DLog(string msg)
        {
            if (!EnableDebugLogs) { return; }
            VRCPlayerApi owner = Networking.GetOwner(gameObject);
            int localId = Utilities.IsValid(Networking.LocalPlayer) ? Networking.LocalPlayer.playerId : -1;
            string ownerName = SafeName(owner);
            Debug.Log("[SlotMgr] L=" + localId + " Owner=" + ownerName + " | " + msg);
        }
    }
}
