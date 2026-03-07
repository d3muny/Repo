// SLMH_VehicleSlot_SingleDebug.cs
// コードの最終目的: 単一機種SlotのFull/LowPoly切替と解除判定、遅延Respawnを適用する
// バージョン名: ver10
// バージョン差分: 接頭語をSAV_からSLMH_へ統一
// バージョン更新日: 2026-03-07 20:09
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SLMH_VehicleSlot_SingleDebug : UdonSharpBehaviour
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
        [Header("Debug")]
        [Tooltip("true: Active化時に遅延Respawnを送信 / false: 送信しない（同期切り分け用）")]
        public bool EnableRespawnOnActivate = false;
        public bool EnableLocalDebugLogs = false;

        private int _lastSeqApplied = int.MinValue;
        private int _lastTickApplied = int.MinValue;
        private int _lastRespawnSeqScheduled = int.MinValue;
        private bool _isActiveLocal = false;

        public bool IsVisualActive()
        {
            // Prefer local state cache, fallback to actual object active state
            if (FullRoot_0 != null) { return FullRoot_0.activeSelf; }
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
            if (debug)
            {
                DLog("ApplyState applied incoming(active=" + activeIndex + ",seq=" + seq + ",tick=" + tick + ") force=" + force + " allowRespawnTrigger=" + allowRespawnTrigger + " visualActive=" + IsVisualActive());
            }
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
            if (!_isActiveLocal) { return true; }

            if (PrimarySeat != null && PrimarySeat.SeatOccupied)
            {
                return false;
            }

            if (ReleaseZone != null)
            {
                Vector3 p = transform.position;
                if (FullRoot_0 != null) { p = FullRoot_0.transform.position; }
                if (!ReleaseZone.bounds.Contains(p))
                {
                    return false;
                }
            }

            return true;
        }

        public void TriggerRespawnNow_All()
        {
            if (Respawner_0 == null) { return; }
            Respawner_0.SendCustomNetworkEvent(NetworkEventTarget.All, "NormalEvent");
        }

        public void _RespawnDelayed_All()
        {
            if (!_isActiveLocal) { return; }
            if (Respawner_0 == null) { return; }

            Respawner_0.SendCustomNetworkEvent(NetworkEventTarget.All, "NormalEvent");
        }

        // Called by manager via SendCustomNetworkEvent(All)
        public void NetSetActiveVisual()
        {
            SetFull(true);
            SetLowPoly(false);
            _isActiveLocal = true;
            if (EnableLocalDebugLogs) { DLog("NetSetActiveVisual"); }
        }

        // Called by manager via SendCustomNetworkEvent(All)
        public void NetSetInactiveVisual()
        {
            SetFull(false);
            SetLowPoly(true);
            _isActiveLocal = false;
            if (EnableLocalDebugLogs) { DLog("NetSetInactiveVisual"); }
        }

        private void DLog(string msg)
        {
            int localId = Utilities.IsValid(Networking.LocalPlayer) ? Networking.LocalPlayer.playerId : -1;
            Debug.Log("[Slot " + SlotId + "] L=" + localId + " | " + msg);
        }
    }
}


