# Quick Design Spec: PB-P0-014C 殭屍漏防、永久士氣上限損傷與下輪效果

**Type**: Addition  
**System**: 殭屍 / 戰鬥 / 士氣 / 下一輪效果  
**GDD Reference**: `Docs/GameDesign/ZombieSurvivalCard_GDD.md` 第 30.6、30.7、30.9 節  
**Date**: 2026-08-23

## Change Summary

為殭屍補上完整反應窗口與資料化漏防後果。殭屍生成時不立即造成傷害；輪末先處理玩家標記的擊殺，再由已取得完整操作窗口且仍存活的殭屍建立當前士氣傷害、永久士氣上限損傷與下一輪負面效果。下一輪負面效果以獨立 `NextRoundEffectData` 定義，除殭屍外也可由卡牌與事件重用。

## Motivation

殭屍後果必須獎勵平時保留戰鬥值的玩家，而不是在玩家尚未操作前直接懲罰。同一份「下一輪效果」也不應分別在殭屍、卡牌與事件重寫三套資料；來源只負責決定何時觸發，效果資料與排程生命週期維持一致。

## New Rules / Values

### 完整反應窗口

- 第 N 輪任一 Turn 抽出並生成的殭屍，最早可於第 N 輪輪末產生後果；玩家保有該 Turn 剩餘時間及本輪後續 Turn。
- 第 N 輪輪末由剩餘加算卡自動揭露的殭屍，最早可於第 N+1 輪輪末產生後果。
- 第 N 輪結束後、第 N+1 輪開始前由事件生成的殭屍，最早可於第 N+1 輪輪末產生後果。
- 無法辨識生成時點的相容路徑採安全預設：至少延後到下一輪輪末。
- 「生成時無效果」只限制殭屍後果，不改變殭屍卡成功打出時的正常 Cost、Effects、使用次數與棄牌／耗盡流程。

### 輪末順序

```text
揭露本輪剩餘加算卡
-> 結算已標記擊殺
-> 篩選已取得完整反應窗口的存活殭屍
-> 建立本輪殭屍士氣／永久上限損傷
-> 排程存活殭屍的下一輪後果
-> 結算本輪食物與其他士氣來源
-> 提交單一士氣批次與輪末報告
```

- 當輪已擊殺的殭屍不產生任何殭屍後果。
- 尚未取得完整反應窗口的殭屍可留在場上，但本輪不建立後果。

### 殭屍數值來源

- `ZombieCardData.Health`：殭屍血量；由殭屍 Asset 決定。
- `ZombieCardData.Attack`：存活時造成的當前士氣傷害；由殭屍 Asset 決定。
- `ZombieCardData.PermanentMaximumMoraleDamage`：永久士氣上限損傷；`0` 代表沒有。
- `ZombieCardData.SurvivalNextRoundEffect`：可選的 `NextRoundEffectData` 引用。
- 世界威脅與 `HordeProfile` 只決定屍群預算、組成、數量與候選資格，不直接覆寫殭屍本體的血量、攻擊或後果。

### 當前士氣與永久上限損傷

- 同一輪所有符合資格的存活殭屍先聚合，再送入 PB-P0-014B 的輪末士氣批次。
- 永久損傷先降低 `MaximumMorale`，再把 `CurrentMorale` 限制到新上限。
- 因新上限造成的當前士氣限制不再額外重複記成同額士氣傷害。
- V1 不設全域或單輪永久損傷上限；危險度由殭屍 Asset 配置、稀有度及 `HordeProfile.MaxCountPerHorde` 控制。
- 輪末報告需分開記錄當前士氣傷害、永久上限損傷、各殭屍來源與實際提交結果。

### 可重用的下輪效果資料

- 新增 `NextRoundEffectData : ScriptableObject`，表示可被不同來源引用的延遲效果定義。
- Data 內含一組 `NextRoundEffectEntry`；每項至少具有 `EffectType` 與 `Amount`。
- PB-P0-014C V1 只實作 `IncreaseNextRoundFoodDemand`。
- `IncreaseNextRoundFoodDemand +3` 一律表示「下一個 Round 的固定食物需求 +3」，不是下一個 Turn。
- 多個來源排程同類效果時相加；效果在目標輪結束完成食物結算後消耗並清除。

