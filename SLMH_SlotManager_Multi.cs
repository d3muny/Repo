// SLMH_SlotManager_Multi.cs
// Final goal: Provide multi-aircraft runtime child scaffold.
// Version: ver02-01
// Change: Baseline unified to ver02-01 (stable snapshot).
// Updated: 2026-03-08 13:26
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SLMH_SlotManager_Multi : UdonSharpBehaviour
    {
        [Header("Base")]
        public SLMH_SlotManager_Base ManagerBase;

        [Header("Multi limits")]
        [Range(1, 16)] public int MaxSlotCount = 16;
        [Range(1, 16)] public int MaxVehicleCount = 16;

        [UdonSynced] public int s0_active = -1;
        [UdonSynced] public int s1_active = -1;
        [UdonSynced] public int s2_active = -1;
        [UdonSynced] public int s3_active = -1;
        [UdonSynced] public int s4_active = -1;
        [UdonSynced] public int s5_active = -1;
        [UdonSynced] public int s6_active = -1;
        [UdonSynced] public int s7_active = -1;
        [UdonSynced] public int s8_active = -1;
        [UdonSynced] public int s9_active = -1;
        [UdonSynced] public int s10_active = -1;
        [UdonSynced] public int s11_active = -1;
        [UdonSynced] public int s12_active = -1;
        [UdonSynced] public int s13_active = -1;
        [UdonSynced] public int s14_active = -1;
        [UdonSynced] public int s15_active = -1;

        [UdonSynced] public int s0_preview = -1;
        [UdonSynced] public int s1_preview = -1;
        [UdonSynced] public int s2_preview = -1;
        [UdonSynced] public int s3_preview = -1;
        [UdonSynced] public int s4_preview = -1;
        [UdonSynced] public int s5_preview = -1;
        [UdonSynced] public int s6_preview = -1;
        [UdonSynced] public int s7_preview = -1;
        [UdonSynced] public int s8_preview = -1;
        [UdonSynced] public int s9_preview = -1;
        [UdonSynced] public int s10_preview = -1;
        [UdonSynced] public int s11_preview = -1;
        [UdonSynced] public int s12_preview = -1;
        [UdonSynced] public int s13_preview = -1;
        [UdonSynced] public int s14_preview = -1;
        [UdonSynced] public int s15_preview = -1;

        [UdonSynced] public int s0_seq = 0;
        [UdonSynced] public int s1_seq = 0;
        [UdonSynced] public int s2_seq = 0;
        [UdonSynced] public int s3_seq = 0;
        [UdonSynced] public int s4_seq = 0;
        [UdonSynced] public int s5_seq = 0;
        [UdonSynced] public int s6_seq = 0;
        [UdonSynced] public int s7_seq = 0;
        [UdonSynced] public int s8_seq = 0;
        [UdonSynced] public int s9_seq = 0;
        [UdonSynced] public int s10_seq = 0;
        [UdonSynced] public int s11_seq = 0;
        [UdonSynced] public int s12_seq = 0;
        [UdonSynced] public int s13_seq = 0;
        [UdonSynced] public int s14_seq = 0;
        [UdonSynced] public int s15_seq = 0;

        private void Start()
        {
            if (ManagerBase != null && ManagerBase.MultiRuntime == null) { ManagerBase.MultiRuntime = this; }
            Log("Multi Start (scaffold)");
        }

        public void Runtime_ApplyAllFromSyncedState()
        {
            Log("Runtime_ApplyAllFromSyncedState (scaffold)");
        }

        public void Runtime_OnLocalInput_ToggleActive(int slotId)
        {
            Log("Runtime_OnLocalInput_ToggleActive (scaffold) slot=" + slotId);
        }

        public void Runtime_OnLocalInput_CyclePreview(int slotId, int dir)
        {
            Log("Runtime_OnLocalInput_CyclePreview (scaffold) slot=" + slotId + " dir=" + dir);
        }

        public void Runtime_AllRespawn()
        {
            Log("Runtime_AllRespawn (scaffold)");
        }

        public void Runtime_OnOwnerLateJoinResyncDelayed() { }
        public void Runtime_OnOwnerLateJoinResyncSecondPass() { }
        public void Runtime_OnOwnerReserializeSnapshot() { }

        private void Log(string msg)
        {
            if (ManagerBase != null)
            {
                ManagerBase.Base_DLog(msg);
                return;
            }
            Debug.Log("[SlotMgr:Multi] " + msg);
        }
    }
}

