# Quick Design Spec: PB-P0-014A 士氣崩潰核心

**Type**: Addition  
**System**: 士氣 / Run 結束  
**GDD Reference**: `Docs/GameDesign/ZombieSurvivalCard_GDD.md` 第 30.2、30.3、30.7、30.10 節  
**Date**: 2026-08-18

## Change Summary

把既有單一士氣值擴充為當前士氣、永久士氣上限與不可逆崩潰階段，並在完整士氣提交後提供只觸發一次的基地崩潰結果。本切片只建立通用 Runtime 與 Run 停止點，不改食物規則、殭屍資料、Profile 內容或正式 UI。

## Design Delta

現行 `MoraleSystem` 只保存可恢復的士氣值、自然回落線與固定最大值；每個來源直接改值，沒有階段紀錄、永久上限變化或失敗訊號。

本故事改為：

1. 新局初始化 `CurrentMorale = 100`、`MaximumMorale = 100`、`MoraleCollapseStage = Stable`。
2. 所有士氣變化由單一批次提交 API 處理；舊 `AddMorale`、`ReduceMorale`、`ChangeMorale` 保留為相容包裝。
3. 先套用永久上限變化並限制當前士氣，再套用當前士氣淨變化。
4. 完整提交後以 `< 70`、`< 35` 推進不可逆階段；一次可以跨越兩階並留下兩筆轉換紀錄。
5. 士氣回復受永久上限限制，但不降低階段。
6. 完整提交後士氣為 0 時，基地崩潰只觸發一次；Run 進入 Failed 階段並停止事件或下一輪。
7. 014A 只提供階段變更事件，不套用任何正式正負效果。

## New Runtime Values

| Value | Default | Rule |
|---|---:|---|
| Initial Morale | 100 | 新 Run 固定起點 |
| Initial Maximum Morale | 100 | 本切片固定起點 |
| Stable Threshold | 70 | 69 才進入 Shaken |
| Critical Threshold | 35 | 34 才進入 Brink |
| Collapse Stage | Stable | 只能 Stable → Shaken → Brink |

## Affected Systems

| System | Impact | Action Required |
|---|---|---|
| `MoraleSystem` | 成為士氣狀態唯一寫入點 | 新增批次提交、上限、階段、結果與事件 |
| `RoundEndResolver` | 新局不再讀取舊 70 起始值 | 使用 100 初始化；既有逐項來源留待 014B 聚合 |
| `RunPhaseController` | 接收基地崩潰並停止流程 | 新增 Failed 階段、RunFailed 事件與流程守衛 |
| 事件／卡牌／探索 | 仍可使用舊 API | 經相容包裝自動走新士氣提交 |
| Debug UI | 只需能看見新狀態 | 014A 顯示 Current / Maximum / Stage；正式 UX 留待 014E |

## Out of Scope

- 食物溢出、安全儲備與食物計畫改版。
- 輪末多來源原子聚合。
- 殭屍永久上限損傷與完整反應窗口。
- `MoralePyramidProfile` 與階段正負效果。
- 正式 HUD、跨階預警與崩潰畫面。
- Save State。

## Acceptance Criteria

- [x] 新局為 100 / 100 / Stable。
- [x] 70 不降階，69 只觸發一次 Shaken。
- [x] 35 不降階，34 只觸發一次 Brink。
- [x] 從 80 一次降至 30 時，記錄 Stable → Shaken → Brink。
- [x] 回復到 100 不降低既有階段。
- [x] 士氣不超過永久上限；永久上限下降不重複扣除同額士氣。
- [x] 同階重複提交不重播階段轉換。
- [x] 士氣 0 只觸發一次基地崩潰，Run 不再進入事件或下一輪。
- [x] StartRun 完整重置士氣、上限、階段與崩潰旗標。
- [x] 舊士氣 API 維持可用，既有來源不會雙重套用。

## Verification

- Unity Edit Mode：`MoraleCollapseCoreTests` 10 / 10 通過。
- 受影響回歸：士氣核心、事件付款、臨時食物共 30 / 30 通過。
- 全套 Edit Mode：148 / 150 通過；其餘 2 項為既存 `EVT-014` 屍群事件資產的效果順序／引用問題，與本切片修改檔案無交集，留在 PB-P2-009D 範圍處理。
