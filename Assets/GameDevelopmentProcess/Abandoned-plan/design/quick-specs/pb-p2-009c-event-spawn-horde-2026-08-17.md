# Quick Design Spec：PB-P2-009C 事件生成屍群

**類型**：Addition\
**系統**：事件／威脅／戰鬥\
**GDD 依據**：`ZombieSurvivalCard_GDD.md` 第 28.8 節\
**日期**：2026-08-17\
**預估實作**：4～6 小時，不含 PB-P2-009D 正式內容製作

## Backlog Reference

- Backlog：`Docs/GameDesign/ZombieSurvivalCard_Scrum_Backlog.md`
- Product Backlog Item：`PB-P2-009C`
- 前置項目：`PB-P2-009A`、`PB-P2-009B`
- 後續項目：`PB-P2-009D`

## 變更摘要

新增事件專用的 `SpawnHorde` 效果，將事件開始時鎖定的威脅快照傳入既有 `HordeBudgetRuntime` 與 `HordeCompositionRuntime`，玩家選擇事件選項後，把組成結果批次轉為場上的 `ZombieThreatRuntime`。

本故事只負責通用程式串接與驗證，不製作正式 Horde Profile 與殭屍池 Asset。

## Scope

### In Scope

本項目包含：

- 在 `GameEffectData` 增加 `Threat / SpawnHorde` 事件效果。
- 讓 Inspector 可以編輯 `HordeProfile` 與 `EventBonusBudget`。
- 在事件開始時使用已鎖定的 `ThreatSnapshot`。
- 在事件顯示前計算並保存 `HordeBudgetSnapshot` 與 `HordeCompositionResult`。
- 玩家選擇選項後，將保存的屍群組成批次加入 `CombatSystem`。
- 對無效 Profile、無合格殭屍與預算不足等情況執行完整的事前驗證。
- 生成失敗時，不消耗事件成本、不修改 Flag，也不產生部分屍群。
- 為上述流程建立 Edit Mode 自動測試。

### Out of Scope

本項目不包含：

- 建立正式的 `HordeProfileData` 遊戲資產。
- 將正式 Profile 接到 `EVT-008` 或 `EVT-014`。
- 修改事件、美術或殭屍威脅 UI 排版。
- 修改既有 `AddZombieThreat` 效果的行為。
- 修改屍群預算與組成演算法。
- 修改 GDD 28.8 的既有設計規則。

正式 Profile、事件資料接線與實際內容驗收由 `PB-P2-009D` 負責。

## 動機

目前系統已能計算屍群預算與殭屍組成，但結果不會進入 `CombatSystem`。因此 `EVT-014 屍群來臨` 尚無法真正產生玩家需要處理的場上威脅。

完成後，殭屍事件會從文字結果變成可見、可點擊、可分配戰鬥值處理的實際壓力。

## 設計差異

目前 GDD 規定：

> PB-P2-009A/B 只產生殭屍卡清單與花費結果，不直接生成場上殭屍。

本故事追加：

> 事件的 SpawnHorde 效果會把鎖定的 HordeCompositionResult 批次加入 CombatSystem，但不負責基地傷害、建築受損或失敗條件。

## 資料設定

`GameEffectData` 增加事件專用分類：

```text
GameEffectCategory.Threat
ThreatEffectType.SpawnHorde
```

`SpawnHorde` 包含：

- `HordeProfileData HordeProfile`
- `int EventBonusBudget`

Inspector 顯示：

- 效果分類：威脅／屍群
- 效果類型：生成屍群
- 屍群 Profile
- 事件額外預算

`SpawnHorde` 不使用一般 `EventMagnitude` 縮放。屍群強度只能來自：

```text
Profile BaseBudget
+ WorldThreatTier
+ BaseAttention
+ RegionPressure
+ EventBonusBudget
```

基地事件的 `RegionPressure` 固定為 0；未來地區事件可另行傳入地區壓力。

## 執行流程

1. `EventFlowController.BeginEventPhase()` 建立並鎖定當日 `ThreatSnapshot`。
2. 解析事件選項時，遇到 `SpawnHorde`：
   - 讀取 `HordeProfileData`。
   - 使用鎖定的 `WorldThreatTier` 與 `BaseAttention`。
   - 加入事件設定的 `EventBonusBudget`。
   - 產生 `HordeBudgetSnapshot`。
   - 產生並暫存 `HordeCompositionResult`。
