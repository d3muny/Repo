// SLMH_AllRespawnButton.cs
// コードの最終目的: Active中の各Slot機体に対してAll Respawnを一括実行する
// バージョン名: ver02
// バージョン差分: 接頭語をSAV_からSLMH_へ統一
// バージョン更新日: 2026-03-07 20:09
using UdonSharp;
using UnityEngine;

namespace SaccFlightAndVehicles
{
    public class SLMH_AllRespawnButton : UdonSharpBehaviour
    {
        public SLMH_SlotManager_SingleDebug Manager;

        public override void Interact()
        {
            if (Manager != null)
            {
                Manager.AllRespawn();
            }
        }
    }
}


