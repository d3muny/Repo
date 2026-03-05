// SAV_DebugButton.cs
// コードの最終目的: 指定SlotのActiveトグル操作をManagerへ中継する
// バージョン名: ver01
// バージョン差分: 初版ヘッダ整備
// バージョン更新日: 2026-03-05
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace SaccFlightAndVehicles
{
    public class SAV_DebugButton : UdonSharpBehaviour
    {
        public SAV_SlotManager_SingleDebug Manager;
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
