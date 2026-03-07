// SAV_SlotManager_SingleDebug.cs
// コードの最終目的: Slot状態の同期管理を一元化し、Full/LowPoly切替とAll Respawnを制御する
// バージョン名: ver16
// バージョン差分: LateJoin再同期は「インスタンスマスターがオーナー取得して返す」経路に一本化
// バージョン更新日: 2026-03-07 16:57

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SAV_SlotManager_SingleDebug : UdonSharpBehaviour
    {
        private int _pendingSlotId = -1;
        private int _pendingNext = -1;
        private bool _pendingToggle = false;
        private int _pendingRetryCount = 0;
        private bool _lateJoinResyncRequested = false;

        [Header("Debug")]
        public bool EnableDebugLogs = true;

        [Header("Slots")]
        public SAV_VehicleSlot_SingleDebug[] Slots;

        // ---- Synced state (per slot, fixed max = 16) ----
        // active: -1 = inactive (LowPoly), 0 = active (Full)
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

        // seq increments whenever state changes
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

        // tick is a global monotonic write id (prevents stale overwrite)
        [UdonSynced] public int s0_tick = 0;
        [UdonSynced] public int s1_tick = 0;
        [UdonSynced] public int s2_tick = 0;
        [UdonSynced] public int s3_tick = 0;
        [UdonSynced] public int s4_tick = 0;
        [UdonSynced] public int s5_tick = 0;
        [UdonSynced] public int s6_tick = 0;
        [UdonSynced] public int s7_tick = 0;
        [UdonSynced] public int s8_tick = 0;
        [UdonSynced] public int s9_tick = 0;
        [UdonSynced] public int s10_tick = 0;
        [UdonSynced] public int s11_tick = 0;
        [UdonSynced] public int s12_tick = 0;
        [UdonSynced] public int s13_tick = 0;
        [UdonSynced] public int s14_tick = 0;
        [UdonSynced] public int s15_tick = 0;

        [UdonSynced] public int writeCounter = 0;
        [UdonSynced] public int lastWriteSlot = -1;
        [UdonSynced] public int lastWriteByPlayerId = -1;
        [UdonSynced] public int lastWriteActive = -1;
        [UdonSynced] public int lastWriteSeq = 0;
        [UdonSynced] public int lastWriteTick = 0;
        [UdonSynced] public int syncEpoch = 0;

        private int _lastAppliedEpoch = int.MinValue;

        private void Start()
        {
            _lastAppliedEpoch = syncEpoch;
            ApplyAll(true);
            DLog("Start ApplyAll(force=true)");

            // Late join helper: request a resync from Instance Master once.
            if (!_lateJoinResyncRequested)
            {
                _lateJoinResyncRequested = true;
                SendCustomEventDelayedSeconds(nameof(_RequestLateJoinResyncFromMaster), 1.2f);
            }
        }

        public override void OnDeserialization()
        {
            bool forceByEpoch = (syncEpoch != _lastAppliedEpoch);
            _lastAppliedEpoch = syncEpoch;
            ApplyAll(forceByEpoch);
            DLog("OnDeserialization fromPlayerId=" + lastWriteByPlayerId + " slot=" + lastWriteSlot + " active=" + lastWriteActive + " seq=" + lastWriteSeq + " tick=" + lastWriteTick + " epoch=" + syncEpoch + " forceByEpoch=" + forceByEpoch);
            if (lastWriteSlot >= 0 && lastWriteSlot <= 15)
            {
                DLog("OnDeserialization slotState " + FormatSlotState(lastWriteSlot));
            }
        }

        // Backward compatibility for existing button wiring
        public void ToggleActive(int slotId)
        {
            RequestToggleActive(slotId);
        }

        // UI calls this
        public void RequestToggleActive(int slotId)
        {
            if (!Utilities.IsValid(Networking.LocalPlayer)) { return; }
            if (slotId < 0 || slotId > 15) { return; }

            int cur = GetActive(slotId);
            int next;

            if (cur == 0)
            {
                SAV_VehicleSlot_SingleDebug slot = GetSlot(slotId);
                if (slot != null && !slot.CanReleaseActive())
                {
                    DLog("RequestToggleActive blocked by CanReleaseActive slot=" + slotId);
                    return;
                }
                next = -1;
            }
            else
            {
                next = 0;
            }

            DLog("RequestToggleActive actor=" + Networking.LocalPlayer.playerId + ":" + Networking.LocalPlayer.displayName + " slot=" + slotId + " cur=" + cur + " next=" + next + " isOwner=" + Networking.IsOwner(gameObject));

            // Ensure we are owner before changing synced vars
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

                // Apply after ownership is actually transferred
                _pendingSlotId = slotId;
                _pendingNext = next;
                _pendingToggle = true;
                _pendingRetryCount = 0;
                SendCustomEventDelayedFrames(nameof(_TryApplyPendingToggle), 1);
                return;
            }

            // Already owner: apply immediately
            ApplyToggle(slotId, next);
        }

        public override void OnOwnershipTransferred(VRCPlayerApi newOwner)
        {
            DLog("OnOwnershipTransferred newOwner=" + SafeName(newOwner) + " localIsOwner=" + Networking.IsOwner(gameObject));
            if (Networking.IsOwner(gameObject))
            {
                SendCustomEventDelayedFrames(nameof(_OwnerReserializeSnapshot), 2);
            }
            if (_pendingToggle && Networking.IsOwner(gameObject))
            {
                _pendingToggle = false;
                ApplyToggle(_pendingSlotId, _pendingNext);
            }
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            DLog("OnPlayerJoined player=" + player.playerId + ":" + SafeName(player) + " localIsOwner=" + Networking.IsOwner(gameObject));
            if (Networking.IsOwner(gameObject))
            {
                // Late join path: send authoritative synced state after a short delay.
                SendCustomEventDelayedSeconds(nameof(_OwnerLateJoinResyncDelayed), 1f);
            }
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            DLog("OnPlayerLeft player=" + player.playerId + ":" + SafeName(player));
        }

        // Joiner-side request path: ask instance master to refresh synchronized state.
        public void _RequestLateJoinResyncFromMaster()
        {
            if (!Utilities.IsValid(Networking.LocalPlayer)) { return; }
            if (Networking.IsOwner(gameObject)) { return; }

            DLog("RequestLateJoinResyncFromMaster send");
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(NetLateJoinResyncRequest));
        }

        // Runs on everyone. Only Instance Master responds.
        public void NetLateJoinResyncRequest()
        {
            if (!Networking.IsMaster) { return; }

            DLog("NetLateJoinResyncRequest received by Instance Master");

            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                SendCustomEventDelayedFrames(nameof(_MasterLateJoinResyncAfterOwner), 2);
                return;
            }

            _OwnerLateJoinResyncDelayed();
        }

        public void _MasterLateJoinResyncAfterOwner()
        {
            if (!Networking.IsMaster) { return; }
            if (!Networking.IsOwner(gameObject)) { return; }
            _OwnerLateJoinResyncDelayed();
        }

        public void _OwnerLateJoinResyncDelayed()
        {
            if (!Networking.IsOwner(gameObject)) { return; }

            syncEpoch++;
            lastWriteSlot = -1;
            lastWriteByPlayerId = Utilities.IsValid(Networking.LocalPlayer) ? Networking.LocalPlayer.playerId : -1;
            lastWriteActive = -3;
            lastWriteSeq = -1;
            lastWriteTick = writeCounter;

            RequestSerialization();
            ApplyAll(true);
            DLog("LateJoinResyncDelayed epoch=" + syncEpoch + " writer=" + lastWriteByPlayerId);
        }

        public void _OwnerReserializeSnapshot()
        {
            if (!Networking.IsOwner(gameObject)) { return; }
            ReserializeSnapshotFromVisuals();
        }

        public void _TryApplyPendingToggle()
        {
            if (!_pendingToggle) { return; }

            if (!Networking.IsOwner(gameObject))
            {
                _pendingRetryCount++;
                if (_pendingRetryCount < 180)
                {
                    SendCustomEventDelayedFrames(nameof(_TryApplyPendingToggle), 1);
                }
                else
                {
                    DLog("_TryApplyPendingToggle timeout slot=" + _pendingSlotId);
                    _pendingToggle = false;
                }
                return;
            }

            _pendingToggle = false;
            ApplyToggle(_pendingSlotId, _pendingNext);
        }

        private void ApplyToggle(int slotId, int next)
        {
            // Prevent stale owner snapshot from clearing other active slots:
            // take current visual states as baseline before writing this slot.
            CaptureActiveStateFromVisuals();

            writeCounter++;

            SetActive(slotId, next);
            IncSeq(slotId);
            SetTick(slotId, writeCounter);

            lastWriteSlot = slotId;
            lastWriteByPlayerId = Utilities.IsValid(Networking.LocalPlayer) ? Networking.LocalPlayer.playerId : -1;
            lastWriteActive = next;
            lastWriteSeq = GetSeq(slotId);
            lastWriteTick = GetTick(slotId);

            RequestSerialization();
            ApplyAll(true);
            BroadcastVisualState(slotId, next);

            DLog("ApplyToggle slot=" + slotId + " next=" + next + " seq=" + lastWriteSeq + " tick=" + lastWriteTick + " writer=" + lastWriteByPlayerId);
            DLog("ApplyToggle slotState " + FormatSlotState(slotId));
        }

        private void CaptureActiveStateFromVisuals()
        {
            int count = (Slots != null) ? Slots.Length : 0;
            for (int i = 0; i < count; i++)
            {
                SAV_VehicleSlot_SingleDebug slot = Slots[i];
                if (!slot) { continue; }
                int id = slot.SlotId;
                if (id < 0 || id > 15) { continue; }

                int visualActive = slot.IsVisualActive() ? 0 : -1;
                SetActive(id, visualActive);
            }
        }

        private void ReserializeSnapshotFromVisuals()
        {
            CaptureActiveStateFromVisuals();

            int count = (Slots != null) ? Slots.Length : 0;
            for (int i = 0; i < count; i++)
            {
                SAV_VehicleSlot_SingleDebug slot = Slots[i];
                if (!slot) { continue; }
                int id = slot.SlotId;
                if (id < 0 || id > 15) { continue; }

                IncSeq(id);
                writeCounter++;
                SetTick(id, writeCounter);
            }

            lastWriteSlot = -1;
            lastWriteByPlayerId = Utilities.IsValid(Networking.LocalPlayer) ? Networking.LocalPlayer.playerId : -1;
            lastWriteActive = -2;
            lastWriteSeq = -1;
            lastWriteTick = writeCounter;

            RequestSerialization();
            ApplyAll(true);
            DLog("ReserializeSnapshotFromVisuals writeCounter=" + writeCounter + " epoch=" + syncEpoch);
        }

        private void BroadcastVisualState(int slotId, int next)
        {
            SAV_VehicleSlot_SingleDebug slot = GetSlot(slotId);
            if (!slot) { return; }

            if (next == 0)
            {
                slot.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetSetActiveVisual");
            }
            else
            {
                slot.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetSetInactiveVisual");
            }

            DLog("BroadcastVisualState slot=" + slotId + " next=" + next);
        }

        public void AllRespawn()
        {
            int count = (Slots != null) ? Slots.Length : 0;
            for (int i = 0; i < count; i++)
            {
                SAV_VehicleSlot_SingleDebug slot = Slots[i];
                if (!slot) { continue; }

                if (slot.SlotId < 0 || slot.SlotId > 15) { continue; }
                if (slot.IsVisualActive())
                {
                    slot.TriggerRespawnNow_All();
                }
            }

            DLog("AllRespawn called");
        }

        private void ApplyAll(bool force)
        {
            int count = (Slots != null) ? Slots.Length : 0;
            bool localIsOwner = Networking.IsOwner(gameObject);
            for (int i = 0; i < count; i++)
            {
                SAV_VehicleSlot_SingleDebug slot = Slots[i];
                if (!slot) { continue; }
                int id = slot.SlotId;
                if (id < 0 || id > 15) { continue; }

                slot.ApplyState(
                    GetActive(id),
                    GetSeq(id),
                    GetTick(id),
                    force,
                    localIsOwner,
                    EnableDebugLogs
                );
            }
        }

        private SAV_VehicleSlot_SingleDebug GetSlot(int slotId)
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

        private int GetActive(int slotId)
        {
            switch (slotId)
            {
                case 0: return s0_active;
                case 1: return s1_active;
                case 2: return s2_active;
                case 3: return s3_active;
                case 4: return s4_active;
                case 5: return s5_active;
                case 6: return s6_active;
                case 7: return s7_active;
                case 8: return s8_active;
                case 9: return s9_active;
                case 10: return s10_active;
                case 11: return s11_active;
                case 12: return s12_active;
                case 13: return s13_active;
                case 14: return s14_active;
                case 15: return s15_active;
            }
            return -1;
        }

        private int GetSeq(int slotId)
        {
            switch (slotId)
            {
                case 0: return s0_seq;
                case 1: return s1_seq;
                case 2: return s2_seq;
                case 3: return s3_seq;
                case 4: return s4_seq;
                case 5: return s5_seq;
                case 6: return s6_seq;
                case 7: return s7_seq;
                case 8: return s8_seq;
                case 9: return s9_seq;
                case 10: return s10_seq;
                case 11: return s11_seq;
                case 12: return s12_seq;
                case 13: return s13_seq;
                case 14: return s14_seq;
                case 15: return s15_seq;
            }
            return 0;
        }

        private int GetTick(int slotId)
        {
            switch (slotId)
            {
                case 0: return s0_tick;
                case 1: return s1_tick;
                case 2: return s2_tick;
                case 3: return s3_tick;
                case 4: return s4_tick;
                case 5: return s5_tick;
                case 6: return s6_tick;
                case 7: return s7_tick;
                case 8: return s8_tick;
                case 9: return s9_tick;
                case 10: return s10_tick;
                case 11: return s11_tick;
                case 12: return s12_tick;
                case 13: return s13_tick;
                case 14: return s14_tick;
                case 15: return s15_tick;
            }
            return 0;
        }

        private void SetActive(int slotId, int value)
        {
            switch (slotId)
            {
                case 0: s0_active = value; break;
                case 1: s1_active = value; break;
                case 2: s2_active = value; break;
                case 3: s3_active = value; break;
                case 4: s4_active = value; break;
                case 5: s5_active = value; break;
                case 6: s6_active = value; break;
                case 7: s7_active = value; break;
                case 8: s8_active = value; break;
                case 9: s9_active = value; break;
                case 10: s10_active = value; break;
                case 11: s11_active = value; break;
                case 12: s12_active = value; break;
                case 13: s13_active = value; break;
                case 14: s14_active = value; break;
                case 15: s15_active = value; break;
            }
        }

        private void IncSeq(int slotId)
        {
            switch (slotId)
            {
                case 0: s0_seq++; break;
                case 1: s1_seq++; break;
                case 2: s2_seq++; break;
                case 3: s3_seq++; break;
                case 4: s4_seq++; break;
                case 5: s5_seq++; break;
                case 6: s6_seq++; break;
                case 7: s7_seq++; break;
                case 8: s8_seq++; break;
                case 9: s9_seq++; break;
                case 10: s10_seq++; break;
                case 11: s11_seq++; break;
                case 12: s12_seq++; break;
                case 13: s13_seq++; break;
                case 14: s14_seq++; break;
                case 15: s15_seq++; break;
            }
        }

        private void SetTick(int slotId, int value)
        {
            switch (slotId)
            {
                case 0: s0_tick = value; break;
                case 1: s1_tick = value; break;
                case 2: s2_tick = value; break;
                case 3: s3_tick = value; break;
                case 4: s4_tick = value; break;
                case 5: s5_tick = value; break;
                case 6: s6_tick = value; break;
                case 7: s7_tick = value; break;
                case 8: s8_tick = value; break;
                case 9: s9_tick = value; break;
                case 10: s10_tick = value; break;
                case 11: s11_tick = value; break;
                case 12: s12_tick = value; break;
                case 13: s13_tick = value; break;
                case 14: s14_tick = value; break;
                case 15: s15_tick = value; break;
            }
        }

        private void DLog(string msg)
        {
            if (!EnableDebugLogs) { return; }
            VRCPlayerApi owner = Networking.GetOwner(gameObject);
            int localId = Utilities.IsValid(Networking.LocalPlayer) ? Networking.LocalPlayer.playerId : -1;
            string ownerName = SafeName(owner);
            Debug.Log("[SlotMgr] L=" + localId + " Owner=" + ownerName + " | " + msg);
        }

        private string FormatSlotState(int slotId)
        {
            return "slot=" + slotId + " active=" + GetActive(slotId) + " seq=" + GetSeq(slotId) + " tick=" + GetTick(slotId);
        }

        private string SafeName(VRCPlayerApi p)
        {
            return (p != null) ? p.displayName : "null";
        }
    }
}
