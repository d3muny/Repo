// SLMH_SlotManager_Base.cs
// Final goal: Provide shared SlotManager foundation (refs, logs, common helpers).
// Version: ver04
// Change: Route Slot/LateJoin refs through ControllerRoot (Single does not directly reference bridge).
// Updated: 2026-03-08 10:53
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

        [Header("Controller Root")]
        public SLMH_ControllerRoot ControllerRoot;

        protected int GetSlotCount()
        {
            if (ControllerRoot == null) { return 0; }
            return ControllerRoot.GetSlotCount();
        }

        protected SLMH_VehicleSlot_Single GetSlotAt(int index)
        {
            if (ControllerRoot == null) { return null; }
            return ControllerRoot.GetSlotAt(index);
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
            if (ControllerRoot == null) { return; }
            ControllerRoot.BindLateJoinBridge(manager);
        }

        protected bool HasLateJoinBridge()
        {
            if (ControllerRoot == null) { return false; }
            return ControllerRoot.HasLateJoinBridge();
        }

        protected void StartLateJoinControl(SLMH_SlotManager_Single manager)
        {
            BindLateJoinBridge(manager);

            if (!HasLateJoinBridge() && !_lateJoinResyncRequested)
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
            if (HasLateJoinBridge()) { return; }

            if (Networking.IsOwner(gameObject))
            {
                SendCustomEventDelayedSeconds("_Base_OwnerLateJoinResyncDelayed", 1f);
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
            if (HasLateJoinBridge()) { return; }

            DLog("RequestLateJoinResyncFromMaster send");
            _awaitingLateJoinResync = true;
            _lateJoinRetryCount = 0;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(NetLateJoinResyncRequest));
            SendCustomEventDelayedSeconds("_Base_RetryLateJoinResyncRequest", 1.6f);
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
            SendCustomEventDelayedSeconds("_Base_RetryLateJoinResyncRequest", 1.6f);
        }

        public void NetLateJoinResyncRequest()
        {
            if (!Networking.IsMaster) { return; }

            DLog("NetLateJoinResyncRequest received by Instance Master");

            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                SendCustomEventDelayedFrames("_Base_MasterLateJoinResyncAfterOwner", 2);
                return;
            }

            SendCustomEvent("_Base_OwnerLateJoinResyncDelayed");
        }

        public void _Base_MasterLateJoinResyncAfterOwner()
        {
            if (!Networking.IsMaster) { return; }
            if (!Networking.IsOwner(gameObject)) { return; }
            SendCustomEvent("_Base_OwnerLateJoinResyncDelayed");
        }

        public void ApplyAllFromLateJoinBridge(int bridgeEpoch, int bridgeWriterId)
        {
            _awaitingLateJoinResync = false;
            SendCustomEvent("_Base_ApplyAllAfterLateJoinBridge");
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

