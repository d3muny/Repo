// SLMH_DebugButton.cs
// コードの最終目的: 指定SlotのActiveトグル操作をManagerへ中継する
// バージョン名: ver02
// バージョン差分: 接頭語をSAV_からSLMH_へ統一
// バージョン更新日: 2026-03-07 20:09
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace SaccFlightAndVehicles
{
    public class SLMH_DebugButton : UdonSharpBehaviour
    {
        public SLMH_SlotManager_SingleDebug Manager;
        public int SlotId = 0;

        public override void Interact()
        {
            if (Manager != null)
            {
                Manager.ToggleActive(SlotId);
            }
        }
    }
}


