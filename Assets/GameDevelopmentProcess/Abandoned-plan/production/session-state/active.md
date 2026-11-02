# Active Design Session

- Task: 建築詳情與書籤式功能面板 — BUILDING-UI-001
- Status: Complete with Notes — lightweight close-out approved 2026-09-17
- Current section: BUILDING-UI-001 已結案
- Primary file: `production/epics/building-ui/BUILDING-UI-001.md`
- Related file: `Docs/Architecture/adr-0002-building-bookmark-presentation.md`
- Next: 尚未指定下一個 story；保留卡牌強化升級期規則差異與少數邊界 Play case 作後續考量
- Last updated: 2026-09-17

## Implementation preflight — 2026-09-17

- Task: 建築詳情與書籤式功能面板（使用者已核准完整實作計畫）
- Status: BLOCKED — dev-story prerequisite documentation
- Missing: Docs/Architecture/tr-registry.yaml; this feature's implementation story and governing ADR reference.
- Control manifest also absent; this is a warning, not independently a blocker.
- No implementation, scene or prefab changes made during this preflight.
- Unity MCP tools are not exposed in the current session; scene application and Play verification remain pending.
- User evidence: event and exploration operations were confirmed working before this feature request.
- Next: obtain direction to establish the minimum feature-specific prerequisite documents, then implement the approved plan without adding gameplay scope.

## Documentation supplement — 2026-09-17

- 使用者已授權補齊本次最小文件；已建立需求追蹤 TR-BUI-001–008、ADR-0002、BUILDING-UI-001（16 項驗收）與證據紀錄。
- 原先「檔案不存在」阻擋已解除。依 architecture-decision，新的 ADR 暫為 Proposed；尚未將實作狀態標為 Ready 或 Complete。
- 尚無全專案 control-manifest，這是非阻擋警告。本切片明確使用 ADR-0002 的 Implementation Guidelines；不擴大為全專案治理遷移。
- 現有 Docs/registry/architecture.yaml 已讀取並檢查相容；尚未新增 ADR-0002 登錄，等待明確同意寫入該 registry。
- 未修改程式、場景、Prefab 或遊戲數值；未執行編譯與 Play。

## Implementation resumed — 2026-09-17

- 使用者已明確接受 ADR-0002 並授權三項架構登錄；ADR 與 registry 已更新，不需再次確認。
- 前次代理與 MCP 程式執行因用量限制中斷，留下呈現配置與資訊頁基底；本次沿用並接續。
- 本次 MCP 可用：確認 SampleScene Edit Mode、初始 isDirty=false、Console 0 errors。
- 書籤／製作頁／Editor 工具依 dev-story 分工實作中。尚未完成驗收。

## Session Extract — /story-done 2026-09-17

- Verdict: COMPLETE WITH NOTES
- Story: `production/epics/building-ui/BUILDING-UI-001.md` — 建築詳情與書籤式功能面板
- Acceptance: 16/16；離線編譯 0 errors、Unity Console 0 errors、4 項針對性檢查通過、使用者小範圍 Play 通過。
- Advisory: 卡牌強化在建築升級期間仍受既有 `Active`-only 規則限制；少數低風險邊界未逐項 Play。
- Tech debt logged: 未另建 tech-debt 項目；備註保留於 story 與 QA evidence。
- Next recommended: None identified；由使用者選定下一項工作。

## PB-P0-014E-E1 Implementation Extract — 2026-10-22

- Task: 士氣資訊與已知輪末預警。
- Status: IN PROGRESS — runtime and automated checks complete; scene/manual verification pending.
- Story: `production/epics/PB-P0-014E-E1.md`。
- ADR: `Docs/Architecture/adr-0003-morale-preview-and-warning.md`（Accepted）。
- Implemented: 唯讀輪末預測、常駐士氣／階段／金字塔、分散預警、永久創傷遮蔽、個別殭屍後果、嚴重風險確認、跨 UI 互動鎖、可修復且保留排版的 Editor 套用工具。
- Validation: 離線編譯 0 errors；Unity 指定 EditMode 35/35 passed。擴大測試的既有 Tight plan 矛盾另記於 evidence。
- Evidence: `production/qa/evidence/pb-p0-014e-e1-evidence.md`。
- Pending: 對目標 Gameplay HUD 場景套用工具；1280×720／1920×1080、灰階、減少動態、輸入不穿透及事件／探索／建築小範圍 Play。
- E2（跨階／崩潰呈現）與 E3（Profile 開局入口）尚未開始。

## Session Extract — /story-done 2026-10-25

- Verdict: COMPLETE
- Story: `production/epics/PB-P0-014E-E1.md` — 士氣資訊與已知輪末預警
- Acceptance: 9/9；Unity 指定回歸通過、場景驗證 0 issues、MCP 畫面證據完成，使用者 Play 驗收通過。
- Deviations: None；正式三角形素材替換保留給後續資產製作。
- Tech debt logged: None。
- Next recommended: PB-P0-014E-E2（跨階／崩潰呈現）或 PB-P0-014E-E3（Profile 開局入口），開始前先建立／確認對應 story。


