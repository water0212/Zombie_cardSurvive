# Zombie_cardSurvive Codex Handoff

更新日期：2026-09-06  
工作目錄：`C:\Users\water\Desktop\GameDev\Zombie_cardSurvive\Assets`

這份文件用來讓新的 Codex 工作階段快速接上目前專案脈絡，特別是 Unity MCP 重新連線後的新工作階段。請先讀完本文件，再讀 GDD / Backlog / 相關程式碼。

## 1. 新工作階段建議開場

可以直接對新的 Codex task 說：

```text
請先讀取 Assets/Docs/CodexHandoff/current-context.md，沿用舊 Zombie_cardSurvive 專案脈絡，接著使用 Unity MCP 檢查目前 SampleScene、Console 與 ZombieThreatViewController 排版設定。
```

若要繼續程式實作，可以改成：

```text
請先讀取 Assets/Docs/CodexHandoff/current-context.md，沿用舊 Zombie_cardSurvive 專案脈絡。接著根據 Backlog 規劃下一個故事，先不要實作，等我確認。
```

## 2. Unity MCP 狀態

- 使用者已安裝 `unity-mcp-10.1.2.mcpb`。
- Unity 內目前可看到 `Window -> MCP for Unity`。
- Unity MCP 視窗顯示：
  - Version: `v10.1.2`
  - Transport: `HTTPLocal`
  - HTTP URL: `http://127.0.0.1:8080`
  - Local Server: Running
  - Session Active: `Zombie_cardSurvive`
- `C:\Users\water\.codex\config.toml` 已設定：

```toml
[mcp_servers.unityMCP]
url = "http://127.0.0.1:8080/mcp"
```

- 2026-08-22 的 Codex 內建 MCP 客戶端曾因初始化封包缺少協定標頭而握手失敗；Unity Server 本身可正常回應標準 Streamable HTTP MCP，並已用相同協定完成資產匯入、Console 與 Edit Mode 驗證。新 task 仍應先嘗試一般 Unity MCP 工具。
- 簡單 HTTP GET 到 `/mcp` 回傳 `406` 屬於正常訊號之一，代表 server 有回應，但不是給瀏覽器 GET 直接使用。

## 3. 專案定位

`Zombie_cardSurvive` 是一款基地指揮者視角的末日生存卡牌 Roguelite。

玩家不是親自外出的單一倖存者，而是基地指揮者。核心體驗是透過卡組、建築、探索、事件與資源調度，維持倖存者群體生存，並逐步打造能穩定產出資源的基地引擎。

設計期望分為三段：

- 前期：資源有限、環境惡劣、有威脅。玩家的心情是「我要活下去」。
- 中期：基地開始穩定，玩家思考「我要如何繼續活下去，而且活得好」。
- 後期：資源富足後，玩家追求強卡、欄位強化、調度工具、永久能力與近似無限流的構築引擎。

## 4. 主要文件

重要文件：

- `Docs/GameDesign/ZombieSurvivalCard_GDD.md`
- `Docs/GameDesign/ZombieSurvivalCard_Scrum_Backlog.md`
- `outputs/card_design_template/zombie_card_design_template_v1_clean.xlsx`
- `outputs/region_link_setup/zombie_card_design_template_v1_linked.xlsx`

目前 GDD 已記錄：

- 探索與卡包 V1
- 建築、施工與製作 V1
- 臨時食物 V1
- 事件規模、倖存者、能量與威脅邊界
- 有限地區資源與探索 Demo 邊界
- 特殊終局概念
- 欄位強化概念
- 士氣簡化方向

## 5. 已實作的大系統

### 卡牌與牌組

- `CardBase` 及多種卡牌資料。
- 正規牌組合法性與替換流程。
- 卡牌倉庫 `CardInventory`。
- 抽牌、棄牌、耗盡、打出流程。
- 卡牌使用次數。
- 探索卡不會免費補回，但卡組正規性仍靠替換流程維持。
- 打牌後 UI 替身卡特效流程 `CardPlayEffectController`。
- 通用選牌 API：
  - `CardSelectionRequest`
  - `ChooseValidPileCards`
  - 調度工具使用的抽牌堆選卡流程。

### 探索與卡包

- 探索卡打出後開啟地區三選一。
- 選擇地區後取得卡包。
- 正常卡進倉庫。
- 殭屍卡可進棄牌堆，之後抽到時生成威脅。
- 已有地區資料模型、卡包資料模型、風險資料模型。
- 已開始導入有限地區資源：
  - 地區卡包剩餘數量
  - 剩餘殭屍數量
  - 探索熱度
  - 搜索進度
  - 清理度

