// SAV_AllRespawnButton.cs
// コードの最終目的: Active中の各Slot機体に対してAll Respawnを一括実行する
// バージョン名: ver01
// バージョン差分: 初版ヘッダ整備
// バージョン更新日: 2026-03-05
using UdonSharp;
using UnityEngine;

namespace SaccFlightAndVehicles
{
    public class SAV_AllRespawnButton : UdonSharpBehaviour
    {
        public SAV_SlotManager_SingleDebug Manager;

        public override void Interact()
        {
            if (Manager != null)
            {
                Manager.AllRespawn();
            }
        }
    }
}