## 2026-10-28：卡種升級與欄位強化決策

> 此遊戲玩法應被捨棄，針對個別卡牌的強化效果應與"欄位"綁定，而非個別卡牌之上。

拒絕將單張卡牌實例強化或單卡改造收錄為正式玩法；相關舊說法僅能作為已捨棄歷史。保留卡種全局升級與欄位強化，詳細規則待討論。換牌後個別強化效果留在欄位，不隨卡牌進入倉庫。此決策只修訂文件，不代表欄位系統已實作，也不取消卡牌實例 ID、剩餘使用次數或施工關聯。

## 2026-10-28：卡種升級與欄位強化細化

正式規則與待定細節統一參照 `Docs/GameDesign/ZombieSurvivalCard_GDD.md` 第 26.11 節。

- 卡種升級只由建築升級預設卡／建築產出卡；探索、事件取得的同種可升級卡也適用。
- 僅在輪間同步替換倉庫與正規欄位中的對應卡，不設計輪內手牌／抽牌／棄牌即時轉換；新卡模板即基礎值。
- 繼承舊卡剩餘使用次數；無限次變有限次是應避免的錯誤配置，給 1 次使用權利並回報錯誤。
- 已解鎖卡種升級不因來源建築毀損或停用撤回。
- 取得強化後自選合法欄位安裝，事件給予也永久保留；本決策不自行授權跨局繼承。
- 傾向可疊加但尚未斷定，不採用先前建議的「一欄一個」作為已確認限制。
- 食物收益強化限裝食物欄位，合法放入的特殊卡同樣增加食物收益，其他收益不變。
- 草圖：單一欄位圖示在左上方，懸停顯示詳情；整組效果在群組背景最左側排成一行。
- 單卡實例強化仍屬已捨棄玩法；本次僅修訂文件，未修改程式或驗收狀態。

## 2026-10-28：欄位收益與升級次數補充確認

已同步 GDD 第 26.11 節，以下不再列為待定：

- 原卡沒有食物收益時，食物收益強化不生效，不新增食物效果。
- 食物收益 +1 每次成功打出只計一次，不因同一卡有多筆食物效果而重複增加；多個強化的疊加規則仍待定。
- 有限次升級為無限次時直接變為無限次；有限次升級後若舊剩餘次數超過新卡上限，則限制為新卡合法最大值，其餘繼承剩餘次數。
- 無限次升級為有限次仍沿用給 1 次使用權利並回報錯誤的既定例外，不因本次補充取消。
- 僅更新設計與交接文件，未修改程式、資產或驗收狀態。

## 2026-10-29：明確交易上下文與共用數值動畫（使用者確認）

採用明確交易上下文方案，不以呼叫者身分、同一幀或時間間隔猜測交易邊界。接口與類別名稱仍為規劃，未實作。

- 一次完整出牌、事件選項或輪末由外部權威流程建立交易上下文；該操作的各次資源修改傳遞同一上下文。相同卡牌下一次打出是不同交易。
- 共用數值介面可提供讀取、Set、Change 與變更通知；各系統保留付款、保留量、士氣批次等專用規則。共用介面不代表所有變更必須採同一修改邏輯。
- StateChanged 保留於底層變更通知，不作為本次共用資源數字／增減動畫的觸發入口。需要逐筆追溯的變更結果應包含交易識別、數值識別、來源、前後值與實際變化；成就及統計本體不納入 E2。
- TransactionCompleted 由外部交易擁有者在完整操作結束後發布一次，包含逐項變更與前後快照／實際淨變化，交給資源 UI。Change 不自行宣告整筆交易完成。
- UI 初次綁定、重設或重新開啟直接同步目前快照，不假裝取得資源。卡牌、事件與輪末共用同一資源動畫接口，輪末協調器只安排播放節奏、略過與單次交接。
- 例：支付 2 食物並取得 2 食物，底層保留兩次有效變更，完整結果淨變化 0，UI 不播放淨增減動畫；兩者仍由同一交易識別串起。
- 快照與結果逐筆建立，不使用一份會被下一筆覆蓋的靜態快照；動畫不負責結算或回滾。交易完成通知與演出完成訊號是不同事件。
- 士氣維持完整批次提交及立即 Failed 守衛；交易上下文只串接來源與結果，不將批次拆成逐項不可逆判定，也不延後權威失敗。
- 現有 static Systems 若接入介面，優先以不另存權威數值的轉接物件逐步整合；不在 E2 無差別重寫建築、探索等整個系統。
- 實作需針對重複 Complete、失敗／中斷收尾、巢狀操作邊界及舊局回呼補測；具體策略在實作時定案，不能以本決策宣稱已具備交易回滾或全系統統計功能。

本段更新設計與實作規劃，不修改玩法數值，不代表 E2 已實作或驗收通過。

## 2026-10-30：Sample 主場景 E1／E2 整合