### 建築、施工與製作

- 建築以 `BuildBaseData` 資料驅動。
- Runtime 生成建築，不生成鎖定建築。
- 等級 0 是空地，施工後變施工地，完成後成為等級 1。
- 施工卡會進倉庫，透過事件後一般替換流程進入牌組。
- 打出施工卡會累積施工進度。
- 完成或取消施工時，相關施工卡應從倉庫、手牌、抽牌堆、棄牌堆、已打出區、耗盡區移除。
- 製作列擁有完整製造力、獨立佇列與容量。
- 製作工作保留成本，完成時提交成本並發放產物。
- 未完成工作跨輪保留。

已做過的建築方向：

- 工作台：可製作探索卡。
- 廚房：製作烹飪卡。
- 倉庫：預計提高永久食物儲存上限。
- 情報中心：預計給予替換機會卡、食物計畫設定、卡牌強化。
- 防禦工事：預計給予與強化戰鬥卡。

### 臨時食物

- `FoodSystem` 有永久食物與臨時食物。
- 臨時食物只可支付輪末基地固定食物消耗。
- 事件、任務、卡牌成本、建築成本、製作成本都不能使用臨時食物。
- 輪末優先消耗臨時食物，再消耗未保留永久食物。
- 輪末結算後剩餘臨時食物清零。
- 烹飪卡方向：消耗 2 永久食物，獲得 6 臨時食物，可使用 3 次。

### 事件系統

- 目前方向是每天一個事件，事件內有選項，不再是事件卡三選一。
- 事件有情緒分類：
  - 正面
  - 負面
  - 混合
- 事件有內容分類：
  - 資源
  - 倖存者
  - 效果
  - 殭屍
  - 劇情
- 已實作事件排程、分類、條件、縮放、支付邊界與 Forced Loss 後果。
- 已實作事件旗標 / 條件的簡化框架，用來處理「某事件已出現過」、「某 flag 是否觸發」等需求。
- 事件可以設定：
  - ImplementationStatus
  - ImpactLevel
  - SpecialRules
  - ImplementationNotes
- 尚未支援的事件可建立 Asset，但不進正式池。

重要設計：

```text
TimeTier = 1 + floor((Day - 1) / 10)

DevelopmentPoints
= 初始以外的倖存者數
+ 已啟用建築等級總和
+ 永久欄位強化數
+ 其他永久發展修正

CurrentSettlementTier = 1 + floor(DevelopmentPoints / 3)
PeakSettlementTier = 本局曾達到的最高 SettlementTier
EventMagnitude = max(TimeTier, PeakSettlementTier)
```

事件數值可依效果分別選擇縮放來源：

- Fixed
- TimeTier
- SettlementTier
- EventMagnitude
- SurvivorCount
- MaxEnergy
- 其他後續資料源

### 威脅與殭屍

已形成威脅分層：

- `WorldThreat`：全球惡化程度，影響殭屍基礎種類與體質。
- `BaseAttention`：基地噪音、基地事件、隱蔽修正，影響基地屍群。
- `RegionPressure`：地區基礎活躍度、探索熱度、清理度，影響當地遭遇率與地區事件。
- `HordeBudget`：把威脅換算成一次遭遇的總預算，殭屍數量與體質共同分配。

PB-P2-009A/B 已完成：

- `HordeBudgetRuntime`
- `HordeCompositionRuntime`
- `HordeProfileData`
- 相關資料型態與 Edit Mode 測試

PB-P2-009C 已完成（2026-08-17）：

- 新增事件效果 `Threat / SpawnHorde`。
- 事件顯示時鎖定 `ThreatSnapshot`、`HordeBudgetSnapshot` 與 `HordeCompositionResult`。
- 玩家確認選項後才把屍群批次加入 `CombatSystem`，並只通知 UI 一次。
- 無效 Profile、無合格殭屍或預算不足會阻擋選項，不扣成本、不清 Flag。
- Unity Edit Mode 新增測試 `10/10`，全套回歸 `128/128` 通過。

PB-P2-009D 已完成（2026-08-18）：

