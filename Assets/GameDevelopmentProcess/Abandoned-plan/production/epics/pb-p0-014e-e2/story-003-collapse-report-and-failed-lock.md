# PB-P0-014E-E2-003：基地崩潰報告與失敗鎖定

- Status: Implemented — manual QA pending
- Date: 2026-10-25
- Type: Integration / UI
- TR-ID: TR-MUI-009
- GDD Reference: `Docs/GameDesign/ZombieSurvivalCard_GDD.md` §30.5～30.10；`design/ux/hud.md` 的結算出口、流程優先級與失敗接管規則。
- ADR Governing Implementation: `Docs/Architecture/adr-0003-morale-preview-and-warning.md`；沿用其 Runtime 權威、完整交易結果與流程鎖定原則。
- Parent Plan: `production/epics/PB-P0-014E-plan.md` E2。
- Dependencies: PB-P0-014E-E2-001；既有 `MoraleSystem`、`RunPhaseController` Failed 流程及卡牌／事件／輪末士氣交易；E3 提供最終 Profile 選擇畫面。

## Scope

以正式交易結果建立基地崩潰報告，讓任何合法來源造成的士氣歸零只進入一次失敗流程，停止後續遊戲推進，並將重新開始請求交給局前 Profile 選擇入口。

## Acceptance Criteria

- [ ] E2-003-01：崩潰報告顯示交易前後的目前士氣、士氣上限、造成最後變化的來源及主要原因；資料來自完整交易結果，不從結算後全域數值倒推。
- [ ] E2-003-02：文案以居民拒絕命令、內部分裂、出走或放棄據點表達基地瓦解，不能只顯示沒有因果的 `Game Over`。
- [ ] E2-003-03：輪末、事件與卡牌造成 `CurrentMorale == 0` 時均由同一失敗入口接管；同一交易或重複通知只建立一份報告並觸發一次 Failed。
- [ ] E2-003-04：權威 Failed 成立後，停止後續事件生成／選擇、建築結算、下一輪、手牌與 Gameplay 操作；已排定的其他一般呈現不得覆蓋失敗畫面。
- [ ] E2-003-05：崩潰優先於事件與正常出口；失敗報告接管前不短暫解除 Gameplay 鎖定，接管後不能返回被中斷的事件、探索或暫時查看流程。
- [ ] E2-003-06：左鍵／`Space` 略過前置士氣或跨階呈現後，仍顯示完整崩潰原因；略過輸入不得穿透並觸發結果畫面的操作。
- [ ] E2-003-07：`重新開始` 只發出一次前往局前 Profile 選擇入口的請求，不自行建立半套新 Run；Profile 選擇畫面與完整重開清理由 E3 實作並完成端到端驗收。
- [ ] E2-003-08：`離開本局` 只在現有場景導覽提供合法出口時顯示並呼叫該入口；不得憑空建立不存在的主選單或無效按鈕。
- [ ] E2-003-09：定向測試涵蓋輪末、事件、卡牌致死、重複失敗通知、待開事件與無呈現元件；Play 驗證失敗後無法操作或推進。

## Implementation Notes

- 失敗守衛與完整報告就緒分開處理：守衛立即停止遊戲，報告等待已提交交易的完整原因後呈現。
- 報告只讀取交易資料，UI 不修改士氣、不重播來源效果，也不自行選擇主要原因。
- `重新開始` 透過明確的流程請求交給 E3；在 E3 完成前可驗證請求只送出一次，但不得宣稱完整重開流程已驗收。
- 沿用最高層覆蓋與輸入阻擋規則，避免失敗畫面與事件／探索同時可操作。

## Out of Scope

完整跨局歷史統計、排行榜、存檔、特殊終局、新主選單、Profile 選擇畫面本體、重開局完整清理及新失敗原因規則。

## Test Evidence Required

- Automated: 針對三種致死來源、單次 Failed、流程停止、出口優先序、重複通知及重新開始請求的 EditMode 測試；實際路徑記入證據。
- Manual: SampleScene 驗證完整原因、失敗接管、略過後報告、按鈕狀態與失敗後無 Gameplay 操作。
- Evidence: `production/qa/evidence/pb-p0-014e-e2-evidence.md`。
- E3 完成前，重新開始至 Profile 選擇的端到端項目須標記待驗證，不得提前宣稱通過。


## 2026-10-29 實作狀態

已完成 E2 程式與 GameplayHUD_P0_Integration 接線。55 項定向測試通過；人工輸入、指定解析度／灰階與 E3 端到端尚待驗收，故不將所有驗收框勾選。2026-10-30 已將測試副本驗證後的 E1／E2 接線整合至 SampleScene，無 Presenter 的相容路徑仍保留。

架構與腳本關聯圖：`Assets/Docs/Architecture/e2-value-transactions.md`。驗證證據：`Assets/production/qa/evidence/pb-p0-014e-e2-evidence.md`。
