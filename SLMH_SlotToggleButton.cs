// SLMH_SlotToggleButton.cs
// コードの最終目的: 指定SlotのActiveトグル操作をManagerへ中継する
// バージョン名: ver02-01
// バージョン差分: ver02-01基準化（安定スナップショット）
// バージョン更新日: 2026-03-08 13:26
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace SaccFlightAndVehicles
{
    public class SLMH_SlotToggleButton : UdonSharpBehaviour
    {
        public SLMH_SlotManager_Single Manager;
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





