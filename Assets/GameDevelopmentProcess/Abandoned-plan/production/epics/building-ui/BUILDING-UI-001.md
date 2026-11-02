# BUILDING-UI-001 — 建築詳情與書籤式功能面板

- Status: Complete with Notes — lightweight close-out review approved by user on 2026-09-17
- Layer: Presentation / UI
- Type: UI
- TR-ID: TR-BUI-001, TR-BUI-002, TR-BUI-003, TR-BUI-004, TR-BUI-005, TR-BUI-006, TR-BUI-007, TR-BUI-008
- ADR Governing Implementation: Docs/Architecture/adr-0002-building-bookmark-presentation.md
- Manifest Version: None — 尚無 control-manifest；使用本 ADR 的 Implementation Guidelines，不假稱全專案規則已補齊
- Dependencies: 既有建築 Runtime、成本保留、PaperUI、核准四種圖示；無新增上游 story。事件／探索操作已由使用者確認正常。
- Test Evidence: production/qa/evidence/building-bookmark-panel-evidence.md
- Design approval: 使用者已明確要求實作完整計畫；2026-09-17 同意補齊前置文件。

## Scope

改造 BuildingDetailView、CardCraftView、CraftJobRowView；新增獨立 UI 配置、資訊頁入口、情報中心唯讀基地頁及冪等 Editor 套用工具。保持既有經濟和結算權威。

## Acceptance Criteria

- [x] AC01 / TR-BUI-001：左建築資訊 → 中功能書籤 → 右功能頁 → 最右目前製作列；非製作頁隱藏任務列並釋出寬度。
- [x] AC02 / TR-BUI-002：名稱、狀態、等級、目前效果、下一級收益及需求完整；依狀態提供施工、升級或取消及施工進度；最高級不顯示虛構下一級。
- [x] AC03 / TR-BUI-003：未建造／等級不足的公開功能可預覽，停用操作並顯示解鎖條件；不公開功能不留空書籤。
- [x] AC04 / TR-BUI-003：首次開啟選第一個可用書籤，無則第一個可預覽；同次保留各頁捲動與展開；重開回到預設。
- [x] AC05 / TR-BUI-004：每個互動一個具功能名稱的書籤；兩條製作列各有產能與佇列，切頁不移動、取消或重排工作。
- [x] AC06 / TR-BUI-004：點食譜只展開，一次一個；顯示產出卡、效果、食物／資源成本、所需製造力及加入製作按鈕；不能排入提供原因。
- [x] AC07 / TR-BUI-005：任務依實際順序、可独立捲動、顯示圖示／缺圖名稱、進度與狀態；同食譜多筆不合併。
- [x] AC08 / TR-BUI-005：左鍵依 JobId 捲到食譜、展開並呈現該筆任務；任務消失後不錯選同食譜另一筆。
- [x] AC09 / TR-BUI-005：右鍵呼叫既有取消 API，待取消不重送；無額外取消按鈕／確認窗，結算時才移除與釋放；顯示左鍵查看／右鍵取消提示。
- [x] AC10 / TR-BUI-006：可序列化配置具有穩定 ID、標題、順序、公開旗標及互動或資訊引用；未配置按 Interactions 順序；明確配置才加入資訊頁。
- [x] AC11 / TR-BUI-006：資訊頁共用 Bind/Refresh/Unbind，情報中心基地詳情唯讀顯示已有數值；隱藏再公開可進入且不改遊戲數值。
- [x] AC12 / TR-BUI-007：數值刷新、加工作、切頁不無條件重建頁面或丟失閱讀位置；解除綁定不殘留訂閱。
- [x] AC13 / TR-BUI-007：關閉與既有流程鎖定保留，事件／探索不能繞過；升級維持既有原等級規則；UI 不持有經濟權威。
- [x] AC14 / TR-BUI-007：Editor 工具可重複套用核准紙張／圖示與場景／Prefab 引用，不重建 SampleScene、不覆寫無關手動修改。
- [x] AC15 / TR-BUI-008：未建造、正常、升級中、最高級、資源不足、滿列、無食譜、空佇列、缺圖及長文字驗證；雙解析度無遮擋、捲動独立，狀態不只靠顏色。
- [x] AC16 / TR-BUI-008：離線編譯、針對性檢查及小範圍 Play 證據分別記錄；不執行完整高記憶體測試套件。

## Implementation Notes

遵守 ADR-0002 全部 Implementation Guidelines。已確認現有 Runtime 只有等級解鎖後才建立 CraftLineRuntime，預覽必須由靜態互動 Data 供應，不能假造實際製作列。既有 RequestCancelCraft 已防重複待取消，不改其返還時機。

## Out of Scope

每日收支／昨日紀錄、劇情卡牌、收藏系統、新製作或資源平衡規則、世界渲染遷移、完整場景重建、全測試套件、全專案文件遷移。

## Verification Procedure

先對照 API 與配置引用 → 低併發離線編譯 → Unity Console → 克隆面板或隔離範例的 1280×720／1920×1080 檢查 → 小範圍 Play 覆核 AC01–16。記錄工具不可用、未執行及失敗，不填造 PASS。

## Completion Notes

- Completed: 2026-09-17
- Verdict: Complete with Notes（使用者核准輕量結案 Review）
- Criteria: 16/16 由程式檢查、4 項針對性測試、雙解析度證據及使用者小範圍 Play 組合覆蓋。
- Test Evidence: `production/qa/evidence/building-bookmark-panel-evidence.md`
- Code Review: Lean mode；未執行完整 lead-programmer／QA agent gate。
- Advisory: 卡牌強化仍保留既有 Runtime 的 `Active`-only 規則，因此建築升級期間暫不可使用；這與 GDD「升級維持原等級功能」仍有既有差異，本 story 未擴大修改經濟規則。
- Advisory: 無食譜、極長文字及隱藏後重新公開等低風險邊界主要由靜態／程式檢查覆蓋，未逐項進行獨立 Play case。
- Scope: `ResourceReservationService.StateChanged` 為 UI 即時刷新所需的有效擴充；未改變成本保留、提交或返還權威。