- 建立 3 個正式殭屍 Asset：`ZMB-001 遊蕩殭屍`、`ZMB-002 毀壞殭屍`、`ZMB-003 狂暴殭屍`。
- 建立 `HORDE-STANDARD-01 標準基地屍群`，預算曲線為基礎 3、每 Tier +2、最大 10。
- `EVT-008` 三個選項都會設定 `FLAG_HORDE_PENDING`；`EVT-014` 成功生成正式屍群後才清除 Flag。
- Event Content V1 正式事件池目前包含 `EVT-003`、`EVT-008`、`EVT-014`。
- 009D 與既有事件／屍群目標測試 `25/25`、Edit Mode 全套 `133/133` 通過，Console 0 errors。
- SampleScene 以 Tier 5 最大 8 隻殭屍驗證：7 隻第一排、1 隻第二排，彼此不重疊並顯示於建築前方。
- 三種正式殭屍目前共用既有殭屍美術，獨立美術仍是後續內容工作。

PB-P0-014A/B 已完成（2026-08-21～22）：

- 士氣以 100 / 100 開局，具有 70、35 兩條不可逆階段門檻與士氣歸零的一次性基地崩潰。
- 輪末、事件選項、卡牌效果與探索結果會先合併同一交易的士氣來源，再提交一次門檻與崩潰判定。
- 食物短缺為每缺 1 扣 3 士氣，短缺來源單輪最多扣 15。
- 食物計畫只保留標準供應與鼓舞餐點；舊緊縮值轉成標準供應。
- 鼓舞餐點總需求為 `ceil(D × 1.2) + MaxEnergy`，額外部分只能用未保留永久食物，成功恢復 5 士氣。
- 高士氣自然回落每輪最多 3，最低停在 70。
- PB-P0-014B 新增測試 11 / 11、Edit Mode 全套 161 / 161 通過，Console 0 errors。
- 2026-08-22 後續將鼓舞／自然回落調整為 `+5 / -3`，設定遷移升為 V2；新增回歸測試已通過編譯，待 MCP 恢復後補跑 Unity Edit Mode。
- 下一步為 PB-P0-014C：殭屍完整反應窗口、漏防後果與永久士氣上限損傷。

卡牌打出管線已於 2026-08-22 重構：

- 手動與抽到自動打出共用 `Costs -> Effects -> 使用次數 -> 棄牌／耗盡`。
- 自動打出免付 `EnergyCost`；手動打出必須支付。
- 自動打出若無法支付 `Costs`，不執行效果並留在手牌；同 Turn 補足後可改為手動打出。
- 附加牌耗盡不建立正規卡位補位需求。
- 1 次使用的殭屍成功後耗盡；2 次以上的殭屍第一次會進棄牌堆並可在後續牌組循環再次觸發。
- Production 與 Editor assembly 已通過編譯（0 warnings / 0 errors）；MCP 未連線，因此新增 Edit Mode 案例尚未在 Unity Test Runner 執行。

目前殭屍失敗條件仍未完全定案：

- 殭屍傷害
- 基地血量
- 防禦工事減傷
- 最終失敗條件

已知現行規則：

- 回合結束時，場上殭屍會對士氣造成壓力。
- 戰鬥值用於點選並標記可擊殺殭屍。
- 合併屍潮是保留想法，尚未實作。

### 殭屍 UI 排版

最近完成的修改：

- 新增 `Scripts/Cards/UI/Zombies/ZombieThreatLayoutSlot.cs`
- 新增 `Scripts/Cards/UI/Zombies/ZombieThreatLayoutCalculator.cs`
- 修改 `Scripts/Cards/UI/Zombies/ZombieThreatViewController.cs`
- 新增 `Tests/Editor/ZombieThreatLayoutCalculatorTests.cs`

排版行為：

- 第一隻殭屍顯示在 `ZombieThreatRoot` 中央。
- 多隻殭屍採中心向外排版：
  - 中央
  - 左
  - 右
  - 更左
  - 更右
- 超過單列容量後往下排，並逐步縮小。
- 這只是 UI 排版，不改戰鬥邏輯。

使用者已回報測試正常。

## 6. 目前重要設計決策

### 倖存者與能量

- 初始 `SurvivorCount = 1`。
- 初始 `MaxEnergy = 1`。
- 普通倖存者：人口 +1、最大能量 +1。
- 特殊倖存者：人口 +1、不增加能量，改提供永久能力。
- 倖存者 V1 不死亡、不離開、不受傷。
- 玩家可以拒絕接納倖存者。
- 若永久食物很充足卻拒絕接納，會損失士氣。

每日食物需求：

```text
難度基礎需求 + SurvivorCount * 2 + 修正值
```

普通難度基礎值目前暫定為 0。

### 回合與牌組

