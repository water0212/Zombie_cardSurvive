# ADR-0002：建築書籤式功能面板與 Runtime 分離

## Status

Accepted

使用者於 2026-09-17 明確接受 ADR-0002 並授權登錄三項架構邊界及接續實作。Accepted 表示技術決策核准，不代表實作或驗收已完成。

## Date

2026-09-17

## Engine Compatibility

| Field | Value |
|---|---|
| Engine | Unity 2022.3.62f3 |
| Domain | uGUI / TextMeshPro / 建築呈現 |
| Knowledge Risk | LOW；使用現有專案元件，不引入新套件 |
| References Consulted | ProjectSettings/ProjectVersion.txt（專案根目錄）；BuildingDetailView、CardCraftView、CraftJobRowView、BuildingRuntime、BuildingSystem 現有程式 |
| Post-Cutoff APIs Used | None |
| Verification Required | Inspector 引用、事件解除訂閱、ScrollRect 保留位置、左右鍵、流程鎖定、雙解析度 |

專案尚無 engine-reference 與 technical-preferences 文件。以釘選版本與現有程式為依據，不宣稱已通過 Unity 編譯或實機驗證。未配置引擎審查代理，因此未執行專家審查；TD-ADR skipped — Lean mode。

## ADR Dependencies

| Field | Value |
|---|---|
| Depends On | None；只改既有 Canvas 詳情面板，不要求世界渲染遷移完成 |
| Enables | BUILDING-UI-001 |
| Blocks | BUILDING-UI-001 程式實作，直到本 ADR Accepted |
| Ordering Note | 先配置與頁面生命週期，再互動與佇列，最後 Editor 套用和驗證 |

## Context

### Problem Statement

現有 BuildingDetailView.Refresh 每次銷毀並重建所有互動；CardCraftView 也重建所有食譜與工作，且點食譜直接排入製作。此行為無法保留閱讀位置，亦不符合核准的書籤、展開與獨立任務列。

### Constraints

- UI 只讀 Runtime 並轉送請求，沿用 GDD 25.3–25.7 與現有支付、保留、取消和結算時機。
- 不更改 SampleScene 的其他手動排版；不重建整個場景。
- 不加入每日紀錄、劇情或收藏系統，也不把詳細面板自動全部公開。
- 延續 Docs/registry/architecture.yaml 的 Gameplay 權威與流程鎖定邊界；不更改 ADR-0001 的 Proposed 狀態。

### Requirements

TR-BUI-001 至 TR-BUI-008，詳見 tr-registry.yaml 與 BUILDING-UI-001。

## Decision

以獨立的可序列化建築呈現配置定義書籤；每筆包含穩定 pageId、標題、排序、公開旗標，以及既有 BuildingInteractionData 或資訊頁的引用（二擇一）。配置不得保存產能、價格、工作進度或解鎖權威。

BuildingDetailView 綁定建築後建立頁面集合；以 pageId 維護同次開啟的頁面實例、捲動與展開狀態。一般數值通知只更新資料，不重建整組 UI。結構真的改變時才增刪對應頁面。Close/重新 Bind 清除閱讀狀態並重新選預設頁。

未配置建築按照 Interactions 順序產生書籤。不得只列已存在的 CraftLines，否則未解鎖製作功能無法預覽。公開但未解鎖頁讀取靜態互動資料呈現預覽，不建立假的 Runtime、不排程。未公開頁完全不建立書籤。

### Architecture Diagram

建築呈現配置 → BuildingDetailView 書籤／閱讀狀態 → 互動頁或唯讀資訊頁。

互動頁 → BuildingSystem 既有請求 API → Runtime／既有結算 → 資料通知 → 更新頁面。

### Key Interfaces

- 資訊頁共用入口：Bind(BuildingSystem owner, BuildingRuntime building)、Refresh()、Unbind()。具體類別名稱於實作中確定，介面職責不變。
- BuildingSystem.TryQueueCraft(buildingId, lineId, recipe)：沿用，不由 UI 自行保留或支付。
- BuildingSystem.RequestCancelCraft(buildingId, lineId, jobId)：沿用，不由 UI 移除工作或返還資源。
- Job 選取及取消以 JobId 定位；Recipe 僅用於導覽，不作工作唯一鍵。
- 資訊頁各自讀取既有資料來源；情報中心明確配置唯讀基地詳情，不自動發現 DetailPanel。

