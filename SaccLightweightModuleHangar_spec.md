# Sacc Lightweight Module Hangar 仕様整理（合意版）

## 1. 目的
- Sacc機体を `SetActive` 切替で運用し、同期負荷・描画負荷を抑える。
- late joiner を含めて、スロット状態を全員で一致させる。
- まずは単機種デバッグを成立させ、将来の複数機種（最大16）に拡張可能な構造を維持する。

## 2. 責務分離
- **Manager（統括）**: 状態保持・同期・全体指示（All Respawn など）。
- **Slot**: `ApplyState` 実行（Full / LowPoly 切替、判定、ローカル表示更新）。

## 3. スロット/機種設計
- Slot は最大16（`Slot_00` ～ `Slot_15`）を想定。
- 機種指定は運用上 **A, B, C...** を使用（内部では index 0..15 対応）。
- `SLMH_VehicleSlot` は機種参照を最大16枠分持つ前提（可変長ではなく固定上限）。
- `VehicleCount` で有効機種範囲を制限（例: 3ならA→B→Cでループ）。

## 4. 状態ルール
- 同一Slotで同時に2機Activeは不可（切替時は必ず解除を挟む）。
- Activeボタンはトグル動作。
  - 非Active時: 選択機種をActive化。
  - Active時: 条件を満たせば非Active化。
- 非選択機体は完全非表示。
- 単機種デバッグ時は実質2状態（Full / LowPoly）で運用。

## 5. ReleaseZone（解除可否判定）
- Slot共通の `ReleaseZone`（Collider）を使用。
- 判定対象は **Full機体 root の Transform.position**（実運用での center 相当）。
- 解除条件を満たさない場合は解除しない。
- 失敗時は日本語メッセージを **3秒間、押した本人のみ** に表示。
  - 文言: `機体が指定リスポン位置にある場合のみ 非アクティブ化や機体変更できます`

## 6. Respawn方針
- 既存の `SaccEntitySendEvent`（リスポン棒）機能を維持して使用。
- Active化後、遅延を入れてRespawnイベント発火（30フレーム待機方針）。
- All Respawn は「Active中の機体」を対象に発火し、細かい可否は既存処理に委譲。

## 7. TMPラベル仕様
- `SLMH_SlotLabel` は **TMPと同じGameObject** に付与。
- 表示は `Prefix + 2桁SlotId`（例: `Slot03`）。
- Prefix と SlotId の間に区切り（アンダーバー等）は自動付与しない。
  - 必要ならPrefix側にユーザーが記入。

## 8. 初期割当テーブル
- 保存場所は `VehicleSlotManager` 本体ではなく、**子オブジェクト `SLMH_DefaultAssignments`**。
- `SLMH_DefaultAssignments` は16スロット分の初期値を保持:
  - 機種（A..P）
  - 初期モード（Full / LowPoly）
- 実際の生成数（`GenerateSlotCount`）は別入力。
  - 生成されないスロット分の初期値は適用対象外（参照されない）。

## 9. Slot自動生成（Editor拡張）
- 実行場所: `SLMH_DefaultAssignments` のInspectorボタン。
- 複製元: 常に `Slot_00`。
- 生成先: `Slot_01` 以降（最大 `Slot_15`）。
- 生成時オプション:
  - 生成数（1..16）
  - 軸（X/Z）
  - オフセット量（可変）
- 配置先階層: `Slot_00` と同階層（同じ親配下）。
- 参照自動更新対象:
  - `Manager.Slots[]`
  - 各ボタンの `SlotId`
- 目的は「手間ゼロで再生成可能」な制作支援。

## 10. Inspector表示
- 実行不可条件や注意喚起は、見落とし防止のため **Inspector上のHelpBox表示** を採用する。

## 11. LateJoin同期ブリッジ（実装反映）
- `VehicleSlotManager` 配下に子Udonとして **`SLMH_LateJoinSyncBridge`** を配置する。
- 役割分担:
  - `SLMH_SlotManager_SingleDebug`: 通常同期・状態保持・Apply実行。
  - `SLMH_LateJoinSyncBridge`: LateJoin時のスナップショット要求/応答専用チャネル。
- Joiner側の流れ:
  - Join後に遅延して `NetRequestSnapshot` を `All` へ送信。
  - 未受信時は最大回数まで再要求。
- インスタンスマスター側の流れ:
  - 要求受信時にブリッジのOwnerを確保。
  - 全Slotのactive状態をスナップショット化して同期送信。
  - 到達安定化のため2nd pass再送を行う。
- 受信側の適用:
  - `OnDeserialization` でbridge値をManagerへ反映し、`ApplyAll` で見た目へ適用。

## 12. デバッグログ運用（実装反映）
- 重要ログはVRChatクライアントログに出力される（Unity Consoleのみを正としない）。
- 主な確認観点:
  - Joinerの要求送信
  - マスターの要求受信
  - スナップショット送信（1st/2nd）
  - Joiner側の逆シリアライズ適用
- ログ保存先:
  - `C:\Users\satoshi\AppData\LocalLow\VRChat\VRChat\output_log_*.txt`

## 13. Animation同期ブリッジ（実装反映）
- `SLMH_AnimSyncBridge` を追加し、Animator Parameter同期を担当する。
- `SLMH_VehicleSlot_SingleDebug` から状態反映時に `SLMH_AnimSyncBridge` を発火する。
  - 通常操作（Manager経由）とLateJoin復元（LateJoinBridge経由）の両経路で同じ発火に乗る。
- 同期対象は最大5枠。
  - 各枠: `ParamName` + `ParamType(0=Bool,1=Float,2=Int)`
  - 空欄はスキップ。
- 同期方式:
  - イベント駆動（状態反映時）
  - 低頻度の定期補正（既定60秒）

## 14. Mode分割準備（実装反映）
- 共通基盤として以下を追加:
  - `SLMH_SlotManager_Base`
  - `SLMH_VehicleSlot_Base`
- 単一機種運用は子クラスへ接続:
  - `SLMH_SlotManager_SingleDebug : SLMH_SlotManager_Base`
  - `SLMH_VehicleSlot_SingleDebug : SLMH_VehicleSlot_Base`
- 今後の方針:
  - ModeA(32スロット) / ModeB(機種選択) は、同ベースから派生する構成で実装する。

