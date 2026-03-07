// SAV_SlotManager_SingleDebug.cs
// コードの最終目的: Slot状態の同期管理を一元化し、Full/LowPoly切替とAll Respawnを制御する
// バージョン名: ver13
// バージョン差分: 同期不達対策として UdonSynced 変数を public 化（2人同期/LateJoin再検証用）
// バージョン更新日: 2026-03-07 16:37

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

        [Header("Slots")]
        public SAV_VehicleSlot_SingleDebug[] Slots;
        [Header("Debug")]
        public bool EnableDebugLog = true;

        // ---- Synced state (per slot, fixed max = 16) ----
        // -1 = inactive (LowPoly), 0 = active (Full)
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

        // seq increments whenever state changes (forces re-apply / respawn scheduling)
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
        [UdonSynced] public int syncTick = 0;
        [UdonSynced] public int lastWriterPlayerId = -1;

        private void Start()
        {
            ApplyAll(true);
        }

        public override void OnDeserialization()
        {
            ApplyAll(false);
            LogSyncState("OnDeserialization");
        }

        // UI calls this
        public void ToggleActive(int slotId)
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
                    return;
                }
                next = -1;
            }
            else
            {
                next = 0;
            }

            // Ensure we are owner before changing synced vars
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

                // Defer the actual state change by 1 frame
                _pendingSlotId = slotId;
                _pendingNext = next;
                _pendingToggle = true;

                SendCustomEventDelayedFrames("_DoPendingToggle", 1);
                return;
            }

            // Already owner: apply immediately
            ApplyToggle(slotId, next);
        }

        public void _DoPendingToggle()
        {
            if (!_pendingToggle) { return; }
            _pendingToggle = false;

            // If ownership still hasn't arrived, try again next frame
            if (!Networking.IsOwner(gameObject))
            {
                _pendingToggle = true;
                SendCustomEventDelayedFrames("_DoPendingToggle", 1);
                return;
            }

            ApplyToggle(_pendingSlotId, _pendingNext);
        }

        private void ApplyToggle(int slotId, int next)
        {
            SetActive(slotId, next);
            IncSeq(slotId);
            syncTick++;
            if (Utilities.IsValid(Networking.LocalPlayer))
            {
                lastWriterPlayerId = Networking.LocalPlayer.playerId;
            }

            RequestSerialization();
            ApplyAll(true);

            // Safe owner name logging (no ?. operator)
            VRCPlayerApi owner = Networking.GetOwner(gameObject);
            string ownerName = (owner != null) ? owner.displayName : "null";
            Debug.Log("[SlotMgr] ApplyToggle slot=" + slotId + " next=" + next + " tick=" + syncTick + " writer=" + lastWriterPlayerId + " owner=" + ownerName);
            LogSyncState("ApplyToggleLocal");
        }

        public void AllRespawn()
        {
            // Anyone can press; no sync needed.
            int count = (Slots != null) ? Slots.Length : 0;
            for (int i = 0; i < count; i++)
            {
                SAV_VehicleSlot_SingleDebug slot = Slots[i];
                if (!slot) { continue; }

                // Only active slots
                if (slot.SlotId < 0 || slot.SlotId > 15) { continue; }
                if (GetActive(slot.SlotId) == 0)
                {
                    slot.TriggerRespawnNow_All();
                }
            }
        }

        private void ApplyAll(bool force)
        {
            int count = (Slots != null) ? Slots.Length : 0;
            for (int i = 0; i < count; i++)
            {
                SAV_VehicleSlot_SingleDebug slot = Slots[i];
                if (!slot) { continue; }
                int id = slot.SlotId;
                if (id < 0 || id > 15) { continue; }

                slot.ApplyState(GetActive(id), GetSeq(id), syncTick, force, false, false);
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

        private void LogSyncState(string tag)
        {
            if (!EnableDebugLog) { return; }

            int localId = -1;
            if (Utilities.IsValid(Networking.LocalPlayer))
            {
                localId = Networking.LocalPlayer.playerId;
            }

            VRCPlayerApi owner = Networking.GetOwner(gameObject);
            string ownerText = (owner != null) ? (owner.displayName + "(" + owner.playerId + ")") : "null";
            Debug.Log("[SlotMgr] " + tag + " local=" + localId + " owner=" + ownerText + " writer=" + lastWriterPlayerId + " tick=" + syncTick);

            int count = (Slots != null) ? Slots.Length : 0;
            for (int i = 0; i < count; i++)
            {
                SAV_VehicleSlot_SingleDebug slot = Slots[i];
                if (slot == null) { continue; }
                int id = slot.SlotId;
                if (id < 0 || id > 15) { continue; }
                Debug.Log("[SlotMgr] " + tag + " slot=" + id + " active=" + GetActive(id) + " seq=" + GetSeq(id));
            }
        }
    }
}

