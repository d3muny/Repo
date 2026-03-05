// SAV_VehicleSlot_SingleDebug.cs
// コードの最終目的: 単一機種SlotのFull/LowPoly切替と解除判定、遅延Respawnを適用する
// バージョン名: ver01
// バージョン差分: 初版ヘッダ整備
// バージョン更新日: 2026-03-05
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SAV_VehicleSlot_SingleDebug : UdonSharpBehaviour
    {
        [Header("Identity")]
        [Range(0, 15)] public int SlotId = 0;

        [Header("Single aircraft references (index 0)")]
        public GameObject FullRoot_0;
        public GameObject LowPolyRoot_0;
        public SaccEntitySendEvent Respawner_0;

        [Header("Release conditions (optional)")]
        [Tooltip("Pilot seat (SaccVehicleSeat). If assigned, OFF is blocked while occupied.")]
        public SaccVehicleSeat PrimarySeat;

        [Tooltip("If assigned, OFF is blocked unless the active vehicle is inside this collider bounds.")]
        public Collider ReleaseZone;

        [Header("Respawn timing")]
        [Range(0, 300)] public int RespawnDelayFrames = 30;

        private int _lastSeqApplied = int.MinValue;
        private int _lastRespawnSeqScheduled = int.MinValue;
        private bool _isActiveLocal = false;

        public void ApplyState(int activeIndex, int seq, bool force)
        {
            if (!force && seq == _lastSeqApplied) { return; }
            _lastSeqApplied = seq;

            bool shouldBeActive = (activeIndex == 0);

            // Apply visibility first (safe even if nulls)
            SetFull(shouldBeActive);
            SetLowPoly(!shouldBeActive);

            // Schedule respawn only on transition to active (or forced reapply with new seq while active)
            if (shouldBeActive && seq != _lastRespawnSeqScheduled)
            {
                _lastRespawnSeqScheduled = seq;
                SendCustomEventDelayedFrames(nameof(_RespawnDelayed_All), RespawnDelayFrames);
            }

            _isActiveLocal = shouldBeActive;
        }

        private void SetFull(bool on)
        {
            if (FullRoot_0 && FullRoot_0.activeSelf != on) { FullRoot_0.SetActive(on); }
            if (Respawner_0 && Respawner_0.gameObject && Respawner_0.gameObject.activeSelf != on) { Respawner_0.gameObject.SetActive(on); }
        }

        private void SetLowPoly(bool on)
        {
            if (LowPolyRoot_0 && LowPolyRoot_0.activeSelf != on) { LowPolyRoot_0.SetActive(on); }
        }

        public bool CanReleaseActive()
        {
            // If not active, always "releasable"
            if (!_isActiveLocal) { return true; }

            // Seat occupied check (if provided)
            if (PrimarySeat != null && PrimarySeat.SeatOccupied)
            {
                return false;
            }

            // Zone check (if provided)
            if (ReleaseZone != null)
            {
                Vector3 p = transform.position;
                // Prefer FullRoot position when available
                if (FullRoot_0 != null) { p = FullRoot_0.transform.position; }
                if (!ReleaseZone.bounds.Contains(p))
                {
                    return false;
                }
            }

            return true;
        }

        // Called by manager "AllRespawn" (immediate)
        public void TriggerRespawnNow_All()
        {
            if (Respawner_0 == null) { return; }
            Respawner_0.SendCustomNetworkEvent(NetworkEventTarget.All, "NormalEvent");
        }

        public void _RespawnDelayed_All()
        {
            // If already inactive by the time delay passes, do nothing
            if (!_isActiveLocal) { return; }
            if (Respawner_0 == null) { return; }

            Respawner_0.SendCustomNetworkEvent(NetworkEventTarget.All, "NormalEvent");
        }
    }
}