- 一輪固定 3 個 Turn。
- 每 Turn 最大能量重新補滿。
- 未使用能量在 Turn 結束消失。
- 目前牌組是 15 張，每 Turn 抽 5 張，剛好 3 Turn。
- 未來可資料驅動支援 18/6/3、21/7/3。
- 牌組配置不會隨 `MaxEnergy` 自動擴張，需由建築、欄位或基地升級主動解鎖。

### 地區有限資源

地區預計有：

- 普通卡包初始數量
- 稀有卡包初始數量
- 極稀有卡包初始數量
- 各自基礎權重
- 有限殭屍數量
- 探索熱度
- 清理度

抽卡包採剩餘比例權重：

```text
CurrentWeight[i] = BaseWeight[i] * RemainingCount[i] / InitialCount[i]
Probability[i] = CurrentWeight[i] / 所有可用類別 CurrentWeight 總和
```

抽中某類卡包後，該類剩餘數量 -1。

普通卡包通常較早耗盡，因此越探索後面，稀有與極稀有卡包的相對機率會上升。

地區枯竭後會從一般探索選項移除。

### 卡包與殭屍

- 卡包資料決定會產生幾張殭屍卡或風險。
- 地區剩餘殭屍數量用來限制當地自然遭遇。
- 產生殭屍卡時，地區剩餘殭屍數量會減少。
- 若地區殭屍歸零，當地自然遭遇率固定為零。
- 世界威脅影響殭屍種類與體質，不直接改變卡包給的殭屍張數。

### 終局設計

每個存檔都應經歷一個終局挑戰。

特殊終局有：

1. 終局出現條件
2. 終局選擇天數
3. 終局選擇事件
4. 接受後事件包
5. 終局達成條件
6. 終局過時事件包
7. 終局達成事件

若特殊終局條件達成，會在固定天數出現選擇事件。玩家接受後，此存檔終局固定。玩家拒絕後，該終局在本局作廢，直到其他特殊終局或預設終局發生。

預設終局會在某個時間點強制啟動。

屍潮終局例子：

- 條件：探索軍事基地取得 `未知信號發收器`。
- 選擇事件：第 41 天。
- 接受後進入屍潮事件包。
- 達成條件：清理完所有殭屍或撐過指定事件序列。
- 終局完成後可以計分結束，也可以繼續無盡模式。

### 士氣簡化方向

原本士氣有手動食物計畫，玩家可調整方案。後來認為可能過於繁瑣。

目前保留的新方向：

- 永久食物超過倉庫上限時，多餘食物可等比例轉換為士氣。
- 讓多餘食物不只是浪費，而是正回饋。
- 士氣降低方式未完全定案，可能與時間、事件、殭屍、基地狀況有關。
- 臨時食物不應因支付輪末需求而懲罰士氣，否則烹飪系統會變得過弱。

### 欄位強化方向

保留但未實作。

概念：

- 不只強化卡牌，也可以強化「欄位」。
- 例如食物欄位若打出食物牌，獲得食物 +1。
- 或某欄位消耗 +1，但資源產出翻倍。
- 這讓本遊戲靠近卡牌構築，但不是傳統只改卡本身的構築。

需注意：

- 欄位構築與卡牌構築應互補，不互相搶核心。
- 前期以生存與資源壓力為主。
- 後期可讓玩家追求卡牌與欄位的組合引擎。

## 7. 視覺方向

目前鎖定概念：

- 方向名：`末日勤務手冊`
- 風格：簡單易懂、細節較少、偏手冊與記錄感。
- 參考：殺戮尖塔的卡牌樣式設計，但更簡潔。
- 地域：災變後 1-3 年的北美五大湖鏽帶虛構城市。
- 血腥程度：低至中度，有撕裂與破裂痕跡，但不描繪細緻傷口。

後來使用者修正卡牌圖片方向：

- 卡牌圖片偏徽記式、剪影式。
- 以不同顏色區分類型。
- 事件與地區可以使用場景式圖片。
- AI 生圖先暫停，使用者可能之後找現成素材。

## 8. 已驗證或使用者回報正常的項目

- 正規牌組合法性四項測試正常。
- 臨時食物機制正常。
- ZombieDetailView 開啟問題已修好。
- 建築施工完成後卡牌刪除 bug 已處理。
- `PB-P1-012` 事件縮放驗證成功。
- `PB-P1-013` 支付邊界與 Forced Loss 測試成功。
- `PB-P2-009A/B` 測試正常。
- 殭屍 UI 排版測試正常。
- `PB-P0-014A/B` 士氣核心與統一批次結算測試正常（全套 Edit Mode 161 / 161）。

