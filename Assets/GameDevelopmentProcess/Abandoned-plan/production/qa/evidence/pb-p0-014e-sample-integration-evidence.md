# PB-P0-014E：SampleScene E1／E2 接線證據

日期：2026-10-30。Unity 2022.3.62f3。

## 交付結果

- 已另存 `Assets/Scenes/Sample_E1E2_IntegrationTest.unity`。
- 驗證副本後，已將同一份接線整合到 `Assets/Scenes/SampleScene.unity`。
- 主場景 GUID 保留 `8c9cfa26abfee488c85f1582747f6a02`，`.meta` 雜湊與修改前一致。
- 交付後兩份 `.unity` 檔案內容雜湊相同。
- 保留唯一 CardController、RunPhaseController、CardHandView、RoundEndResolver、EventFlowController、BuildingSystem、ExplorationSystem、EventSystem。
- GameplayHudRuntimePresenter、MoraleHudPresenter、SettlementPresenter 各一個，ResourceValuePresenter 四個；沒有 P0GameplayHudSliceController。
- 真實 CardHandView 的卡牌 Prefab、handContainer、playZone、playEffectController 引用完整。

## 定向 EditMode 測試

Job：`967d923e50784bf59809cbace92e9e7e`。

30 執行、30 通過、0 失敗、0 跳過。Unity 回報時間 0.800414 秒。

- `SampleE1E2IntegrationTests`：4 項。
- `ValueTransactionE2Tests`：17 項。
- `RoundEndPreviewTests`：9 項。

本次新增測試涵蓋：

1. 舊 ResourceDebugView 不得蓋過呈現層持有的數字；解除持有後恢復原刷新能力。
2. ResourceValuePresenter 在 Awake 前被同步時，仍保存並顯示原先可見的文字顏色。
3. 儲存的測試場景保留真正手牌及完整 E1／E2 引用。
4. 重複套用整合工具不會增加重複控制器或資源呈現元件。

原始工具回覆：`pb-p0-014e-sample-integration-tests.json`。

最後調整士氣面板位置後，再次執行了測試副本 Play 驗證；不將單一版面位置調整描述成又執行一遍所有單元測試。

## 測試副本 Play：通過

Editor 專用 `SampleE1E2PlayProbe` 使用場景既有控制器及實際 UI 元件，記錄七組成功條件：

1. 控制器唯一，保留 CardHandView，E1／E2 引用正確，沒有示範初始化。
2. 真實事件選項／結果完成後，經 EndEvent 正常進入 Turn。
3. 真實 CardView → CardHandView → CardController 出牌；一張牌產生一份交易結果，各數值淨變化符合實際系統狀態，最終 UI 顯示正確。
4. 舊數值輪詢不會提前覆寫動畫數字；出牌呈現結束後可解除操作鎖。
5. 透過 EndTurn 開啟 E1 風險確認，實際 Return 按鈕取消時不提交結算。
6. 實際 Continue 按鈕經 RoundEndResolver 結算；呈現期間維持 RoundEnd 與鎖定，公開 Turn 入口不能繞過；完成後只交接到下一個事件。
7. 多來源崩潰立即 Failed，不能再推進階段，完整顯示食物短缺／殭屍漏防兩項原因並維持失敗鎖定。

原始工具回覆：`pb-p0-014e-sample-integration-play.json`。

## 畫面與重新載入

- 實際查看較小 Game 視窗時發現原主 Canvas 為 ConstantPixelSize；已改為 1920×1080 參考尺寸的 Expand 縮放，能量、手牌及 EndTurn 控制不再被裁切。
- 資源文字曾因 Awake／OnEnable 順序變成透明；已加入重現測試並修正。重新載入後食物、資源、戰鬥文字 alpha 均為 1。
- 士氣面板移到資源列旁的上方空白區。
- 重新載入 SampleScene 後：Edit 模式、scene dirty=false、引用驗證 errors=[]、Console error=0。

## 主場景復測限制

套用後再次啟動主場景 Play 時發生 Unity 崩潰，因此最後一輪主場景 Play 復測未完成，不能記為通過。

重開後已確認磁碟沒有回滾，兩份場景內容一致，原 GUID 與修正腳本仍在。依使用者要求停止追查該錯誤，沒有再次自動進入 Play。崩潰調查不列入本次場景整合的完成條件。

本次不宣稱實體輸入、所有解析度／灰階、完整探索內容或 E3 重開流程已驗收通過。

## 備份

本機修改前備份：`C:/Users/USER/Documents/ChatGPT/遊戲開發/e2-integration-before/`。

包含原 SampleScene 與 meta、原 ResourceDebugView、原 ResourceValuePresenter。未執行 Git 重設或清除其他未提交工作。
