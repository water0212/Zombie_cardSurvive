# Quick Design Spec: PB-P0-014D 資料化士氣金字塔 Profile 與階段效果替換

**Type**: Addition  
**System**: 士氣 / 能量 / 資源 / Run 初始化  
**GDD Reference**: `Docs/GameDesign/ZombieSurvivalCard_GDD.md` 第 30.3、30.4、30.7、30.9 節  
**Date**: 2026-08-24
**Status**: Implemented and verified (2026-08-24)

## Change Summary

建立可資料化的完整士氣金字塔 Profile，並在新 Run 開始時鎖定一個 Profile Runtime Snapshot。士氣跨階時，系統以整組替換方式停用舊階段規則並啟用新階段規則，不允許 Stage 1、Stage 2 疊加，也不因士氣回復而恢復舊效果。

PB-P0-014D 先提供一個固定 Demo Profile；局前多 Profile 選擇與正式說明 UI 留給 PB-P0-014E。

## Motivation

`MoraleSystem` 已能記錄不可逆階段，但目前階段尚未帶有正式資料效果。Profile 必須讓設計者在 Inspector 中設定完整三階方案，同時避免把某個 Profile ID 或特定效果寫死在士氣狀態機內。

階段效果包含持續修正與週期性結算，不能直接視為一次性的 `GameEffectData`。若跨階時只重複執行加減數值，容易產生重複扣除、無法移除 Stage 1、或修改資產後影響進行中 Run 的問題。

## Design Delta

GDD 第 30.3、30.4 節規定：

> 階段效果採整組替換，不疊加：轉換時先移除舊階段的全部效果，再套用新階段效果一次。

> V1 使用完整的 `MoralePyramidProfile` 預設組；玩家不能把不同 Profile 的單項效果自由混搭。

本規格將這些規則具體化為 Profile Data、Run Snapshot 與單一階段效果 Runtime，並確認第一個 Demo Profile 的數值與生效邊界。

## Data Authority

### MoralePyramidProfileData

新增 `MoralePyramidProfileData : ScriptableObject`，至少保存：

- `ProfileId`
- 顯示名稱
- 說明
- Stage 0、Stage 1、Stage 2 的 `MoraleStageDefinition`

每個 `MoraleStageDefinition` 保存顯示資訊與 `MoraleStageEffectEntry` 清單。PB-P0-014D V1 支援：

| Effect Type | Amount 語意 | 執行方式 |
|---|---:|---|
| `LoseResourceEachRound` | 非負整數 | 階段有效期間，每個 Round 輪末失去指定資源 |
| `ModifyMaximumEnergy` | 有號整數 | 階段有效期間，修正實際最大能量 |

Stage 0 不需要建立空效果項目，效果清單可為空。Runtime 不以 `ProfileId` 判斷效果；它只解讀資料中的效果類型與數值。

### Run Snapshot

新 Run 的 `PrepareRun` 必須在開始抽牌前完成 Profile 驗證並複製 Runtime Snapshot。Snapshot 保存 Profile ID、顯示資料與三階效果值；進行中 Run 不持續讀取可編輯 Asset，因此遊戲中修改 Asset 不會靜默改變本局規則。

PB-P0-014E 完成前，SampleScene 與正式 Demo 自動指定 `MORALE-PYRAMID-DEMO-01`。若 Profile 未指定、ID 空白、缺少任一階段或包含 V1 不支援的效果，阻擋新 Run 並提供清楚錯誤，不啟用半套效果。

## Fixed Demo Profile

```text
Profile ID: MORALE-PYRAMID-DEMO-01
顯示名稱: 標準生存

Stage 0：無效果

Stage 1：
LoseResourceEachRound = 1

Stage 2：
ModifyMaximumEnergy = -1
```

完整語意：

- Stage 1 每個合法輪末最多失去 1 資源。
- 資源最低為 0；資源不足時只扣除現有量，不建立負數、欠債或額外士氣損失。
- Stage 2 啟用時，Stage 1 的每輪資源損失停止，不與 Stage 2 疊加。
- Stage 2 對基礎最大能量套用一份 `-1` 修正；`EffectiveMaxEnergy = max(0, BaseMaxEnergy - 1)`。
- 最大能量允許為 0，因為卡牌能量消耗可以為 0。
- Stage 2 修正只存在一份，不會在每個 Round 或每次士氣提交時再次減少。
- 之後增加基礎最大能量時，Stage 2 的 `-1` 修正仍保留。例如基礎最大能量由 1 增加為 2，實際最大能量由 0 變為 1。

## Activation and Replacement Rules

1. 新 Run 以 `MoraleCollapseStage = 0` 開始，鎖定 Profile Snapshot，啟用 Stage 0 空效果組。
2. 完整士氣批次首次跨入 Stage 1 或 Stage 2 後，才執行一次階段替換。
3. 一次由 Stage 0 跨到 Stage 2 時，可保留既有的兩筆階段轉換紀錄，但最終只啟用 Stage 2 效果，不短暫執行 Stage 1 的週期性損失。
4. 重複提交相同階段、重新整理 UI 或士氣回復，都不得重新套用效果。
5. 階段替換不回頭改寫觸發降階的既有士氣交易或輪末快照。
6. Stage 1 的資源損失在輪末指定結算點依當時有效階段執行一次；若該結算點前已進入 Stage 2，本輪不再執行 Stage 1 損失。
7. Stage 2 在跨階當下不扣除目前 Turn 已取得的能量；最大能量修正於下一次 Turn 補充能量時提交，並將該 Turn 能量補到新的實際最大值。
8. 新 Run、重新開始或重置測試時，必須清除 Profile Snapshot、目前效果組、待提交的能量修正與每輪執行記錄。

