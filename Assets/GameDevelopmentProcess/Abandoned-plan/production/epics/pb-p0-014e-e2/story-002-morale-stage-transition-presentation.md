# PB-P0-014E-E2-002：士氣跨階、略過與減少動態呈現

- Status: Implemented — manual QA pending
- Date: 2026-10-25
- Type: Visual/Feel
- TR-ID: TR-MUI-008
- GDD Reference: `Docs/GameDesign/ZombieSurvivalCard_GDD.md` §30.3、§30.7～30.10；`design/ux/hud.md`「輪末結算與士氣跨階呈現」及「動態效果與閃爍」。
- ADR Governing Implementation: `Docs/Architecture/adr-0003-morale-preview-and-warning.md`；沿用其 Runtime 權威、減少動態效果與輸入隔離原則。
- Parent Plan: `production/epics/PB-P0-014E-plan.md` E2。
- Dependencies: PB-P0-014E-E2-001；PB-P0-014E-E1 的士氣 HUD、金字塔與摘要區；Runtime 提供的正式交易前後狀態與實際效果替換結果。

## Scope

呈現正式士氣交易的數字變化及不可逆跨階結果，支援正常播放、逐步略過與減少動態效果，並在中央演出結束後留下可追溯摘要。

## Acceptance Criteria

- [ ] E2-002-01：士氣主數字由交易前值計數至已提交的最終值；跨越正式門檻時建立閱讀停頓，但 UI 中間值不提交回 Runtime。
- [ ] E2-002-02：跨階時中央金字塔取得焦點，舊層永久破損且保留輪廓；背景與其他 HUD 只適度壓暗，不建立額外確認流程。
- [ ] E2-002-03：畫面列出 Runtime 回報的實際舊效果 `已移除`、最終新效果 `已添加` 及 `本局不可恢復`；文字不得早於正式 Stage 提交出現。
- [ ] E2-002-04：單一交易一次跨兩階時，兩個已跨越層都永久破損；只移除真正的舊效果並加入最終 Stage 效果，不顯示中間 Stage 效果曾啟用。
- [ ] E2-002-05：士氣回升只更新數字；已失去階層、破損狀態及已移除效果均不修復或重新啟用。
- [ ] E2-002-06：中央呈現結束後回到常駐 HUD，通知／摘要保留本次效果移除、加入及不可恢復資訊，玩家仍能追溯結果。
- [ ] E2-002-07：輪末呈現期間，左鍵或 `Space` 只略過目前視覺步驟並立即補齊該步驟正確最終畫面；後續步驟需再次輸入，連按不會漏掉資料或重複交接。
- [ ] E2-002-08：略過最後步驟的輸入在事件、崩潰畫面或 Gameplay 接管前被消耗，不得同時選擇事件、關閉畫面、操作卡牌或推進流程。
- [ ] E2-002-09：減少動態效果以短暫淡入、狀態切換、淡出及較短數字計數取代移動、大幅縮放與震動；呈現順序、最終 Stage、永久破損與文字摘要不變。
- [ ] E2-002-10：正常播放、逐步略過、連續輸入與減少動態效果的最終士氣、Stage、效果及摘要完全一致；1920×1080、1280×720 與灰階下仍可讀。

## Implementation Notes

- 動畫只呈現正式結果，不自行判斷門檻或推導效果名稱。
- 略過應完成目前步驟的視覺狀態，再由 Story 001 的佇列決定下一步。
- 優先沿用 E1 金字塔、詳情與通知元件；新增素材引用須保留缺圖時可辨識的文字／形狀備援。
- 減少動態效果屬呈現設定，不能改變資料、流程鎖定或完成時序。

## Out of Scope

新 Profile 或新階段效果、士氣門檻與數值調整、崩潰報告內容、完整設定畫面、正式三角形美術資產製作及其他 HUD 動畫重製。

## Test Evidence Required

- Automated: 針對單階、一次跨兩階、回升、逐步略過、連按及完成狀態一致性的定向測試；實際路徑記入證據。
- Manual: SampleScene 於 1920×1080、1280×720、灰階及減少動態效果下完成 Play 檢查，記錄正常播放與略過結果。
- Evidence: `production/qa/evidence/pb-p0-014e-e2-evidence.md`，附關鍵狀態截圖或等效人工紀錄。
- 不以靜態階層或編譯成功冒充實際 Play 驗收。


## 2026-10-29 實作狀態

已完成 E2 程式與 GameplayHUD_P0_Integration 接線。55 項定向測試通過；人工輸入、指定解析度／灰階與 E3 端到端尚待驗收，故不將所有驗收框勾選。2026-10-30 已將測試副本驗證後的 E1／E2 接線整合至 SampleScene，無 Presenter 的相容路徑仍保留。

架構與腳本關聯圖：`Assets/Docs/Architecture/e2-value-transactions.md`。驗證證據：`Assets/production/qa/evidence/pb-p0-014e-e2-evidence.md`。
