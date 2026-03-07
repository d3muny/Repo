// SLMH_SlotToggleButton.cs
// コードの最終目的: 指定SlotのActiveトグル操作をManagerへ中継する
// バージョン名: ver03
// バージョン差分: クラス名からDebugを除去し用途名へ変更
// バージョン更新日: 2026-03-07 23:46
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




