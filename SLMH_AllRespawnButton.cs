// SLMH_AllRespawnButton.cs
// コードの最終目的: Active中の各Slot機体に対してAll Respawnを一括実行する
// バージョン名: ver02-01
// バージョン差分: ver02-01基準化（安定スナップショット）
// バージョン更新日: 2026-03-08 13:26
using UdonSharp;
using UnityEngine;

namespace SaccFlightAndVehicles
{
    public class SLMH_AllRespawnButton : UdonSharpBehaviour
    {
        public SLMH_SlotManager_Base Manager;

        public override void Interact()
        {
            if (Manager != null)
            {
                Manager.Base_AllRespawn();
            }
        }
    }
}





