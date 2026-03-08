// SLMH_LateJoinSyncBridge.cs
// コードの最終目的: LateJoin時の状態要求とスナップショット反映を安定化する
// バージョン名: ver05
// バージョン差分: Inspector表示を最小化（Manager以外は非表示）
// バージョン更新日: 2026-03-08 13:15

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SLMH_LateJoinSyncBridge : UdonSharpBehaviour
    {
        [Header("Refs")]
        public SLMH_SlotManager_Base Manager;

        [HideInInspector]
        public bool EnableDebugLogs = true;

        [HideInInspector, UdonSynced] public int SnapshotEpoch = 0;
        [HideInInspector, UdonSynced] public int SnapshotWriterPlayerId = -1;
        [HideInInspector, UdonSynced] public int s0_active = -1;
        [HideInInspector, UdonSynced] public int s1_active = -1;
        [HideInInspector, UdonSynced] public int s2_active = -1;
        [HideInInspector, UdonSynced] public int s3_active = -1;
        [HideInInspector, UdonSynced] public int s4_active = -1;
        [HideInInspector, UdonSynced] public int s5_active = -1;
        [HideInInspector, UdonSynced] public int s6_active = -1;
        [HideInInspector, UdonSynced] public int s7_active = -1;
        [HideInInspector, UdonSynced] public int s8_active = -1;
        [HideInInspector, UdonSynced] public int s9_active = -1;
        [HideInInspector, UdonSynced] public int s10_active = -1;
        [HideInInspector, UdonSynced] public int s11_active = -1;
        [HideInInspector, UdonSynced] public int s12_active = -1;
        [HideInInspector, UdonSynced] public int s13_active = -1;
        [HideInInspector, UdonSynced] public int s14_active = -1;
        [HideInInspector, UdonSynced] public int s15_active = -1;
        private int _lastAppliedEpoch = int.MinValue;
        private bool _awaitingSnapshot = false;
        private int _retryCount = 0;

        private void Start()
        {
            if (!Utilities.IsValid(Networking.LocalPlayer)) { return; }

            // Keep bridge owner at instance master for request handling stability.
            if (Networking.IsMaster && !Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            // Joiner asks master once world settles.
            if (!Networking.IsMaster)
            {
                SendCustomEventDelayedSeconds(nameof(_RequestSnapshotFromMaster), 1.2f);
            }
        }

        public void _RequestSnapshotFromMaster()
        {
            if (!Utilities.IsValid(Networking.LocalPlayer)) { return; }
            if (Networking.IsMaster) { return; }

            _awaitingSnapshot = true;
            _retryCount = 0;
            DLog("RequestSnapshotFromMaster send");
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(NetRequestSnapshot));
            SendCustomEventDelayedSeconds(nameof(_RetryRequestSnapshot), 1.6f);
        }

        public void _RetryRequestSnapshot()
        {
            if (!_awaitingSnapshot) { return; }
            if (!Utilities.IsValid(Networking.LocalPlayer)) { return; }
            if (Networking.IsMaster) { return; }

            _retryCount++;
            if (_retryCount > 3)
            {
                _awaitingSnapshot = false;
                DLog("RetryRequestSnapshot exhausted");
                return;
            }

            DLog("RetryRequestSnapshot send count=" + _retryCount);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(NetRequestSnapshot));
            SendCustomEventDelayedSeconds(nameof(_RetryRequestSnapshot), 1.6f);
        }

        // Runs on all. Only instance master responds.
        public void NetRequestSnapshot()
        {
            if (!Networking.IsMaster) { return; }
            DLog("NetRequestSnapshot received by Instance Master");

            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                SendCustomEventDelayedFrames(nameof(_MasterPushAfterOwner), 2);
                return;
            }

            _PushSnapshot();
        }

        public void _MasterPushAfterOwner()
        {
            if (!Networking.IsMaster) { return; }
            if (!Networking.IsOwner(gameObject)) { return; }
            _PushSnapshot();
        }

        private void _PushSnapshot()
        {
            if (Manager == null) { DLog("PushSnapshot skipped Manager=null"); return; }

            s0_active = Manager.Base_GetActiveForBridge(0);
            s1_active = Manager.Base_GetActiveForBridge(1);
            s2_active = Manager.Base_GetActiveForBridge(2);
            s3_active = Manager.Base_GetActiveForBridge(3);
            s4_active = Manager.Base_GetActiveForBridge(4);
            s5_active = Manager.Base_GetActiveForBridge(5);
            s6_active = Manager.Base_GetActiveForBridge(6);
            s7_active = Manager.Base_GetActiveForBridge(7);
            s8_active = Manager.Base_GetActiveForBridge(8);
            s9_active = Manager.Base_GetActiveForBridge(9);
            s10_active = Manager.Base_GetActiveForBridge(10);
            s11_active = Manager.Base_GetActiveForBridge(11);
            s12_active = Manager.Base_GetActiveForBridge(12);
            s13_active = Manager.Base_GetActiveForBridge(13);
            s14_active = Manager.Base_GetActiveForBridge(14);
            s15_active = Manager.Base_GetActiveForBridge(15);

            SnapshotEpoch++;
            SnapshotWriterPlayerId = Utilities.IsValid(Networking.LocalPlayer) ? Networking.LocalPlayer.playerId : -1;
            RequestSerialization();
            DLog("PushSnapshot epoch=" + SnapshotEpoch + " writer=" + SnapshotWriterPlayerId);

            SendCustomEventDelayedSeconds(nameof(_PushSnapshotSecondPass), 1.0f);
        }

        public void _PushSnapshotSecondPass()
        {
            if (!Networking.IsMaster) { return; }
            if (!Networking.IsOwner(gameObject)) { return; }
            _PushSnapshotSinglePass();
        }

        private void _PushSnapshotSinglePass()
        {
            if (Manager == null) { return; }

            s0_active = Manager.Base_GetActiveForBridge(0);
            s1_active = Manager.Base_GetActiveForBridge(1);
            s2_active = Manager.Base_GetActiveForBridge(2);
            s3_active = Manager.Base_GetActiveForBridge(3);
            s4_active = Manager.Base_GetActiveForBridge(4);
            s5_active = Manager.Base_GetActiveForBridge(5);
            s6_active = Manager.Base_GetActiveForBridge(6);
            s7_active = Manager.Base_GetActiveForBridge(7);
            s8_active = Manager.Base_GetActiveForBridge(8);
            s9_active = Manager.Base_GetActiveForBridge(9);
            s10_active = Manager.Base_GetActiveForBridge(10);
            s11_active = Manager.Base_GetActiveForBridge(11);
            s12_active = Manager.Base_GetActiveForBridge(12);
            s13_active = Manager.Base_GetActiveForBridge(13);
            s14_active = Manager.Base_GetActiveForBridge(14);
            s15_active = Manager.Base_GetActiveForBridge(15);

            SnapshotEpoch++;
            SnapshotWriterPlayerId = Utilities.IsValid(Networking.LocalPlayer) ? Networking.LocalPlayer.playerId : -1;
            RequestSerialization();
            DLog("PushSnapshotSecondPass epoch=" + SnapshotEpoch + " writer=" + SnapshotWriterPlayerId);
        }

        public override void OnDeserialization()
        {
            if (SnapshotEpoch <= _lastAppliedEpoch) { return; }
            _lastAppliedEpoch = SnapshotEpoch;
            _awaitingSnapshot = false;

            if (Manager == null) { DLog("OnDeserialization skipped Manager=null"); return; }

            Manager.Base_SetActiveForBridge(0, s0_active);
            Manager.Base_SetActiveForBridge(1, s1_active);
            Manager.Base_SetActiveForBridge(2, s2_active);
            Manager.Base_SetActiveForBridge(3, s3_active);
            Manager.Base_SetActiveForBridge(4, s4_active);
            Manager.Base_SetActiveForBridge(5, s5_active);
            Manager.Base_SetActiveForBridge(6, s6_active);
            Manager.Base_SetActiveForBridge(7, s7_active);
            Manager.Base_SetActiveForBridge(8, s8_active);
            Manager.Base_SetActiveForBridge(9, s9_active);
            Manager.Base_SetActiveForBridge(10, s10_active);
            Manager.Base_SetActiveForBridge(11, s11_active);
            Manager.Base_SetActiveForBridge(12, s12_active);
            Manager.Base_SetActiveForBridge(13, s13_active);
            Manager.Base_SetActiveForBridge(14, s14_active);
            Manager.Base_SetActiveForBridge(15, s15_active);

            Manager.Base_ApplyAllFromLateJoinBridge(SnapshotEpoch, SnapshotWriterPlayerId);
            DLog("OnDeserialization applied epoch=" + SnapshotEpoch + " writer=" + SnapshotWriterPlayerId);
        }

        private void DLog(string msg)
        {
            if (!EnableDebugLogs) { return; }
            int localId = Utilities.IsValid(Networking.LocalPlayer) ? Networking.LocalPlayer.playerId : -1;
            bool isOwner = Networking.IsOwner(gameObject);
            Debug.Log("[LateJoinBridge] L=" + localId + " owner=" + isOwner + " | " + msg);
        }
    }
}




