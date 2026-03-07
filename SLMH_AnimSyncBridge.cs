// SLMH_AnimSyncBridge.cs
// コードの最終目的: Animator Parameterを同期し、遠距離やLateJoin時の見た目ズレを補正する
// バージョン名: ver02
// バージョン差分: InspectorにParamTypeガイド表示を追加
// バージョン更新日: 2026-03-07 23:34
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SLMH_AnimSyncBridge : UdonSharpBehaviour
    {
        [Header("References")]
        public Animator TargetAnimator;

        [Header("Periodic resync")]
        public bool EnablePeriodicResync = true;
        [Range(10f, 300f)] public float PeriodicIntervalSeconds = 60f;

        [Header("Debug")]
        public bool EnableDebugLogs = false;

        [Header("ParamType Guide: 0=Bool / 1=Float / 2=Int")]

        [Header("Param 1")]
        public string ParamName1;
        [Tooltip("0=Bool, 1=Float, 2=Int")]
        [Range(0, 2)] public int ParamType1 = 0;

        [Header("Param 2")]
        public string ParamName2;
        [Tooltip("0=Bool, 1=Float, 2=Int")]
        [Range(0, 2)] public int ParamType2 = 0;

        [Header("Param 3")]
        public string ParamName3;
        [Tooltip("0=Bool, 1=Float, 2=Int")]
        [Range(0, 2)] public int ParamType3 = 0;

        [Header("Param 4")]
        public string ParamName4;
        [Tooltip("0=Bool, 1=Float, 2=Int")]
        [Range(0, 2)] public int ParamType4 = 0;

        [Header("Param 5")]
        public string ParamName5;
        [Tooltip("0=Bool, 1=Float, 2=Int")]
        [Range(0, 2)] public int ParamType5 = 0;

        [UdonSynced] private int SnapshotSeq = 0;
        [UdonSynced] private int SnapshotWriterId = -1;

        [UdonSynced] private bool b1;
        [UdonSynced] private bool b2;
        [UdonSynced] private bool b3;
        [UdonSynced] private bool b4;
        [UdonSynced] private bool b5;

        [UdonSynced] private float f1;
        [UdonSynced] private float f2;
        [UdonSynced] private float f3;
        [UdonSynced] private float f4;
        [UdonSynced] private float f5;

        [UdonSynced] private int i1;
        [UdonSynced] private int i2;
        [UdonSynced] private int i3;
        [UdonSynced] private int i4;
        [UdonSynced] private int i5;

        private int _lastAppliedSeq = -1;
        private bool _loopStarted = false;

        private void Start()
        {
            if (TargetAnimator == null)
            {
                TargetAnimator = GetComponent<Animator>();
            }

            if (EnablePeriodicResync && !_loopStarted)
            {
                _loopStarted = true;
                SendCustomEventDelayedSeconds(nameof(_PeriodicTick), PeriodicIntervalSeconds);
            }
        }

        public void NotifyStateApplied()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(NetRequestOwnerSnapshot));
        }

        public void NotifySlotBecameInactive()
        {
            // Slot inactive時は何もしない。再Active時のイベントで再同期する。
        }

        public void NetRequestOwnerSnapshot()
        {
            if (!Networking.IsOwner(gameObject)) { return; }
            CaptureAndSerialize("NetRequestOwnerSnapshot");
        }

        public void _PeriodicTick()
        {
            if (EnablePeriodicResync && Networking.IsOwner(gameObject))
            {
                CaptureAndSerialize("Periodic");
            }

            SendCustomEventDelayedSeconds(nameof(_PeriodicTick), PeriodicIntervalSeconds);
        }

        public override void OnDeserialization()
        {
            if (SnapshotSeq <= _lastAppliedSeq) { return; }
            _lastAppliedSeq = SnapshotSeq;
            ApplySyncedToAnimator();
            DLog("OnDeserialization apply seq=" + SnapshotSeq + " writer=" + SnapshotWriterId);
        }

        private void CaptureAndSerialize(string reason)
        {
            if (TargetAnimator == null) { return; }

            CaptureOne(1, ParamName1, ParamType1);
            CaptureOne(2, ParamName2, ParamType2);
            CaptureOne(3, ParamName3, ParamType3);
            CaptureOne(4, ParamName4, ParamType4);
            CaptureOne(5, ParamName5, ParamType5);

            SnapshotSeq++;
            SnapshotWriterId = Utilities.IsValid(Networking.LocalPlayer) ? Networking.LocalPlayer.playerId : -1;
            RequestSerialization();

            // owner自身も即時適用して状態を揃える
            ApplySyncedToAnimator();
            _lastAppliedSeq = SnapshotSeq;
            DLog("CaptureAndSerialize reason=" + reason + " seq=" + SnapshotSeq + " writer=" + SnapshotWriterId);
        }

        private void CaptureOne(int index, string paramName, int paramType)
        {
            if ((paramName == null || paramName == "")) { return; }
            if (!HasParameter(paramName)) { return; }

            if (paramType == 0)
            {
                bool v = TargetAnimator.GetBool(paramName);
                SetBoolSlot(index, v);
                return;
            }

            if (paramType == 1)
            {
                float v = TargetAnimator.GetFloat(paramName);
                SetFloatSlot(index, v);
                return;
            }

            int iv = TargetAnimator.GetInteger(paramName);
            SetIntSlot(index, iv);
        }

        private void ApplySyncedToAnimator()
        {
            if (TargetAnimator == null) { return; }

            ApplyOne(1, ParamName1, ParamType1);
            ApplyOne(2, ParamName2, ParamType2);
            ApplyOne(3, ParamName3, ParamType3);
            ApplyOne(4, ParamName4, ParamType4);
            ApplyOne(5, ParamName5, ParamType5);
        }

        private void ApplyOne(int index, string paramName, int paramType)
        {
            if ((paramName == null || paramName == "")) { return; }
            if (!HasParameter(paramName)) { return; }

            if (paramType == 0)
            {
                TargetAnimator.SetBool(paramName, GetBoolSlot(index));
                return;
            }

            if (paramType == 1)
            {
                TargetAnimator.SetFloat(paramName, GetFloatSlot(index));
                return;
            }

            TargetAnimator.SetInteger(paramName, GetIntSlot(index));
        }

        private bool HasParameter(string paramName)
        {
            if (TargetAnimator == null) { return false; }
            AnimatorControllerParameter[] ps = TargetAnimator.parameters;
            if (ps == null) { return false; }

            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].name == paramName) { return true; }
            }
            return false;
        }

        private void SetBoolSlot(int index, bool value)
        {
            if (index == 1) { b1 = value; return; }
            if (index == 2) { b2 = value; return; }
            if (index == 3) { b3 = value; return; }
            if (index == 4) { b4 = value; return; }
            if (index == 5) { b5 = value; return; }
        }

        private bool GetBoolSlot(int index)
        {
            if (index == 1) { return b1; }
            if (index == 2) { return b2; }
            if (index == 3) { return b3; }
            if (index == 4) { return b4; }
            if (index == 5) { return b5; }
            return false;
        }

        private void SetFloatSlot(int index, float value)
        {
            if (index == 1) { f1 = value; return; }
            if (index == 2) { f2 = value; return; }
            if (index == 3) { f3 = value; return; }
            if (index == 4) { f4 = value; return; }
            if (index == 5) { f5 = value; return; }
        }

        private float GetFloatSlot(int index)
        {
            if (index == 1) { return f1; }
            if (index == 2) { return f2; }
            if (index == 3) { return f3; }
            if (index == 4) { return f4; }
            if (index == 5) { return f5; }
            return 0f;
        }

        private void SetIntSlot(int index, int value)
        {
            if (index == 1) { i1 = value; return; }
            if (index == 2) { i2 = value; return; }
            if (index == 3) { i3 = value; return; }
            if (index == 4) { i4 = value; return; }
            if (index == 5) { i5 = value; return; }
        }

        private int GetIntSlot(int index)
        {
            if (index == 1) { return i1; }
            if (index == 2) { return i2; }
            if (index == 3) { return i3; }
            if (index == 4) { return i4; }
            if (index == 5) { return i5; }
            return 0;
        }

        private void DLog(string msg)
        {
            if (!EnableDebugLogs) { return; }
            int localId = Utilities.IsValid(Networking.LocalPlayer) ? Networking.LocalPlayer.playerId : -1;
            Debug.Log("[AnimSyncBridge] L=" + localId + " owner=" + Networking.IsOwner(gameObject) + " | " + msg);
        }
    }
}


