# 技術債與待確認事項

更新：2026-10-31。只記問題與邊界，排序由 Story-Sequence.md 決定；記錄不代表開始實作。

## HUD-001 統一 HUD 更新與動畫責任

使用者已確認保留的技術債，尚未實作。

- GameplayHudRuntimePresenter、ResourceDebugView 與 SettlementPresenter／ResourceValuePresenter 依賴更新權旗標避免重複寫入，後續改為每個數值 UI 的單一固定寫入者。
- SettlementPresenter 聚焦排程、輪末／跨階順序與鎖定；數值元件集中顯示策略，避免 Energy 在 Present／PresentLive 兩處特判。
- 主數值插值與浮動數字漸出共用時間；評估分離及每種數值獨立時間／啟用設定。
- PresentLive 的 displayedTemporaryFood 是食物專屬參數，其他種類收到卻不用；一併整理兩條路徑中 Food／TemporaryFood 合併動畫的特殊處理。
- IValuePresentation 未涵蓋 PresentLive，Presenter 仍持有具體類型；Coordinator 與 Drain 重複描述步驟，需避免擴充後不同步。
- 保持權威邏輯與動畫分離、普通變更不鎖、任何時段跨階／輪末集中演出、失敗／Reset 守衛與去重。不能藉重構改玩法。

## 暫緩的極簡資源 UI

使用者選擇主數字顯示可用量，臨時食物下方黃色；不希望常駐很多保留文字。細節尚未全部確定且目前暫緩。保留使用者建立的黃色文字物件，不自動重排或接入。臨時食物不可支付一般成本，不能以 UI 排列暗示兩者通用。

## 待檢查風險

- 靜態事件在關閉 Domain Reload、重複 Play／換場景的生命週期。
- 長局 seenResults 成長、阻塞期間外部交易快照及動畫交接。
- ResetRequested 的例外派送與其他交易事件不一致，按需求評估。
- 成就／統計、動畫開始／完成／被取代事件為未來候選，不是已提供 API；交易完成和動畫完成不可混為一談。
- §26.11 疊加、拆裝、次序待定；傷口污染原卡去向、倉庫容量細節、終局數字仍需設計。不能從工作簿空白題目推定已決策。

## 本次文件遷移相容調整

EventScheduleSimulationUtility 的 ReportAssetPath 原指向舊 Assets/Docs；遷移後只將輸出位置改到 GameDevelopmentProcess/Playtest，避免工具重新生成舊文件入口。這是 Editor 報告路徑更新，不改遊戲行為，未重跑 30 天模擬。

## E3 與主選單邊界（2026-11-02）

重開清理與場景導覽屬 E3 的正式範圍，不延後為 HUD-001 技術債。Scene 卸載不自動清除 static System／訂閱／常駐物件；須實作並驗證明確生命周期。正式主選單 UI 由 PB-P3-005 承接，不能用它阻擋 E3 占位流程驗收。