## Runtime Architecture

- `MoraleSystem` 仍是 `CurrentMorale`、`MaximumMorale`、`MoraleCollapseStage` 與崩潰判定的唯一權威，不直接判斷 Profile ID。
- 新的 Profile Runtime 保存本局 Snapshot、目前啟用階段與效果查詢結果，並以目前階段整組取代舊組。
- `RoundEndResolver` 在固定位置查詢目前的每輪資源損失，透過 `ResourceSystem` 將資源限制在 0 以上並記錄實際扣除量。
- `EnergySystem` 區分基礎最大能量與 Profile 修正；增加／減少最大能量的既有成長入口修改基礎值，實際最大能量由兩者計算並限制在 0 以上。
- 階段效果不使用一次性 `GameEffectData` 反覆加減來模擬持續狀態，避免跨階移除與重複套用錯誤。

## Affected Systems

| System | Impact | Action Required |
|---|---|---|
| `MoralePyramidProfileData` | 新增完整三階 Profile Asset | 定義 Profile、階段與 V1 效果資料 |
| Profile Runtime | 新增本局 Snapshot 與效果組權威 | 驗證、鎖定、替換、查詢及重置 |
| `MoraleSystem` | 已提供不可逆轉換紀錄 | 在完整批次後通知 Profile Runtime，不寫死效果 |
| `ResourceSystem` | Stage 1 每輪最多扣 1 | 提供限制到 0 並回報實際扣除量的入口 |
| `EnergySystem` | Stage 2 持續修正實際最大能量 | 區分基礎值與 Profile 修正，最低允許 0 |
| `RoundEndResolver` | 新增固定的階段週期效果結算點 | 每輪至多執行一次並納入報告 |
| `RunPhaseController` | 新 Run 需要鎖定 Profile | 在開始流程前驗證並建立 Snapshot，重置時清除 |
| PB-P0-014E UI | 後續讀取 Profile 與生效階段 | 本故事只提供資料與 Runtime 查詢，不製作正式 UI |

## Acceptance Criteria

- [x] 新 Run 能從 `MORALE-PYRAMID-DEMO-01` 建立不可被 Asset 即時編輯改寫的 Runtime Snapshot。
- [x] Profile 遺漏或資料無效時不開始 Run，也不留下半套階段效果。
- [x] Stage 0 不改變資源、基礎最大能量或實際最大能量。
- [x] Stage 1 啟用後每個合法輪末只失去 1 資源；資源 0 時保持 0，且不影響士氣。
- [x] 進入 Stage 2 後 Stage 1 資源損失停止，同一輪不因兩階紀錄而執行兩套效果。
- [x] Stage 2 只套用一份最大能量 `-1`，最低允許為 0，重複士氣提交不會繼續降低。
- [x] Stage 2 不扣除目前 Turn 已取得的能量，下一個 Turn 才依新實際最大能量補充。
- [x] Stage 2 期間增加基礎最大能量時，實際最大能量正確保留 `-1` 修正。
- [x] 士氣回復到 70 或 35 以上不恢復 Stage 0／Stage 1 效果。
- [x] 新 Run 完整清除上一局的 Snapshot、階段效果、待提交修正與輪次記錄。
- [x] 不影響 PB-P0-014B 的士氣批次、PB-P0-014C 的下輪效果或卡牌自動打出流程。

## Out of Scope

- 局前 Profile 選擇、介紹、比較與確認 UI；屬於 PB-P0-014E。
- 第二個以上的正式 Profile。
- 玩家自由混搭不同 Profile 的正面與負面效果。
- 食物、卡牌使用次數、殭屍體質或其他高耦合階段效果。
- Save State 與讀檔；Runtime Snapshot 需保留可序列化方向，但本故事不實作存檔。
- 正式數值平衡與長局死亡螺旋驗收。

## Verification Plan

- Edit Mode：Profile 驗證、Snapshot 鎖定、Stage 0／1／2 原子替換與重複提交冪等性。
- Edit Mode：Stage 1 每輪一次、資源不足、跨階同輪及 Stage 2 停止 Stage 1。
- Edit Mode：Stage 2 最大能量 1 -> 0、基礎最大能量後續成長、下一 Turn 才生效及新 Run 重置。
- Unity 編譯、受影響測試、全套 Edit Mode 回歸與 Console 0 errors。

## Implementation and Verification Record

- 已建立固定 Demo Profile Asset 並指定給 SampleScene 的 `RunPhaseController`。
- 014D 與受影響定向測試 23 / 23 通過。
- 最終全套 Unity Edit Mode 190 / 190 通過，編譯與測試 Console 0 errors。
- SampleScene Play Mode 可正常以固定 Profile 自動開局，0 errors；既有事件池仍會提示第 1 天無合法事件並使用 neutral fallback，與本規格無關。

## GDD Update Required?

是，已同步套用至 `Docs/GameDesign/ZombieSurvivalCard_GDD.md` 第 30.4、30.9、30.10 節；同時完成 PB-P0-014C 在第 30.6 節的下輪效果命名與永久損傷上限規則同步。
