# 建築書籤面板：驗收紀錄

- Date: 2026-09-17
- Story: production/epics/building-ui/BUILDING-UI-001.md
- Overall: PASS WITH NOTES — 2026-09-17 輕量結案 Review

## 使用者驗收

- 使用者已確認事件與探索操作正常。
- 使用者於小範圍 Play 中確認建築書籤切換、食譜展開、加入製作、任務操作與整體排版正常。
- 初次發現同一食譜再次點擊不會收合；修正後使用者再次確認可正常收合。
- 情報中心食物計畫的同方案點擊會浪費本回合調整權；使用者在 `TrySetPendingPlan()` 加入正規化後的同方案拒絕條件並確認正常。

## 自動與靜態檢查

- 釘選 Unity 2022.3.62f3 已從本機 ProjectVersion.txt 確認。
- 低併發離線編譯：348 warnings、0 errors；警告主要為既有序列化欄位的靜態編譯提示。
- Unity MCP Console：0 errors。
- 針對性檢查全部通過：
  - `StaticLockedPreview_DoesNotCreateRuntimeOrReactivateHiddenPage`
  - `ExpandRecipe_TogglesReadingSelection_WithoutCreatingJob`
  - `DuplicateRecipeJobs_SelectById_RemovalDoesNotSelectSibling`
  - `PendingCancellation_HasExplicitTextNotOnlyColour`
- Editor 套用工具曾連續套用驗證，目標後代物件數維持 26，未重建 SampleScene。
- 必要 Prefab、情報中心呈現配置與資訊頁資產引用均存在。

## 驗收矩陣

| 範圍 | 驗收項目 | 結果 |
|---|---|---|
| 左側與建築狀態 | AC01–03、AC13 | PASS WITH NOTE |
| 頁面與閱讀位置 | AC04–05、AC10–12 | PASS |
| 食譜、成本與資料缺省 | AC06–07、AC15 | PASS WITH NOTE |
| Job 定位／取消與結算 | AC08–09 | PASS |
| Editor 冪等與引用 | AC14 | PASS |
| 雙解析度及文字／非顏色提示 | AC15 | PASS |
| 離線編譯與小範圍 Play | AC16 | PASS |

## 畫面證據

- `Temp/CodexPaperUI/building_bookmark_720.png`（1280×720）
- `Temp/CodexPaperUI/building_bookmark_1080.png`（1920×1080）
- `Temp/CodexPaperUI/building_info_720.png`（1280×720）

## 非阻擋備註

1. 卡牌強化沿用既有 Runtime 的 `Active`-only 限制；建築升級期間暫不可強化。此點與 GDD「升級期間維持原等級功能」仍有既有差異，沒有在本 UI story 中更改經濟規則。
2. 無食譜、極長文字、隱藏後重新公開等低風險邊界主要由靜態及程式檢查覆蓋，未逐項建立獨立 Play case。

不得執行曾造成記憶體壓力的完整測試套件。每次更新需補上實際命令或操作、輸出、截圖路徑與剩餘限制。