3. 玩家尚未選擇前，不生成場上殭屍。
4. 玩家選擇該選項後，事件執行計畫先完成可用性檢查。
5. 檢查通過後，批次將每筆 `HordeCompositionEntry.ZombieCard` 交給 `CombatSystem`。
6. `CombatSystem` 為每張卡建立一個 `ZombieThreatRuntime`。
7. `CombatSystem.StateChanged` 通知既有殭屍 UI 更新。
8. 事件進入結果畫面；玩家可使用目前的「暫時查看遊戲資訊」功能查看新增殭屍。
9. 殭屍沿用既有標記擊殺與輪末結算流程。

## 組成鎖定與亂數

屍群組成在事件畫面出現時即鎖定，選擇事件後不重新抽取。

亂數種子由以下資料建立：

```text
RunSeed
+ EventDay
+ EventId 穩定雜湊
+ ChoiceIndex
+ EffectIndex
```

不得使用 `string.GetHashCode()`，以免不同執行環境產生不同結果。應使用專案內固定演算法或新增簡單的穩定字串雜湊。

相同存檔種子、日期、事件與選項應得到相同屍群組成。

## 失敗與邊界規則

- `HordeProfile` 未指定：選項判定為設定無效，不可執行。
- Profile 沒有合法殭屍：選項不可執行。
- 最終預算買不起任何合法殭屍：選項不可執行。
- 不可執行時不得扣除事件成本，也不得清除相關 RunFlag。
- 玩家場上已有殭屍時，新屍群直接追加，不取代、不合併舊殭屍。
- 每筆組成結果只能生成一次。
- 保留既有每次屍群最多 256 隻的安全限制。
- `AddZombieThreat` 舊效果維持原行為，不改成屍群生成。
- SpawnHorde 成功後，後續效果才可清除 `FLAG_HORDE_PENDING`。
- 基地血量、建築損壞、防禦減傷與失敗條件不在本故事範圍。

## CombatSystem 調整

新增批次生成入口，概念介面：

```text
SpawnZombieThreats(IReadOnlyList<ZombieCardData>)
```

規則：

- 先確認所有輸入卡牌合法，再修改場上狀態。
- 每張卡建立一個獨立 `ZombieThreatRuntime`。
- 整批完成後只發送一次 `StateChanged`。
- 回傳本次建立的 Runtime 清單，供測試與 Debug 使用。

## 除錯資訊

`EventFlowController` 或解析後效果應保留最近一次：

- `HordeBudgetSnapshot`
- `HordeCompositionResult`
- 實際生成數量
- 停止原因
- 使用的亂數種子

這些資訊先供測試與 Debug 使用，不直接公開給正式玩家 UI。

## PB-P2-009D 邊界

本故事不建立或修改正式 Horde Profile Asset，也不把 `EVT-014` 標記為正式可用。

`PB-P2-009D` 才負責：

- 建立第一批正式 `HordeProfileData`。
- 設定正式殭屍池、成本、權重與 Tier。
- 將 Profile 接入 `EVT-014`。
- 讓 `EVT-008` 的選項設定 `FLAG_HORDE_PENDING`。
- 讓 `EVT-014` 成功生成後清除該 Flag。
- 執行正式事件到戰鬥 UI 的平衡驗收。

## Dependencies

| 項目 | 狀態 | 本項目使用內容 |
|---|---|---|
| PB-P2-009A | DONE | `HordeProfileData`、威脅快照與屍群預算計算 |
| PB-P2-009B | DONE | 確定性屍群組成與停止原因 |
| PB-P1-012 | DONE | 事件階段鎖定與事件執行流程 |
| PB-P1-013 | DONE | 選項執行前驗證與原子化執行 |
| PB-P2-009D | FOLLOW-UP | 正式 Profile 資產及 EVT-008／EVT-014 接線 |

`PB-P2-009D` 不是 `PB-P2-009C` 的前置相依，因此不阻擋本項目實作。測試可以使用測試中建立的暫時 `HordeProfileData`。

## 受影響系統

