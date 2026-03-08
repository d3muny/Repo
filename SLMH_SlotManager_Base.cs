// SLMH_SlotManager_Base.cs
// Final goal: Provide shared SlotManager foundation (refs, logs, common helpers).
// Version: ver02
// Change: Centralize shared Slot/LateJoin bridge ownership and access helpers.
// Updated: 2026-03-08 10:23
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
        public SLMH_VehicleSlot_Single[] Slots;

        [Header("LateJoin Bridge (child Udon)")]
        public SLMH_LateJoinSyncBridge LateJoinBridge;

        protected int GetSlotCount()
        {
            return (Slots != null) ? Slots.Length : 0;
        }

        protected SLMH_VehicleSlot_Single GetSlotAt(int index)
        {
            if (Slots == null) { return null; }
            if (index < 0 || index >= Slots.Length) { return null; }
            return Slots[index];
        }

        protected SLMH_VehicleSlot_Single GetSlotById(int slotId)
        {
            int count = GetSlotCount();
            for (int i = 0; i < count; i++)
            {
                SLMH_VehicleSlot_Single slot = GetSlotAt(i);
                if (slot != null && slot.SlotId == slotId)
                {
                    return slot;
                }
            }
            return null;
        }

        protected void BindLateJoinBridge(SLMH_SlotManager_Single manager)
        {
            if (LateJoinBridge == null) { return; }
            LateJoinBridge.Manager = manager;
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