### 不同載體的觸發規則

`NextRoundEffectData` 只描述效果內容，不自行決定觸發者或時點。來源規則如下：

| 來源 | 引用位置 | 排程條件 | 生效時間 |
|---|---|---|---|
| 殭屍 | `ZombieCardData.SurvivalNextRoundEffect` | 第 N 輪輪末已具攻擊資格且仍存活 | 第 N+1 輪 |
| 卡牌 | `GameEffectData` 的下輪效果分類 | 第 N 輪成功支付 Cost 並完成打出 | 第 N+1 輪 |
| 事件 | `GameEffectData` 的下輪效果分類 | 事件選項驗證、付款及執行成功 | 緊接著開始的下一輪 |

- 卡牌或事件直接引用時不需要場上存在殭屍，也不把效果附加到殭屍身上。
- 卡牌 Cost 無法支付、打出失敗，或事件選項驗證／付款失敗時，不建立排程。
- 事件使用 V1 後果時不套用事件倍率；事件倍率整合不屬於 014C。
- 殭屍在輪末成功排程後，該次效果立即鎖定；下一輪擊殺該殭屍不取消已排程後果，只阻止它替再下一輪建立新排程。
- 遊戲開始前的事件若成功排程，效果套用於第 1 輪。

### 程式架構約束

- `NextRoundEffectData` 是效果定義層，負責「做什麼」，不保存 Run 中的倒數或來源狀態。
- `GameEffectData` 是卡牌／事件的效果轉接層；新增下輪效果分類及 `NextRoundEffectData` 引用欄位。
- `GameEffectCarrier` 仍表示這筆 `GameEffectData` 由 Card、Event 或 Other 執行，用於 Inspector、付款語意與 Context；它不是 `NextRoundEffectData` 的容器，也不單獨決定延遲時點。
- `ZombieCardData` 直接引用 `NextRoundEffectData`，不得把存活後果放入繼承自 `CardBase` 的一般 `Effects`，避免抽到自動打出時立刻觸發。
- Runtime 需有單一下輪效果排程服務，保存效果定義、數值、來源類型、來源 ID 與目標輪次。
- 卡牌、事件與輪末殭屍檢查都只呼叫同一排程入口；目標輪開始時由排程服務統一啟用，目標輪完成結算後統一消耗。
- `FoodPlanSystem` 只接收目標輪已啟用的食物需求修正，不負責判斷來源或計算下一輪。

## Affected Systems

| System | Impact | Action Required |
|---|---|---|
| `ZombieCardData` | 新增永久損傷與存活後果資料 | 加入序列化欄位與唯讀屬性 |
| `CombatSystem` / 殭屍 Runtime | 需要保留生成與首次可攻擊輪次 | 篩選輪末符合資格的存活殭屍並提供逐隻結果 |
| `RoundEndResolver` | 目前所有存活殭屍立即使用 Attack | 改為先擊殺、再依資格聚合傷害與排程後果 |
| `MoraleSystem` | 接收永久上限損傷 | 經既有批次一次提交、限制與崩潰判定 |
| `NextRoundEffectData` | 新資料類型 | V1 支援下一輪食物需求增加 |
| 下輪效果排程服務 | 新 Runtime 權威 | 排程、啟用、鎖定、堆疊、消耗與重置 |
| `GameEffectData` | 卡牌與事件需能引用相同 Data | 新增後果分類、引用欄位與排程執行分支 |
| `GameEffectDataDrawer` | 新分類需可編輯 | 依 Carrier 顯示共用後果欄位 |
| `RunPhaseController` | 掌握輪次與階段邊界 | 在新局重置及新輪開始時通知排程服務 |
| `FoodPlanSystem` | 接收當輪已啟用的額外需求 | 保持本輪結算後清除，不自行跨輪保存 |
| `RoundEndReport` | 需可追溯 | 加入殭屍資格、永久損傷與下一輪排程明細 |

