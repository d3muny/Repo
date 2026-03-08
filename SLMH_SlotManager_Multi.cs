// SLMH_SlotManager_Multi.cs
// コードの最終目的: 複数機種(最大16)を1Slot内で切替するRuntimeChildを提供する
// バージョン名: ver01
// バージョン差分: 初版追加（Base連携用の最小APIと同期配列の枠を定義）
// バージョン更新日: 2026-03-08 12:00

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SLMH_SlotManager_Multi : SLMH_SlotManager_Base
    {
        [Header("Multi limits")]
        [Range(1, 16)] public int MaxSlotCount = 16;
        [Range(1, 16)] public int MaxVehicleCount = 16;

        // Runtime plan (fixed 16 for sync stability)
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
            if (MultiRuntime == null) { MultiRuntime = this; }
            DLog("Multi Start (scaffold) slotsMax=" + MaxSlotCount + " vehiclesMax=" + MaxVehicleCount);
        }

        // ---- RuntimeChild API (called from Base) ----
        public void Runtime_ApplyAllFromSyncedState()
        {
            // Multi slot apply will be added after VehicleSlot_Multi is defined.
            DLog("Runtime_ApplyAllFromSyncedState (Multi scaffold)");
        }

        public void Runtime_OnLocalInput_ToggleActive(int slotId)
        {
            DLog("Runtime_OnLocalInput_ToggleActive (Multi scaffold) slot=" + slotId);
        }

        public void Runtime_OnLocalInput_CyclePreview(int slotId, int dir)
        {
            DLog("Runtime_OnLocalInput_CyclePreview (Multi scaffold) slot=" + slotId + " dir=" + dir);
        }
    }
}