## 9. 目前待辦與建議下一步

最適合接著做的工作有三條：

### A. 補齊殭屍傷害與失敗條件設計

目前殭屍只是威脅與士氣壓力，完整失敗條件尚未定案。

需要設計：

- 殭屍沒清掉時怎麼造成傷害。
- 是扣士氣、扣基地耐久、破壞建築，還是混合。
- 防禦工事如何減傷。
- 玩家何時死亡或基地崩潰。

PB-P2-009D 已把事件與場上殭屍壓力連起來，下一步可使用正式屍群情境討論並驗證這組規則。

### B. 後續內容與視覺工作

- 為 3 種正式殭屍製作可辨識的獨立美術。
- 長局 Playtest 後調整 Profile 預算、權重、Tier 與單次數量上限。
- 正式事件池目前只有 3 個 Ready 事件，其他事件仍應依各自缺少的支援系統逐步完成。

## 10. 常用程式碼區域

- `Scripts/Cards/`
- `Scripts/Cards/UI/`
- `Scripts/Cards/UI/Zombies/`
- `Scripts/ValueSystems/`
- `Scripts/Exploration/`
- `Scripts/Buildings/`
- `Scripts/Events/`
- `Scripts/Threats/`
- `Scripts/Run/`
- `Tests/Editor/`

重要類別：

- `CardBase`
- `CardController`
- `CardInventory`
- `CardSelectionRequest`
- `GameEffectData`
- `FoodSystem`
- `CombatSystem`
- `ZombieThreatRuntime`
- `ZombieThreatViewController`
- `ExplorationSystem`
- `RegionRuntime`
- `BuildingSystem`
- `EventScheduler`
- `EventData`
- `EventMagnitudeRuntime`
- `RunFlagRuntime`
- `HordeBudgetRuntime`
- `HordeCompositionRuntime`

## 11. 工作習慣與注意事項

- 不要手寫 Unity `.asset` YAML；用 Unity Editor API / Setup Utility 建立或更新 Asset。
- Setup Utility 通常設計成冪等，可以重複執行。
- 使用者偏好先討論規劃，再實作大型系統。
- 小 bug 可以直接修。
- 使用者正在學程式，講解時請盡量說明語意與資料流，不只說做了什麼。
- 對 `IEnumerable`、`Action<T>`、`switch case`、屬性語法等基礎 C# 語法，使用者會希望用初學者角度解釋。
- 避免一次導入過度抽象；專案仍在可玩 Demo 與核心規則成形階段。
- 若要更動場景或 Prefab，新的 task 優先使用 Unity MCP。

## 12. 2026-09-06：P0 Gameplay HUD 最新狀態

- P0 驗證場景：`Scenes/GameplayHUD_P0_Integration.unity`。
- 重建入口：`Tools > Zombie Card Survive > Gameplay HUD > Build P0 Integration Slice`。
- HUD 已改為事件式讀取 Runtime 士氣、食物、資源、戰鬥、能量、手牌與牌堆，不使用每幀輪詢，也不持有 Gameplay 權威資料。
- 使用者已確認第一輪功能正常；畫面仍有細節與美術精修空間。
- 最新驗證圖：`Temp/CodexP0/p0_runtime_final.png`。
- 2026-09-04 執行完整 EditMode 測試時，系統/pagefile 使用率達 97%，Unity 因記憶體壓力崩潰。完整套件沒有完成，不可記為通過。
- 2026-09-06 在 Unity 關閉時完成靜態審查與修正：保留資源交易不再公開中間狀態、回合產出只發布一次最終狀態、士氣 Reset 會通知 HUD、Presenter 可安全重新綁定、缺少 CardController 時會隱藏舊手牌。
- P0 Setup Utility 已準備建立真正的 `RunPhaseController` 並套用 `ScriptObj/Morale/MORALE-PYRAMID-DEMO-01_StandardSurvival.asset`；Unity 恢復後必須重建一次場景才會把新引用寫入 Scene。
- 已新增針對性測試；Unity 重啟後先只跑：`ValueSystemStateChangedTests`、`BuildingRuntimeTests` 的 reservation commit 新測試、`GameplayHudRuntimePresenterTests`。
- 2026-09-06 外部 C# 低併發建置成功：0 warning、0 error。新增測試檔仍需 Unity 重新產生專案檔後再編譯／執行。
- 完整 P0／ADR-0001 驗收仍未完成：10 手牌、7 建築／12 殭屍壓力、殭屍血量獨立點擊、Canvas 與世界共用鎖定、1280×720 字級／對比、色弱／灰階、減少動態效果及效能量測。