| 系統 | 影響 | 必要動作 |
|---|---|---|
| GameEffectData | 增加 Threat／SpawnHorde 設定 | 新增 enum、Profile 與額外預算欄位 |
| Inspector Drawer | 顯示屍群專用欄位 | 隱藏一般事件縮放欄位 |
| Resolved Event Runtime | 鎖定屍群組成 | 保存 Budget 與 Composition |
| EventFlowController | 提供威脅快照與穩定種子 | 不直接生成殭屍 |
| Horde Runtime | 沿用既有計算 | 不修改公式 |
| CombatSystem | 支援批次生成 | 一次通知 UI |
| Zombie UI | 沿用 StateChanged 更新 | 不改排版邏輯 |
| Event Content | 本故事不改正式 Asset | 留待 PB-P2-009D |

## 驗收條件

- [x] 事件效果可在 Inspector 選擇 `Threat → SpawnHorde`。
- [x] SpawnHorde 必須指定 `HordeProfileData`。
- [x] 基地屍群使用鎖定的 `WorldThreatTier` 與 `BaseAttention`，`RegionPressure = 0`。
- [x] `EventBonusBudget` 只加入一次。
- [x] 同一種子與事件資料會產生相同屍群組成。
- [x] 事件顯示後改變其他遊戲數值，不會改變已鎖定的屍群。
- [x] 玩家尚未選擇前，場上殭屍數量不變。
- [x] 選擇後生成數量與 `HordeCompositionResult.SpawnCount` 完全一致。
- [x] 每個 `ZombieThreatRuntime.SourceCard` 都對應組成結果中的 `ZombieCardData`。
- [x] Profile 無效或無法生成殭屍時，不扣成本、不清 Flag。
- [x] 場上既有殭屍不會被覆蓋。
- [x] UI 能收到狀態更新並顯示新增殭屍。
- [x] 新殭屍可沿用既有標記擊殺與輪末結算。
- [x] 舊 `AddZombieThreat`、探索殭屍卡與既有事件效果不受影響。
- [x] 全套 Edit Mode 測試通過，Console 0 errors。

## Verification Plan

### Edit Mode Tests

預計新增：

`Tests/Editor/EventHordeSpawnEffectTests.cs`

至少驗證：

- `SpawnHorde` 使用鎖定的威脅快照，而不是選擇當下重新讀取世界狀態。
- `EventBonusBudget` 正確加入事件屍群預算。
- 事件屍群的 `RegionPressure` 固定為 `0`。
- 相同 Seed 與輸入會得到相同屍群組成。
- 顯示事件時只建立預覽，不會提前生成殭屍。
- 玩家確認選項後才把保存的屍群加入 `CombatSystem`。
- 新殭屍追加到既有威脅，不會覆蓋既有殭屍。
- 批次生成只觸發一次 `CombatSystem.StateChanged`。
- Profile 無效、沒有合格卡牌或預算不足時，選項執行失敗。
- 失敗時不消耗成本、不修改 Flag、不產生部分結果。
- `AddZombieThreat` 的原有行為不受影響。
- 生成數量不得超過 256 個安全上限。

### Unity Integration Check

完成實作後確認：

- Unity 專案能正常編譯。
- 新增及既有相關 Edit Mode 測試全部通過。
- SampleScene 能正常進入 Play Mode。
- Console 為 0 errors。
- 不需要建立或修改正式 Horde Profile 資產。

## Definition of Complete

`PB-P2-009C` 必須同時符合以下條件才算完成：

- `SpawnHorde` 可以從事件預覽一路執行到 `CombatSystem`。
- 所有 Acceptance Criteria 均有測試或 Unity 驗證證據。
- 新增測試與既有 Horde、Event、Combat 測試全部通過。
- Unity Console 為 0 errors。
- 沒有提前實作屬於 `PB-P2-009D` 的正式內容資產與事件接線。

## GDD 更新

不需要。此故事是在實作 GDD 第 28.8 節已定義的責任邊界，沒有改變既有規則。

## 實作驗收

- 完成日期：2026-08-17
- Unity：2022.3.62f3
- `EventHordeSpawnEffectTests`：10/10 通過
- Edit Mode 全套回歸：128/128 通過
- Console：0 errors
- 正式 Horde Profile 與 EVT-008／EVT-014 接線仍留給 PB-P2-009D
