// SLMH_VehicleSlot_Single.cs
// コードの最終目的: 単一機種SlotのFull/LW機切替と解除判定、遅延Respawnを適用する
// バージョン名: ver02-01
// バージョン差分: ver02-01基準化（安定スナップショット）
// バージョン更新日: 2026-03-08 13:26
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SLMH_VehicleSlot_Single : SLMH_VehicleSlot_Base
    {
        [Header("Release Zone")]
        [Tooltip("If assigned, OFF is blocked unless active vehicle center is inside this collider bounds.")]
        public Collider ReleaseZone;

        [Header("Aircraft References")]
        public GameObject FullRoot;
        [Tooltip("Pilot seat (SaccVehicleSeat). If assigned, OFF is blocked while occupied.")]
        public SaccVehicleSeat PrimarySeat;
        public SLMH_AnimSyncBridge AnimSyncBridge;
        public GameObject LightWeightRoot;
        public SaccEntitySendEvent Respawner;

        [Header("Respawn timing")]
        [Range(0, 300)] public int RespawnDelayFrames = 30;
        [Tooltip("true: Active化時に遅延Respawnを送信 / false: 送信しない（同期切り分け用）")]
        public bool EnableRespawnOnActivate = false;

        private int _lastSeqApplied = int.MinValue;
        private int _lastTickApplied = int.MinValue;
        private int _lastRespawnSeqScheduled = int.MinValue;
        private bool _isActiveLocal = false;

        public bool IsVisualActive()
        {
            if (FullRoot != null) { return FullRoot.activeSelf; }
            return _isActiveLocal;
        }

        public void ApplyState(int activeIndex, int seq, int tick, bool force, bool allowRespawnTrigger, bool managerDebugEnabled)
        {
            bool debug = managerDebugEnabled || EnableLocalDebugLogs;

            if (!force)
            {
                if (tick < _lastTickApplied)
                {
                    if (debug) { DLog("ApplyState ignored reason=olderTick incoming(active=" + activeIndex + ",seq=" + seq + ",tick=" + tick + ") last(seq=" + _lastSeqApplied + ",tick=" + _lastTickApplied + ")"); }
                    return;
                }
                if (tick == _lastTickApplied && seq <= _lastSeqApplied)
                {
                    if (debug) { DLog("ApplyState ignored reason=staleSeq incoming(active=" + activeIndex + ",seq=" + seq + ",tick=" + tick + ") last(seq=" + _lastSeqApplied + ",tick=" + _lastTickApplied + ")"); }
                    return;
                }
            }

            _lastSeqApplied = seq;
            _lastTickApplied = tick;

            bool shouldBeActive = (activeIndex == 0);

            SetFull(shouldBeActive);
            SetLowPoly(!shouldBeActive);

            if (EnableRespawnOnActivate && allowRespawnTrigger && shouldBeActive && seq != _lastRespawnSeqScheduled)
            {
                _lastRespawnSeqScheduled = seq;
                SendCustomEventDelayedFrames(nameof(_RespawnDelayed_All), RespawnDelayFrames);
            }

            _isActiveLocal = shouldBeActive;
            NotifyAnimBridgeStateChanged(shouldBeActive);

            if (debug)
            {
                DLog("ApplyState applied incoming(active=" + activeIndex + ",seq=" + seq + ",tick=" + tick + ") force=" + force + " allowRespawnTrigger=" + allowRespawnTrigger + " visualActive=" + IsVisualActive());
            }
        }

        private void SetFull(bool on)
        {
            if (FullRoot && FullRoot.activeSelf != on) { FullRoot.SetActive(on); }
            if (Respawner && Respawner.gameObject && Respawner.gameObject.activeSelf != on) { Respawner.gameObject.SetActive(on); }
        }

        private void SetLowPoly(bool on)
        {
            if (LightWeightRoot && LightWeightRoot.activeSelf != on) { LightWeightRoot.SetActive(on); }
        }

        public bool CanReleaseActive()
        {
            Vector3 p = transform.position;
            if (FullRoot != null) { p = FullRoot.transform.position; }
            if (!_isActiveLocal) { return true; }
            if (PrimarySeat != null && PrimarySeat.SeatOccupied) { return false; }
            if (ReleaseZone != null && !ReleaseZone.bounds.Contains(p)) { return false; }
            return true;
        }

        public void TriggerRespawnNow_All()
        {
            if (Respawner == null) { return; }
            Respawner.SendCustomNetworkEvent(NetworkEventTarget.All, "NormalEvent");
        }

        public void _RespawnDelayed_All()
        {
            if (!_isActiveLocal) { return; }
            if (Respawner == null) { return; }
            Respawner.SendCustomNetworkEvent(NetworkEventTarget.All, "NormalEvent");
        }

        public void NetSetActiveVisual()
        {
            SetFull(true);
            SetLowPoly(false);
            _isActiveLocal = true;
            NotifyAnimBridgeStateChanged(true);
            if (EnableLocalDebugLogs) { DLog("NetSetActiveVisual"); }
        }

        public void NetSetInactiveVisual()
        {
            SetFull(false);
            SetLowPoly(true);
            _isActiveLocal = false;
            NotifyAnimBridgeStateChanged(false);
            if (EnableLocalDebugLogs) { DLog("NetSetInactiveVisual"); }
        }

        private void NotifyAnimBridgeStateChanged(bool active)
        {
            if (AnimSyncBridge == null) { return; }

            if (!active)
            {
                AnimSyncBridge.NotifySlotBecameInactive();
                return;
            }

            AnimSyncBridge.NotifyStateApplied();
        }

    }
}



