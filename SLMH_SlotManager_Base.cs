// SLMH_SlotManager_Base.cs
// Final goal: Provide shared SlotManager foundation (refs, logs, late-join control).
// Version: ver08
// Change: Add bridge/all-respawn forwarders and keep Base as only external manager reference.
// Updated: 2026-03-08 12:18
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SLMH_SlotManager_Base : UdonSharpBehaviour
    {
        private bool _lateJoinResyncRequested = false;
        private bool _awaitingLateJoinResync = false;
        private int _lateJoinRetryCount = 0;

        [Header("Debug")]
        public bool EnableDebugLogs = true;

        [Header("Slots")]
        public SLMH_VehicleSlot_Base[] Slots;

        [Header("LateJoin Bridge (child Udon)")]
        public SLMH_LateJoinSyncBridge LateJoinBridge;

        [Header("Runtime child (optional)")]
        public SLMH_SlotManager_Single SingleRuntime;
        public SLMH_SlotManager_Multi MultiRuntime;

        public int Base_GetSlotCount()
        {
            return (Slots != null) ? Slots.Length : 0;
        }

        public SLMH_VehicleSlot_Base Base_GetSlotAt(int index)
        {
            if (Slots == null) { return null; }
            if (index < 0 || index >= Slots.Length) { return null; }
            return Slots[index];
        }

        public SLMH_VehicleSlot_Base Base_GetSlotById(int slotId)
        {
            int count = Base_GetSlotCount();
            for (int i = 0; i < count; i++)
            {
                SLMH_VehicleSlot_Base slot = Base_GetSlotAt(i);
                if (slot != null && slot.SlotId == slotId)
                {
                    return slot;
                }
            }
            return null;
        }

        public void Base_BindLateJoinBridge()
        {
            if (LateJoinBridge == null) { return; }
            LateJoinBridge.Manager = this;
        }

        public bool Base_HasLateJoinBridge()
        {
            return LateJoinBridge != null;
        }

        public void Base_RuntimeApplyAllFromSyncedState()
        {
            if (SingleRuntime != null)
            {
                SingleRuntime.Runtime_ApplyAllFromSyncedState();
                return;
            }
            if (MultiRuntime != null)
            {
                MultiRuntime.Runtime_ApplyAllFromSyncedState();
            }
        }

        public void Base_RuntimeToggleActive(int slotId)
        {
            if (SingleRuntime != null)
            {
                SingleRuntime.Runtime_OnLocalInput_ToggleActive(slotId);
                return;
            }
            if (MultiRuntime != null)
            {
                MultiRuntime.Runtime_OnLocalInput_ToggleActive(slotId);
            }
        }

        public void Base_RuntimeCyclePreview(int slotId, int dir)
        {
            if (SingleRuntime != null)
            {
                SingleRuntime.Runtime_OnLocalInput_CyclePreview(slotId, dir);
                return;
            }
            if (MultiRuntime != null)
            {
                MultiRuntime.Runtime_OnLocalInput_CyclePreview(slotId, dir);
            }
        }

        public int Base_GetActiveForBridge(int slotId)
        {
            if (SingleRuntime != null) { return SingleRuntime.GetActiveForBridge(slotId); }
            return -1;
        }

        public void Base_SetActiveForBridge(int slotId, int value)
        {
            if (SingleRuntime != null) { SingleRuntime.SetActiveForBridge(slotId, value); }
        }

        public void Base_ApplyAllFromLateJoinBridge(int bridgeEpoch, int bridgeWriterId)
        {
            if (SingleRuntime != null)
            {
                SingleRuntime.ApplyAllFromLateJoinBridge(bridgeEpoch, bridgeWriterId);
                return;
            }
            ApplyAllFromLateJoinBridge(bridgeEpoch, bridgeWriterId);
        }

        public void Base_AllRespawn()
        {
            if (SingleRuntime != null)
            {
                SingleRuntime.AllRespawn();
                return;
            }
            if (MultiRuntime != null)
            {
                MultiRuntime.Runtime_AllRespawn();
            }
        }

        public void Base_StartLateJoinControl()
        {
            Base_BindLateJoinBridge();

            if (!Base_HasLateJoinBridge() && !_lateJoinResyncRequested)
            {
                _lateJoinResyncRequested = true;
                SendCustomEventDelayedSeconds(nameof(_Base_RequestLateJoinResyncFromMaster), 1.2f);
            }
        }

        public void Base_ResetAwaitingLateJoinOnDeserialization()
        {
            _awaitingLateJoinResync = false;
        }

        public void Base_HandlePlayerJoinedLateJoin(VRCPlayerApi player)
        {
            Base_DLog("OnPlayerJoined player=" + player.playerId + ":" + Base_SafeName(player) + " localIsOwner=" + Networking.IsOwner(gameObject));
            if (Base_HasLateJoinBridge()) { return; }

            if (Networking.IsOwner(gameObject))
            {
                SendCustomEventDelayedSeconds("_Base_OwnerLateJoinResyncDelayed", 1f);
            }
        }

        public void Base_HandlePlayerLeftLateJoin(VRCPlayerApi player)
        {
            Base_DLog("OnPlayerLeft player=" + player.playerId + ":" + Base_SafeName(player));
        }

        public void _Base_RequestLateJoinResyncFromMaster()
        {
            if (!Utilities.IsValid(Networking.LocalPlayer)) { return; }
            if (Networking.IsOwner(gameObject)) { return; }
            if (Base_HasLateJoinBridge()) { return; }

            Base_DLog("RequestLateJoinResyncFromMaster send");
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
                Base_DLog("LateJoinResync retry exhausted");
                _awaitingLateJoinResync = false;
                return;
            }

            Base_DLog("LateJoinResync retry send count=" + _lateJoinRetryCount);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(NetLateJoinResyncRequest));
            SendCustomEventDelayedSeconds("_Base_RetryLateJoinResyncRequest", 1.6f);
        }

        public void NetLateJoinResyncRequest()
        {
            if (!Networking.IsMaster) { return; }

            Base_DLog("NetLateJoinResyncRequest received by Instance Master");

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

        public void _Base_OwnerLateJoinResyncDelayed()
        {
            if (SingleRuntime != null)
            {
                SingleRuntime.Runtime_OnOwnerLateJoinResyncDelayed();
                return;
            }
            if (MultiRuntime != null)
            {
                MultiRuntime.Runtime_OnOwnerLateJoinResyncDelayed();
            }
        }

        public void _Base_OwnerLateJoinResyncSecondPass()
        {
            if (SingleRuntime != null)
            {
                SingleRuntime.Runtime_OnOwnerLateJoinResyncSecondPass();
                return;
            }
            if (MultiRuntime != null)
            {
                MultiRuntime.Runtime_OnOwnerLateJoinResyncSecondPass();
            }
        }

        public void _Base_OwnerReserializeSnapshot()
        {
            if (SingleRuntime != null)
            {
                SingleRuntime.Runtime_OnOwnerReserializeSnapshot();
                return;
            }
            if (MultiRuntime != null)
            {
                MultiRuntime.Runtime_OnOwnerReserializeSnapshot();
            }
        }

        public void ApplyAllFromLateJoinBridge(int bridgeEpoch, int bridgeWriterId)
        {
            _awaitingLateJoinResync = false;
            SendCustomEvent("_Base_ApplyAllAfterLateJoinBridge");
            Base_DLog("LateJoinBridgeApply epoch=" + bridgeEpoch + " writer=" + bridgeWriterId);
        }

        public void _Base_ApplyAllAfterLateJoinBridge()
        {
            Base_RuntimeApplyAllFromSyncedState();
        }

        public string Base_SafeName(VRCPlayerApi p)
        {
            return (p != null) ? p.displayName : "null";
        }

        public void Base_DLog(string msg)
        {
            if (!EnableDebugLogs) { return; }
            VRCPlayerApi owner = Networking.GetOwner(gameObject);
            int localId = Utilities.IsValid(Networking.LocalPlayer) ? Networking.LocalPlayer.playerId : -1;
            string ownerName = Base_SafeName(owner);
            Debug.Log("[SlotMgr] L=" + localId + " Owner=" + ownerName + " | " + msg);
        }
    }
}
