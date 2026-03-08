// SLMH_SlotManager_Base.cs
// Final goal: Provide shared SlotManager foundation (refs, logs, common helpers).
// Version: ver03
// Change: Move LateJoin control flow to base (Single keeps state-specific logic only).
// Updated: 2026-03-08 10:38
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SLMH_SlotManager_Base : UdonSharpBehaviour
    {
        protected bool _lateJoinResyncRequested = false;
        protected bool _awaitingLateJoinResync = false;
        protected int _lateJoinRetryCount = 0;

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

        protected void StartLateJoinControl(SLMH_SlotManager_Single manager)
        {
            BindLateJoinBridge(manager);

            if (LateJoinBridge == null && !_lateJoinResyncRequested)
            {
                _lateJoinResyncRequested = true;
                SendCustomEventDelayedSeconds(nameof(_Base_RequestLateJoinResyncFromMaster), 1.2f);
            }
        }

        protected void ResetAwaitingLateJoinOnDeserialization()
        {
            _awaitingLateJoinResync = false;
        }

        protected void HandlePlayerJoinedLateJoin(VRCPlayerApi player)
        {
            DLog("OnPlayerJoined player=" + player.playerId + ":" + SafeName(player) + " localIsOwner=" + Networking.IsOwner(gameObject));
            if (LateJoinBridge != null) { return; }

            if (Networking.IsOwner(gameObject))
            {
                SendCustomEventDelayedSeconds(nameof(_Base_OwnerLateJoinResyncDelayed), 1f);
            }
        }

        protected void HandlePlayerLeftLateJoin(VRCPlayerApi player)
        {
            DLog("OnPlayerLeft player=" + player.playerId + ":" + SafeName(player));
        }

        public void _Base_RequestLateJoinResyncFromMaster()
        {
            if (!Utilities.IsValid(Networking.LocalPlayer)) { return; }
            if (Networking.IsOwner(gameObject)) { return; }
            if (LateJoinBridge != null) { return; }

            DLog("RequestLateJoinResyncFromMaster send");
            _awaitingLateJoinResync = true;
            _lateJoinRetryCount = 0;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(NetLateJoinResyncRequest));
            SendCustomEventDelayedSeconds(nameof(_Base_RetryLateJoinResyncRequest), 1.6f);
        }

        public void _Base_RetryLateJoinResyncRequest()
        {
            if (!_awaitingLateJoinResync) { return; }
            if (!Utilities.IsValid(Networking.LocalPlayer)) { return; }
            if (Networking.IsOwner(gameObject)) { return; }

            _lateJoinRetryCount++;
            if (_lateJoinRetryCount > 3)
            {
                DLog("LateJoinResync retry exhausted");
                _awaitingLateJoinResync = false;
                return;
            }

            DLog("LateJoinResync retry send count=" + _lateJoinRetryCount);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(NetLateJoinResyncRequest));
            SendCustomEventDelayedSeconds(nameof(_Base_RetryLateJoinResyncRequest), 1.6f);
        }

        public void NetLateJoinResyncRequest()
        {
            if (!Networking.IsMaster) { return; }

            DLog("NetLateJoinResyncRequest received by Instance Master");

            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                SendCustomEventDelayedFrames(nameof(_Base_MasterLateJoinResyncAfterOwner), 2);
                return;
            }

            SendCustomEvent(nameof(_Base_OwnerLateJoinResyncDelayed));
        }

        public void _Base_MasterLateJoinResyncAfterOwner()
        {
            if (!Networking.IsMaster) { return; }
            if (!Networking.IsOwner(gameObject)) { return; }
            SendCustomEvent(nameof(_Base_OwnerLateJoinResyncDelayed));
        }

        public void ApplyAllFromLateJoinBridge(int bridgeEpoch, int bridgeWriterId)
        {
            _awaitingLateJoinResync = false;
            SendCustomEvent(nameof(_Base_ApplyAllAfterLateJoinBridge));
            DLog("LateJoinBridgeApply epoch=" + bridgeEpoch + " writer=" + bridgeWriterId);
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