## 13. 2026-09-06：Paper UI 材質預覽

- 2026-09-17：使用者核准建築書籤面板完整計畫並授權補前置文件。新增 `Docs/Architecture/tr-registry.yaml`（僅此切片的 TR-BUI-001–008）、`Docs/Architecture/adr-0002-building-bookmark-presentation.md`（Proposed）、`production/epics/building-ui/BUILDING-UI-001.md`（16 項驗收）與 `production/qa/evidence/building-bookmark-panel-evidence.md`（NOT RUN）。尚未修改程式／場景／Prefab。事件與探索操作已由使用者確認正常。使用者表示 MCP 已開啟，但本工作階段尚無 Unity 工具／資源，不能宣稱已連線。下一步確認 ADR-0002 並取得 registry 登錄同意後，接續實作；不新增昨日紀錄、劇情／收藏系統，不跑完整測試套件。

- 2026-09-16 後續：使用者明確同意保存 SampleScene 全部目前修改（含手動調整）。已於 Edit Mode 對該場景唯一 ExplorationFlowView 套用最終結果內距／行距設定，確認 resultPanel、resultSummaryText、ScrollRect content／viewport、RectMask2D 引用完整，SaveScene 回報 true、isDirty=false。Console 0 errors。此前保存阻擋已解除；未執行完整 Play 互動驗收，最終內距調整未重新截圖。

- 2026-09-16：使用者已手動套用並確認事件 Sprite／代價排版、支付及結果數值一致；標題手動修好。工具已補 `titleText`，不再漏綁。探索第一版 `ExplorationPresentationSetupUtility` 完成：三地區卡比例排版、插圖引用、TMP inline sprite 支援、紙張樣式、結果 ScrollRect／RectMask2D＋長文字區，保留原三選一／自動开包與結算邏輯。已經 MCP 套用至載入的 SampleScene，但尚未確認保存。排除 Region1/Image 無 sprite 的舊白色佔位物（停用 Image，未刪除）。克隆 UI 預覽 `Temp/CodexPaperUI/exploration_selection_20260916.png`、`exploration_result_20260916.png`，未進入 Play、未改探索狀態。結果預覽發現紙張內距不足，程式後續改 viewport 至 .055/.14–.945/.86、行距 6，該最終微調尚未套用／重新截圖。Unity Console 0 errors；離線編譯 0 errors／322 warnings。最後「重複套用＋SaveScene／SaveAssets」被自動審核拒絕，理由為可能保存其他未提交場景修改；需使用者明確同意保存 SampleScene 當前修改，或由使用者手動執行 Apply Exploration Presentation 並保存。不得記為已保存或完整互動驗收通過。

- 2026-09-14：事件 inline Sprite 與代價 Layout 程式完成，場景待套用。新 `EventTextIconSetupUtility` 入口 `Tools > Zombie Card Survive > Gameplay HUD > Apply Event Text Icons and Cost Layout`（Edit Mode），生成 food/resource/combat/energy 四個 TMP Sprite Asset，以 food 的 fallback 串起其餘三者，綁定事件敘述／紀錄／結果摘要／選項文字。標記格式 `<sprite name="food">`，不自動改寫內容或遊戲效果。代價遷移為 CostLayout／資源群組兩層 HorizontalLayoutGroup；Runtime 隱藏整组以消除空位，維持不足與無代價行為；選項名稱寬度收至 52% 以保留代價區，不改圖片／敘述外部錨點。舊 ApprovedResourceIconSetupUtility 也接入，重複套用以既有序列化引用找圖示，避免重建巢狀圖示。離線編譯 0 errors／320 warnings（含外部參考、序列化欄位警告）；本工作階段無可呼叫 Unity MCP，未生成 .asset／保存 Scene／做視覺驗證。使用說明 `Art/GameplayHUD/Icons/Review01/event-inline-icons.md`。需檢查四種圖示的行內基準線／換行、單雙代價／無代價／不足與三位數成本排版。