## Implementation Guidelines

1. 保留 BuildingDetailView 既有公開 Bind/Close 及 prefab 引用相容性；必要的新增引用交由可重複執行 Editor 工具補齊。
2. 同次開啟保留每頁 ScrollRect 位置、展開食譜與 selectedJobId；切頁不改變任何工作。
3. 首選可用書籤，沒有則首個公開預覽；隱藏目前頁後重新選合法頁，不留空按鈕。
4. 食譜標題只展開；明確「加入製作」才送請求。一次展開一個食譜。
5. 任務列獨立捲動並沿用 Jobs 順序。左鍵定位食譜並顯示該 Job；右鍵送取消且 CancelPending 不重送。不增加取消按鈕或確認框。
6. 只顯示當前製作列；非製作頁隱藏任務列並擴大功能區。缺插圖用名稱替代；空資料提供可讀提示。
7. 操作按鈕與提交時皆尊重現有權威檢查；事件／探索查看模式不可繞過 CanSubmitActions。鎖定頁可閱讀但不可執行。
8. 未建造／等級不足從靜態 Data 預覽，升級維持原等級現有規則。不為配合 UI 偷改 Runtime 規則；若發現程式與 GDD 不一致，另列缺陷。
9. Bind 前先解除舊訂閱；Close/Unbind/OnDestroy 解除對應訂閱，避免重開後重複回呼。
10. Editor 工具以既有目標與序列化引用更新，使用核准 PaperUI／Icons；不得依模糊名字替換整個場景或無差別保存其他資產。

## Alternatives Considered

### 每次更新重建所有互動

簡單且沿用現況，但會丟失閱讀位置、反覆分配物件；不採用。

### 將製作工作複製到 UI 共用佇列

易排列，但造成第二份權威資料並混淆平行製作列；不採用。

### 自動收集所有 DetailPanel 為情報中心書籤

省去配置，但會意外公開不應呈現的資訊；不採用。

## Consequences

### Positive

閱讀狀態穩定、舊建築維持相容、可逐步加入明確配置的唯讀資訊頁。

### Negative

需管理頁面生命週期與 ID；暫存頁面比單一頁面占更多記憶體，Close 時釋放。

### Risks

- 重複／空 pageId：Editor 驗證並明示錯誤，不靜默混用狀態。
- 成本可用量變化後按鈕過期：使用既有數值通知更新，提交仍以 Runtime 結果為準。
- 工作結算移除已選 Job：清除失效選取，不切到另一筆同食譜工作。
- 晚到事件與已銷毀 View：對稱解除訂閱並測試重開。
- 長文字與 ScrollRect 巢狀輸入：雙解析度實測；避免同方向容器爭搶捲動。

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|---|---|---|
| ZombieSurvivalCard_GDD.md §25.1–25.3 | View 不擁有進度／成本；升級保留原功能 | UI 配置與 Runtime 分離 |
| §25.5–25.7 | 平行製作列、保留成本、事件後取消 | 每列獨立書籤，沿用下單／取消 API |
| 使用者核准建築 UI 計畫 | 書籤、預覽、Job 導覽、情報中心 | BUILDING-UI-001 與 TR-BUI-001–008 |

## Performance Implications

- CPU：通知式更新，避免所有頁面無條件 Instantiate/Destroy；不新增每幀全量重建。
- Memory：頁面僅保留到本次關閉；不啟動完整測試套件。
- Load Time：Editor 套用產生引用，Runtime 不使用 AssetDatabase。
- Network：無。

## Migration Plan

新增呈現配置與資訊頁共用入口 → 改造 BuildingDetailView → 改造食譜／Job 操作 → Editor 套用既有 Prefab／Scene → 離線編譯 → 小範圍 Unity 驗證。舊 Interactions 保留且未配置仍可用。

## Validation Criteria

依 BUILDING-UI-001 全部驗收項目；證據記入 production/qa/evidence/building-bookmark-panel-evidence.md。未實測項目必須標示待驗證，不以文件完成代替實作通過。

## Related Decisions

- adr-0001-sprite-renderer-base-world-and-canvas-hud-boundary.md（相關邊界，非本次實作前置條件）
- ../../production/epics/building-ui/BUILDING-UI-001.md
