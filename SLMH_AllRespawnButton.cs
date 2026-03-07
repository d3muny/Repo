// SLMH_AllRespawnButton.cs
// コードの最終目的: Active中の各Slot機体に対してAll Respawnを一括実行する
// バージョン名: ver03
// バージョン差分: Manager参照型をSingle命名へ追従
// バージョン更新日: 2026-03-07 23:46
using UdonSharp;
using UnityEngine;

namespace SaccFlightAndVehicles
{
    public class SLMH_AllRespawnButton : UdonSharpBehaviour
    {
        public SLMH_SlotManager_Single Manager;

        public override void Interact()
        {
            if (Manager != null)
            {
                Manager.AllRespawn();
            }
        }
    }
}




