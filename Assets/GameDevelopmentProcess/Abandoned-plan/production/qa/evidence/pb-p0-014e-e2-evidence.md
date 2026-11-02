# PB-P0-014E-E2 驗證紀錄

日期：2026-10-29。Unity 2022.3.62f3。透過已開啟的本機 Unity MCP 執行。

狀態：實作完成、定向自動測試通過；人工輸入／視覺矩陣與 E3 端到端仍待驗收。未執行完整測試套件。

## 實作範圍

- 明確 ValueTransaction、獨立快照、逐項實際變更與完成通知；static Systems 使用無重複數值的 GameValues 轉接器。
- 卡牌／事件明確 Context、輪末同步呼叫範圍相容橋接；士氣維持原批次規則與立即 Failed。
- 共用數字／淨增減、士氣計數、單階／兩階永久破損、效果替換摘要、略過與減少動態。
- 完整多來源崩潰報告；無 E3 合法出口時隱藏重新開始／離開按鈕。
- pending transaction ID 單次交接、缺少／停用 Presenter 收尾、舊局取消與動畫例外收尾。
- GameplayHUD_P0_Integration 場景已接線並儲存；P0 初始化使用 Reset 同步，不播放假的取得／跨階演出。

架構與完整關聯圖：`Assets/Docs/Architecture/e2-value-transactions.md`。

## 自動測試

Unity MCP job：`38039ca725514aad8651e86ed6637800`。
**55 項執行、55 通過、0 失敗、0 跳過**；執行時間 1.0774849 秒（Unity 回報）。同目錄 `pb-p0-014e-e2-tests.json` 保存工具原始結果。

只指定以下測試類別：

- `ValueTransactionE2Tests`：17 項，包括同筆扣 2 得 2 的兩筆日誌及零淨變化、重複 Complete、獨立快照、付款失敗、保留量、巢狀操作、中斷不回滾、多批次來源、不可逆階段、過期步驟完成、無 Presenter、三種來源立即 Failed、實際 CardController 出牌／EventChoice／RoundEndResolver 路徑及單次重開請求。
- `RoundEndPreviewTests`：E1 無副作用預測／預警回歸。
- `MoraleSettlementTests`：士氣淨值批次與輪末規則回歸。
- `EventPaymentBoundaryTests`：事件付款、強制損失與不足後果回歸。
- `EventResultPresentationTests`：既有事件結果呈現回歸。

回歸期間發現一項舊測試仍預期選擇「正規化後等於目前計畫」回傳 true；E1 既有實作已將其視為 false 的無變更請求。本次只修正該測試預期，未修改 FoodPlanSystem 生產規則。

最後一次上述測試後，另完成臨時食物合計顯示與 P0 初始化的呈現修正；已重新經 Unity 編譯、Console 與 Play 檢查。沒有將未重跑的全套測試宣稱為已通過。

## Play／MCP 檢查

在 GameplayHUD_P0_Integration：

- 正常跨兩階：80 → 25，Stage 0 → 2；畫面顯示兩層輪廓／裂痕及「已損毀」，只列 Stage 0 舊效果和 Stage 2 最終效果，常駐摘要保留。
- 完整崩潰：來源 FoodShortage -70 與 ZombieLeak -15／上限 -5；提交當下立即讀到 Failed 與操作鎖。報告讀到實際 25 → 0、上限 100 → 95，並保留兩項要求變化。
- 失敗後呼叫 EnterEventPhase、EnterTurnPhase、EndTurn，仍維持 Failed 與鎖定。
- 減少動態 + 連續 SkipCurrentStep API：最終 `25|Brink|False`（士氣／Stage／操作鎖），效果摘要與正常呈現相同。這是 API 級測試，不冒充實體左鍵／Space 驗收。
- 有 Presenter 的輪末：提交時 RoundEnd／pending=true／locked=true；嘗試 EnterTurnPhase 不能提前放行。重開 Play 再驗，完成時 Event／pending=false／locked=false、佇列空且步驟 Complete。
- 工具驅動檢查曾在等待期間仍讀到 pending；追查發現背景 Play 停在 frame=2，並非協程已持續執行卻不交接。本次 Play 暫時開啟背景執行並請求 Editor PlayerLoop 推進後，frame 增長至 5654、佇列完成，未改動 ProjectSettings；人工連續輪末仍列待驗收。
- 截圖檢查發現初版常駐摘要遮住士氣 HUD，已移到右側空白區。

在 SampleScene：

- 現場查得 GameplayHudRuntimePresenter=0、MoraleHudPresenter=0，因此沒有新增或重建其舊 HUD。
- 無 Presenter 的輪末實際 Play 回傳 Event／pending=false／failed=false，沒有卡死；讀取 Console 為 0 個 error。
- 不宣稱 SampleScene 已具備新版 E2 演出，也不宣稱 P0 切片已涵蓋完整探索／事件玩法。

本次截圖為當時 Unity Game View 約 1102×541；它不是指定解析度矩陣的證據。

- `pb-p0-014e-e2-stage-play.png`：中央兩層破損畫面；當時摘要還在頂部，後續已修正位置。
- `pb-p0-014e-e2-summary-final.png`：修正後右側常駐摘要。
- `pb-p0-014e-e2-collapse-final.png`：最終完整崩潰報告。
- 最終 P0 初始 Play 查詢為 presenting=false、summary 空字串、locked=false；初始化沒有假演出。最終報告查詢 visible=true，Console error=0。

## 仍需人工驗收

- 實體左鍵／Space 逐步略過、長按／連按後無輸入穿透；常規多輪與各種事件 UI 接管。
- 1920×1080、1280×720、灰階與減少動態的閱讀驗收。
- 真實遊戲操作途徑的卡牌、建築、殭屍、時段控制在呈現／Failed 期間均不可互動。
- Presenter 在不同視覺步驟被停用、場景切換時的實際畫面收尾。
- E3 Profile 選擇、完整新局清理與離開本局的端到端流程；目前只驗證請求與合法入口契約。

故事驗收框保留未全勾選，避免用編譯、API 測試或靜態圖代替人工驗收。