- 2026-09-13：事件結果圖示程式完成、場景套用待辦（本工作階段沒有 Unity MCP 工具）。`EventChoiceExecutionPlan` 在支付前／結算後捕捉四種核心數值，將 `EventValueSnapshot` 差值保存在 `EventChoiceExecutionResult.ValueChanges`。新 `EventResultValuesView` 顯示四個獨立 Image＋TMP 正負數字，標示「本次淨變化（含支付）」；零變化項目隱藏，特殊效果與士氣仍由摘要文字呈現，缺引用回退文字。`ApprovedResourceIconSetupUtility` 已加入 ResultValueIcons 建立／引用與結果區排版；需在 SampleScene Edit Mode 執行 Apply Approved Resource Icons 並保存。離線編譯 0 errors（完整專案有既有／序列化欄位與外部參考警告）；純資料直接呼叫檢查實際差值、不重複扣費、圖示文字去重及舊結果回退通過。新增 NUnit 測試可編譯，但未在 Unity Test Runner 執行；沒有場景／截圖／Play 驗證。結果顯示淨值，例支付 4 食物再獲得 6 顯示 +2，成本仍於選擇前獨立顯示。

- 2026-09-10 後續可讀性調整：使用者確認圖示替換與代價正常，但尺寸太小。事件成本圖示由 40 增至 56 px，數字 26–32、選項名稱 24–32、無代價 24–30、不足原因 20–24；雙成本間距同步拉開。修改集中於 `ApprovedResourceIconSetupUtility`，已保存 SampleScene，沒有更改 HUD 其他數值區尺寸。Edit Mode 雙成本可選／不足預覽：`Temp/CodexPaperUI/event_icons_enlarged.png`，無重疊；編譯 0 errors。

- 2026-09-10：核准四張資源圖示已套用。食物 `icon_resource_food_02.png`、資源 `icon_resource_parts_03.png`、戰鬥 `icon_resource_combat_01.png`、能量 `icon_resource_energy_01.png`，位於 `Art/GameplayHUD/Icons/Review01/`。新增可重複套用的 `ApprovedResourceIconSetupUtility`，同步接入 Paper UI flow 與 P0 建立工具。SampleScene 四個事件選項使用獨立 Image＋TMP 成本；不足時維持停用、圖示變淡、紅字成本及原因，無成本顯示「無代價」，缺圖時回退舊文字。P0 HUD 四個數值使用核准圖示，Presenter 保留原數值來源與更新方式。克隆 UI 上檢查雙成本、單成本、不足、無代價四種情境通過；編譯 Console 0 errors。預覽 `Temp/CodexPaperUI/event_approved_icons.png`、`hud_approved_icons.png`（示例值、非完整遊戲驗收；HUD 截圖後再收斂數字字級上限）。使用者先前正在 Play，本次已退出；最後保持 SampleScene Edit Mode。未執行完整測試套件。

- 2026-09-09：事件畫面排版依使用者草圖更新並套用到 `Scenes/SampleScene.unity`，規則寫回可重複執行的 `PaperUiFlowSkinUtility`。左上為事件插圖、左下為可選填事件紀錄、右上為標題與當前處境敘述、右下固定分配四個等高選項欄位。`EventData` 新增 `JournalText`，`EventFlowView` 在選擇與結果狀態皆顯示該欄；舊事件未填寫時保持留白。既有 `FlowInspectionController` 的查看／返回按鈕不屬於 EventFlowView，本次已將兩者移到同一個右上區域並保留原鎖定／返回邏輯。結果狀態仍以右下「下一步」取代四個選項。靜態選擇狀態預覽：`Temp/CodexPaperUI/event_layout_four_choice_review.png`。
- 使用者後續手動放大事件圖片：`EventImage` 錨點改為 X `0.025–0.46`、Y `0.35–0.965`；`JournalText` 改為 X `0.035–0.455`、Y `0.055–0.30`，保留左右 12 px、上下 8 px 文字內距。已同步寫回 `PaperUiFlowSkinUtility` 並更新靜態預覽場景，避免重新套用工具時覆蓋。證據：`Temp/CodexPaperUI/event_layout_manual_large_art_review.png`。
- 2026-09-09：事件選項整合代價、不可選原因與結果摘要。每列左側為行動、右側由 `EventChoiceAvailability` 顯示永久食物／資源支付；不可選時停用按鈕並顯示具體不足。成功執行時 `EventChoiceExecutionResult` 保存實際套用的效果清單，結果頁顯示支付、Forced Loss、士氣與主要效果摘要，UI 不重算遊戲狀態。`SampleScene` 已建立並綁定四組 `ChoiceCostText`／`UnavailableReasonText` 與 `ResultSummaryText`，場景驗證 0 issues，Unity 編譯 0 errors。靜態選擇預覽已更新於 `Scenes/PaperUI_FlowReview.unity`。