## Out of Scope

- 正式殭屍存活效果 UI、輪末預警與 Tooltip；屬於 PB-P0-014E。
- 士氣金字塔 Profile 與階段效果；屬於 PB-P0-014D。
- 除食物需求增加外的其他 `NextRoundEffectType`。
- 事件倍率作用於延遲後果。
- 防禦工事減傷、殭屍攻擊動畫、音效與正式數值平衡。
- Save State；但 Runtime 資料需保留可序列化方向。

## Acceptance Criteria

- [x] Turn 中生成的殭屍在該輪剩餘操作窗口內可被擊殺，若輪末仍存活才產生後果。
- [x] 輪末自動揭露與輪間事件生成的殭屍，不會在玩家尚未取得下一輪操作窗口前產生後果。
- [x] 已標記並於輪末擊殺的殭屍不造成當前士氣、永久上限或下一輪效果。
- [x] `Attack` 只造成當前士氣傷害；永久上限損傷由殭屍 Asset 的獨立數值決定。
- [x] 永久上限損傷降低上限並限制當前值，但不重複扣除同額當前士氣。
- [x] 多隻殭屍的士氣與永久損傷聚合後只提交一次，報告可追溯到來源。
- [x] 殭屍輪末存活時可排程下一輪食物需求增加，並在下一輪結算後清除。
- [x] 同一下輪效果 Data 可由卡牌成功打出後排程，且失敗打出不排程。
- [x] 同一下輪效果 Data 可由事件成功選項排程，且驗證或付款失敗不排程。
- [x] 卡牌或事件在第 N 輪建立的食物需求修正只作用於第 N+1 輪，不會在第 N 輪或下一個 Turn 提前生效。
- [x] 已排程的殭屍下輪效果不因下一輪擊殺來源殭屍而取消；該殭屍不再替後續輪次排程。
- [x] 多個殭屍、卡牌與事件引用同類效果時正確相加，且新局開始會完整清除排程。

## Verification Plan

- Edit Mode：不同生成階段的首次可攻擊輪次、輪末擊殺優先、未知時點安全預設。
- Edit Mode：永久上限損傷的套用順序、Current clamp、無重複傷害與多來源聚合。
- Edit Mode：殭屍、卡牌、事件各自成功／失敗排程，跨 Turn 不提前、跨 Round 正確啟用。
- Edit Mode：已排程後擊殺不取消、存活殭屍逐輪重排、混合來源堆疊與新局重置。
- Unity 編譯、全套 Edit Mode 回歸與 Console 0 errors。

### 改名與相容性（2026-08-23）

- 通用資料與排程統一由「殭屍後果」改名為「下輪效果」；因 V1 僅支援下一個 Round，不使用範圍更廣的「延遲效果」。
- `GameEffectCategory.NextRoundEffect` 保留原列舉數值位置，既有 Asset 不會改變效果分類。
- `GameEffectData.nextRoundEffect` 與 `ZombieCardData.survivalNextRoundEffect` 使用 `FormerlySerializedAs` 承接舊欄位名稱，避免既有 Inspector 引用遺失。

### 實作驗證紀錄（2026-08-23）

- Unity 2022.3.62f3 正式匯入與 Runtime / Editor 編譯完成，Console 0 errors。
- `NextRoundEffectTests`：10 / 10 通過。
- Edit Mode 全套回歸：178 / 178 通過。
- 2026-08-24 使用者實測卡牌與殭屍引用下輪效果，結果正確。
- 使用 Unity MCP 建立 `NRE-001_下輪食物需求增加3` Demo Asset；未擅自指定給正式殭屍。
- 額外修正永久上限損傷與鼓舞餐點／高士氣回落的批次預覽順序，避免降低上限後仍扣餐點成本或錯誤追加自然回落。

## GDD Update Required?

是，已於 2026-08-24 套用。GDD 第 30.6 節已補上可重用下輪效果 Data、三種載體時點、排程後鎖定及 V1 食物需求效果，並移除「保留單輪永久損傷上限」的舊規則。
