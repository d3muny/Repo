// SLMH_AllRespawnButton.cs
// コードの最終目的: Active中の各Slot機体に対してAll Respawnを一括実行する
// バージョン名: ver04
// バージョン差分: Manager参照をSingleからBaseへ変更
// バージョン更新日: 2026-03-08 12:18
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




