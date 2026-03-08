// SLMH_ControllerRoot.cs
// コードの最終目的: SingleRuntimeが参照する共通ハブとしてSlot群とLateJoinBridgeを保持する
// バージョン名: ver01
// バージョン差分: 初版作成（Slot配列とLateJoinBridge参照を集約）
// バージョン更新日: 2026-03-08 10:53
using UdonSharp;
using UnityEngine;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SLMH_ControllerRoot : UdonSharpBehaviour
    {
        [Header("Slots")]
        public SLMH_VehicleSlot_Single[] Slots;

        [Header("LateJoin Bridge (child Udon)")]
        public SLMH_LateJoinSyncBridge LateJoinBridge;

        public int GetSlotCount()
        {
            return (Slots != null) ? Slots.Length : 0;
        }

        public SLMH_VehicleSlot_Single GetSlotAt(int index)
        {
            if (Slots == null) { return null; }
            if (index < 0 || index >= Slots.Length) { return null; }
            return Slots[index];
        }

        public bool HasLateJoinBridge()
        {
            return LateJoinBridge != null;
        }

        public void BindLateJoinBridge(SLMH_SlotManager_Single manager)
        {
            if (LateJoinBridge == null) { return; }
            LateJoinBridge.Manager = manager;
        }
    }
}