已另存 Assets/Scenes/Sample_E1E2_IntegrationTest.unity；30 項定向 EditMode 測試通過，副本七組 Play 接線檢查通過後，已整合回 SampleScene。原主場景 GUID 與真實手牌／事件／建築／探索控制器保留。修正舊數字輪詢覆寫 E2、資源文字 Awake 順序透明，以及固定像素裁切控制區問題。

主場景最後一次 Play 復測因 Unity 崩潰而中斷；重開後磁碟／引用驗證通過。使用者要求不再追查該錯誤，停止自動 Play。不可將中斷的主場景復測或實體輸入／解析度／E3 端到端記為通過。

架構：Assets/Docs/Architecture/sample-e1-e2-integration.md。
證據：Assets/production/qa/evidence/pb-p0-014e-sample-integration-evidence.md。
修改前備份：C:/Users/USER/Documents/ChatGPT/遊戲開發/e2-integration-before/。
## 2026-10-30：一般數值動畫不再鎖定操作

依使用者確認修改：普通卡牌／事件／System 只播放局部數值動畫；士氣跨階（任何時段）與固定輪末立即使用原阻塞演出，歸零保留 Failed 接管。新 LiveValueAnimation 支援連續交易重新定向與淨增減合併；阻塞接管、Reset、停用均防止舊動畫覆寫。

49 項定向 EditMode 測試通過（新分流 23、交易 17、E1 預警 9），未自動執行 Play。三份場景不需重接。SampleE1E2PlayProbe 已改為檢查普通牌不鎖定、跨階／失敗才鎖定。

目前規則與證據：Assets/Docs/Architecture/e2-responsive-value-presentation.md。
修改前腳本備份：C:/Users/USER/Documents/ChatGPT/遊戲開發/e2-responsive-before/。
## 2026-10-30：技術債 HUD-001 — 統一 HUD 數值更新與動畫責任

狀態：已確認、待重構。使用者要求記錄為目前留下的技術債；本次不實作重構。

問題：GameplayHudRuntimePresenter、ResourceDebugView 與新增的 SettlementPresenter／ResourceValuePresenter 之間，仍以更新權接管旗標避免重複寫入數值文字。SettlementPresenter 同時承擔一般數值更新、動畫分流、集中演出及操作鎖，後續容易增加耦合。LiveValueAnimation 目前共用時間與進度，主數值插值和浮動增減文字漸出亦相互綁定。

後續重構方向（具體接口待設計）：
- 每個數值 UI 只有一個固定寫入者；移除舊 HUD／Debug 元件重複刷新及不必要的接管旗標，不再疊加另一套 HUD。
- 通用數值呈現元件集中管理立即更新／插值、浮動增減文字與樣式設定，避免針對 Energy 等卡死特殊判斷，避免在一般與集中演出重複實作顯示策略。
- SettlementPresenter 聚焦演出排程、輪末／跨階順序與操作鎖；輪內和輪末共用數值呈現能力。
- 評估分離主數值插值時間與浮動文字漸出時間，以及每個數值獨立配置的需求，不將討論中的方案當成已實作。

需保留：交易與真實數值不依賴動畫成功；普通數值改變不鎖操作，任何時段士氣跨階與固定輪末集中演出，崩潰／重設守衛維持。能量立即顯示最終值、其他資源保留動畫是使用者已表達的需求，實作狀態需另查，不以本紀錄宣稱完成。臨時食物／保留量 UI 改版目前暫緩，使用者已建立黃色文字物件，後續不得覆蓋其手動調整。

### HUD-001 補充：PresentLive 的食物專屬參數

使用者要求記錄：ResourceValuePresenter.PresentLive(displayedValue, displayedTemporaryFood, ...) 目前每個數值元件都會收到 displayedTemporaryFood，但只有 Food 元件使用它，其他資源忽略。原因是目前 Food UI 同時呈現永久與臨時食物，因此通用呈現介面洩漏食物專屬需求。

後續重構需檢討此接口，讓通用數值呈現不必接收不相關的臨時食物參數，並一併整理 SettlementPresenter.RenderLiveFrame 與 ResourceValuePresenter.Present 中合併 Food／TemporaryFood 變化量的特殊處理。具體採獨立數值元件或組合顯示層待設計，不在本次實作；須保留既有交易結果與動畫分流規則。

## 2026-10-30：E2 API 文件與後續交付慣例

已依目前磁碟程式整理 Assets/Docs/ScriptCodeReview/E2_API_Review.md，包含交易／轉接／演出 API、關係圖、Sample 接線、使用者能量立即更新及樣式手改、問題／驗證界線與 HUD-001。最新手改沒有重跑測試；使用者僅確認能量 PresentLive，Present 尚無實際觀察情境。

使用者要求後續每個大進度完成時製作／更新 API 文件，放在 Assets/Docs/ScriptCodeReview；應包含公開 API 與事件、腳本關係、呼叫方式、已遇問題、驗證範圍、技術債及未來擴充。不能將提案或歷史測試當成目前完成驗收。
