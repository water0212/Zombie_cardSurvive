# PB-P0-014E-E1：士氣資訊與已知輪末預警驗證紀錄

- Date: 2026-10-25
- Story: `production/epics/PB-P0-014E-E1.md`
- Overall: PASS — AUTOMATED / MCP VISUAL / USER PLAY SIGN-OFF
- Engine: Unity 2022.3.62f3

## 已完成實作

- Runtime 提供零提交的已知輪末預測，涵蓋食物、保留與臨時食物、鼓舞餐、資源損失、戰鬥衰減、殭屍、永久士氣上限損傷、自然回落與階段結果。
- 預測與正式結算共用士氣、鼓舞餐、資源損失及戰鬥衰減計算點。
- 常駐士氣／階段／三層金字塔、懸停與右鍵固定詳情、分散預警、首次危險短暫強調及減少動態模式已實作。
- 永久創傷存在時，士氣警告隱藏精確總量與精確終值；個別殭屍詳情仍顯示其已知後果。
- 嚴重輪末風險使用單一確認框；取消不提交，確認前重新擷取快照，確認期間鎖住手牌、世界、殭屍、建築、食物方案與快捷鍵入口。
- Editor 工具可補建缺少的物件與引用；重跑時保留既有根節點排版，不重建場景且不自動儲存。

## 自動與靜態驗證

- 離線編譯：0 errors。警告來自既有 Unity 序列化欄位提示與離線組件版本衝突。
- Unity 指定 EditMode 回歸：35 passed / 0 failed / 0 skipped。
- 2026-10-25 修改後指定回歸：11 passed / 0 failed / 0 skipped（`RoundEndPreviewTests`、`MoraleHudE1SetupUtilityTests`）。
- 覆蓋類別：
  - `RoundEndPreviewTests`
  - `MoraleHudE1SetupUtilityTests`
  - `NextRoundEffectTests`
  - `MoraleCollapseCoreTests`
  - `ZombieThreatDetailViewTests`
  - `GameplayHudRuntimePresenterTests`
- 已驗證的關鍵邊界：重複預測不通知也不改變保留量／排程；短缺；臨時與保留食物；鼓舞餐成功與失敗原因；戰鬥超量衰減；標記擊殺；永久創傷；延後殭屍反應窗口；精確終值遮蔽；Editor 工具重跑修復與排版保留；不完整確認視窗不宣告可用。
- 額外擴大測試曾執行 43 項，結果 42 通過；唯一失敗為既有 `LegacyTightPlan_NormalizesToStandardSupply`，其測試預期同方案正規化操作回傳 true，但目前既有正式程式回傳 false。PB-P0-014E 未修改該判斷，故列為既有測試／行為矛盾，不計入本切片通過數。

## 程式審查

- ADR-0003：COMPLIANT。
- 審查中修正：完整快照等價比較、Controller 銷毀釋鎖、食物方案鎖定、Presenter 停用清理、不完整 Modal 防隱形鎖、Editor 工具局部修復與排版保留。
- Verdict: APPROVED；2026-10-25 使用者 Play 驗收完成。

## 2026-10-25 Unity MCP 畫面驗證

- 已在 `GameplayHUD_P0_Integration` 套用工具並儲存場景；Console 為 0 errors / 0 warnings。
- 舊 `Morale` 與 `Threshold` 已停用；`PB_P0_014E_E1_MoraleHUD` 已移入既有 `MoralePanel`，不再重複呈現兩套士氣資訊。
- 以 1280×720 Game View 驗證右鍵詳情會依指標位置移動，並在畫面邊緣自動限制範圍。
- 修正懸停詳情閃退：面板改為完整放在指標旁，且背景不再攔截 UI raycast；播放模式驗證面板持續啟用且 `raycastTarget=false`。
- 以 QA 狀態將永久食物設為 0，已顯示本輪短缺；食物、資源與戰鬥提示改為上下錯開，避免同時出現時互相遮蓋。
- QA 狀態已加入 Unity 上方選單：`Tools > Zombie Card Survive > Gameplay HUD > QA`，包含食物短缺、跨階段風險與還原展示數值。
- 已顯示士氣跨階段危險確認框，包含風險文字、返回處理與仍然結束本輪兩個選項。
- 畫面證據：`Screenshots/PB_P0_014E_E1_pointer_detail_left.png`、`Screenshots/PB_P0_014E_E1_staggered_warnings.png`、`Screenshots/PB_P0_014E_E1_danger_confirmation_visual.png`。

## 使用者驗收

- 2026-10-25：使用者確認詳情面板與食物短缺設定皆能正常顯示，並宣告驗收完畢。
- PB-P0-014E-E1 可標記 Complete。E2 與 E3 仍屬後續切片，本次結案不代表 PB-P0-014E 全部完成。