- 2026-09-08 後續已用 Edit Mode 相機直接渲染四類預覽，解決 Scene View 截圖停留舊畫面的驗證障礙。證據：`Temp/CodexPaperUI/review_EventPanelUI.png`、`review_ExplorationFlowView.png`、`review_ZombieDetailView.png`、`review_BuildingDetailView.png`。確認紙張與文字配色套用可見；沒有進入 Play，渲染後還原 Canvas 模式、相機目標與面板開關。
- 本次材質外觀檢查完成，並非正式 UX／互動驗收：預覽沿用原版型和佔位文字，事件標題偏大、建築按鈕文字超出底板；事件／探索白色圖片區為未填入插圖的預覽。後續需獨立排版修整與真實內容檢查。

- 2026-09-08 使用者手動停止後，MCP 已確認 Edit Mode、Console 0 errors／0 warnings。Scene View 可看到事件紙張，但切換預覽後截圖仍回傳舊事件畫面；四類面板視覺驗收仍待完成。已還原事件預覽顯示，未再次進入 Play。

- 2026-09-08 已套用正式流程表面：SampleScene 的事件／探索使用亮紙背景與深色文字、探索卡及事件選項使用亮色九宮格、殭屍／建築詳情使用深色九宮格；同步更新 BuildingDetailView.prefab 及 P0 DetailPanel／Notification。
- 重複套用工具：`Scripts/UI/World/Editor/PaperUiFlowSkinUtility.cs`。套用前後比對 SampleScene 109 個非 Graphic MonoBehaviour 序列化資料一致，未更動流程、按鈕引用與 CanvasGroup。
- 靜態視覺預覽場景：`Scenes/PaperUI_FlowReview.unity`，包含四類面板副本，已移除 Gameplay scripts。編譯 Console 0 errors；Play 截圖因 MCP timeout 尚未完成，不可宣稱視覺驗收通過。已送出停止 Play 要求，需確認結果。

- 2026-09-08 補驗證完成：P0 Play Mode 正確顯示「【白天】 → 下午 → 黑夜」，回合控制器為 1 個；實際截圖確認中央士氣、左上金字塔與右上資源列不再重疊。Console 0 errors／0 warnings，已成功退出 Play Mode。證據：`Temp/CodexPaperUI/p0_phase_layout_verified_20260908.png`。這次只驗證前述修正，不代表整套 HUD／回合流程已全面驗收。

- 2026-09-07 後續：已保存 P0 士氣／資源排版調整，金字塔移到左上 TimePanel；確認 Scene 原本缺少 RunPhaseController，已建立並綁定 Presenter 與 P0GameplayHudSliceController。Editor 編譯無錯誤。
- 本次 Play 驗證未完成：Unity Editor.log 回報 paged memory 95%（約 25.8 GB / 27 GB），MCP 斷線／逾時。已送出停止要求但未取得可靠成功回應；需使用者確認 Unity 停止 Play 後釋放記憶體，再補驗證時段與排版。不可記為 Runtime 通過。

- 2026-09-07 第一階段套用：P0 場景的 TimePanel、MoralePanel、ResourceBar、ShortcutRail 與快捷項目使用已核准的深色 Paper UI，保留原互動引用；P0 建立工具同步套用。運行截圖：`Temp/CodexPaperUI/p0_paper_ui_20260907.png`，Console 0 errors。
- 此次僅完成常駐表面替換。截圖仍顯示既有士氣／金字塔重疊及「時段尚未初始化」，需後续修正；事件、探索、詳情與卡牌尚未套用此材質，不能視為整體 UI 驗收完成。

- 新增材質包：`Art/GameplayHUD/PaperUI/`，包含全螢幕紙張背景、亮／暗九宮格面板、可重複纖維材質與透明石墨磨損層。
- 已由 Unity 套用 Sprite、Alpha、Clamp／Repeat、Mipmap、最大尺寸與九宮格 Border 設定。
- 亮面板 Border：`160 / 120 / 160 / 120`；暗面板 Border：`96 / 72 / 96 / 72`。
- 新增可重複建立的 Editor 工具：`Scripts/UI/World/Editor/PaperUiMaterialPreviewSetupUtility.cs`。
- 選單入口：`Tools > Zombie Card Survive > Gameplay HUD > Build Paper UI Material Preview`。
- 預覽場景：`Scenes/PaperUI_MaterialPreview.unity`；不覆寫 SampleScene 或 P0 HUD 場景。
- Unity 場景驗證 0 issues，Console 0 errors；完整測試套件未執行。
