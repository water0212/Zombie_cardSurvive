> 2026-10-31 遷移：保留原 ADR 決策與狀態；現行玩法讀 ../GDD.md，工作順序讀 ../Story-Sequence.md。內文舊來源路徑可在 ../Abandoned-plan 的原目錄找到，不是另一套進度入口。

# ADR-0003：士氣已知狀態預測與警告呈現

## Status

Accepted — 2026-10-21。使用者已核准 PB-P0-014E-plan 的交付邊界、資訊揭露與呈現方式，並明確要求開始製作。本 ADR 僅記錄該已核准方案的分層，不授權新增玩法或變更數值；Accepted 不代表實作或驗收完成。

## Engine Compatibility

- Engine：Unity 2022.3.62f3；uGUI / TextMeshPro，沿用既有 Input System。
- Knowledge Risk：LOW；使用現有專案 API，不引入套件。
- References Consulted：RoundEndResolver、RunPhaseController、GameplayHudRuntimePresenter；PB-P0-014E-plan；GDD 第 30 章；HUD UX。
- Post-Cutoff APIs Used：None。
- Verification Required：唯讀性、預測／提交一致性、流程鎖定、Inspector 引用與雙解析度 Play。
- 未找到專案 control-manifest 或 technical-preferences；本 ADR 指南作為此切片分層規則，並保留此文件缺口，不補造全專案規範。

## ADR Dependencies

- Depends On：既有 PB-P0-014A～D Runtime；無需等待世界呈現遷移。
- Enables：PB-P0-014E-E1。
- ADR-0001 僅作既有 Canvas／世界邊界參考，本切片不核准或修改該 Proposed ADR。
- E2 完整交易呈現與 E3 開局入口在後續切片實作；不得因 E1 完成而宣稱整項完成。

## Context

士氣已具備權威結算，但玩家缺少已知後果預測。輪末可能揭露剩餘附加卡，不能在預覽提前執行或消耗亂數。HUD 不集中揭露永久上限損傷總量，但危險判斷不能忽略該損傷。

## Decision

權威系統提供唯讀快照，規則層以與實際結算共用的計算產生已知狀態預測，UI 只格式化與呈現。確認操作重新驗證最新狀態後，才呼叫既有合法推進入口。

## Implementation Guidelines

1. Runtime 是唯一權威。預測不得呼叫 Resolve、付款後還原、RevealRemainingAdditiveCardsForRound、排程效果、推進回合、觸發狀態通知或消耗亂數。
2. 抽取純計算共用點，避免 UI 複製公式。保留既有結算順序、門檻、食物保留規則、完整反應窗口及 Stage 生效時機；同一輸入且無新增未知結果時，預測須與實際提交一致。
3. 預測只讀目前已知狀態。未抽卡、未知事件及未決定隨機結果不計入，不承諾未來操作不會改變結果。
4. 規則層完整計算永久上限損傷與最終危險；有此風險時，士氣區隱藏精確預測終值／損傷總量，改顯示警告。個別殭屍詳情仍顯示其已知後果；無永久損傷時可顯示已知狀態 A → B。
5. UI 只送請求。一次完整交易後刷新，避免中間通知產生閃動的假警告；訂閱需對稱解除，重開局／停用清除暫存與確認狀態。
6. 結束本輪才要求嚴重危險確認。多種風險使用同一框，崩潰優先；取消不提交；確認需重算並拒絕過期內容直接放行。失敗、事件、探索及其他既有鎖定不可繞過。
7. 不以 Time.timeScale 取代流程鎖定。固定詳情與確認框的關閉輸入需被消耗，不得穿透到手牌、世界物件或下一個流程。
8. 短文字是主要語意，圖示／形狀提供備援。危險首次出現才短暫強調，持續刷新不反覆閃動；減少動態效果保留全部資訊。
9. Editor 工具須冪等，只補必要物件與引用，保留 SampleScene 手動排版；不得手寫 Unity 資產 YAML 或重建整個場景。
10. 低併發離線編譯及針對性測試；未連線的 Unity 場景套用、Console 與 Play 項目明確記錄未驗證，不執行完整高記憶體套件。

## Alternatives and Consequences

- 不採 UI 自行複製結算公式：容易隨數值修改失去一致性。
- 不採實際結算後回滾來預覽：通知、亂數、卡牌及排程不能安全回滾。
- 採唯讀快照與共用計算：可測試、可追溯；成本是需明確分離計算與副作用，並處理交易刷新與過期確認。

## Requirements and Validation

TR-MUI-001～006；驗收見 PB-P0-014E-E1。數值、唯讀性與流程測試通過後，仍須人工確認字型、版面、圖示、鎖定與互動。
