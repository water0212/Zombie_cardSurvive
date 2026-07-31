# Building System V1 使用指南

本資料夾存放建築系統共用的 UI Prefab。建築本身採資料驅動設計：一般情況下，新增建築不需要複製 Prefab 或修改 `BuildingSystem`，只需要建立並組合 ScriptableObject 資料。

## 1. Prefab 用途

| Prefab | 用途 |
|---|---|
| `BuildingWorldView.prefab` | 基地畫面中的建築、空地與施工狀態 |
| `BuildingDetailView.prefab` | 點擊建築後顯示詳情、施工與互動列表 |
| `CardCraftView.prefab` | 一條獨立的卡牌製作列 |
| `CraftRecipeButton.prefab` | 製作配方按鈕 |
| `CraftJobRow.prefab` | 製作佇列中的一項工作 |
| `FoodPlanInteractionView.prefab` | 情報中心使用的食物計畫設定面板 |
| `CardUpgradeView.prefab` | 建築強化指定卡牌時使用的互動面板 |

所有建築共用這些 Prefab。建築名稱、圖片、顏色、座標、等級與功能由 `BuildBaseData` 決定。

## 2. 新增可製作卡牌的建築

以下以「廚房」為例。

### 步驟一：建立建造卡

在 Project 視窗選擇：

`Create > Zombie Card Survive > Cards > Construction Card`

建造卡固定視為 `Build` 類型，只能放入 `Unrestricted` 欄位。通常不需要設定使用次數限制，讓同一張建造卡可以跨輪反覆提供施工進度。

### 步驟二：建立製作配方

選擇：

`Create > Zombie Card Survive > Buildings > Card Craft Recipe`

設定：

- `Display Name`：配方名稱。
- `Artwork`：配方圖示。
- `Output Card`：完成後加入卡牌倉庫的卡牌。
- `Output Count`：每次完成的產量。
- `Required Production`：完成所需製造力。
- `Cost`：排入佇列時保留、完成時扣除的食物與資源。

### 步驟三：建立製作列

選擇：

`Create > Zombie Card Survive > Buildings > Card Craft Line`

設定：

- `Required Building Level`：建築達到幾級後解鎖。
- `Base Production`：每次事件後取得的製造力。
- `Queue Capacity`：同時可保留的工作數量。
- `Recipes`：此製作列可以製作的配方。

每一個 `Card Craft Line` 都是獨立產線，擁有自己的完整製造力、佇列與溢出製造力。

### 步驟四：建立建築資料

選擇：

`Create > Zombie Card Survive > Buildings > Building`

設定：

- `Display Name`：建築名稱。
- `Artwork`、`Color`：基地畫面外觀。
- `Anchored Position`：建築在 Canvas 基地畫面中的座標。
- `Starts Unlocked`：是否在新遊戲開始時直接顯示為等級 0 空地。
- `Levels`：各級施工需求與等級獎勵。
- `Interactions`：此建築擁有的製作列或其他互動。

### 步驟五：加入建築目錄

開啟 `SampleScene`，找到：

`BuildingV1Root > BuildingSystem > Building Catalog`

把新的 `BuildBaseData` 拖入 `Building Catalog`。只有目錄中的建築能被 `BuildingSystem` 查詢或解鎖。

測試新建築時，可以先勾選 `Starts Unlocked`。進入 Play Mode 後，建築會以等級 0 空地顯示在指定座標。

## 3. Building Level 設定

`BuildBaseData.Levels` 的每一筆資料代表一個可施工等級。

| 欄位 | 說明 |
|---|---|
| `Level` | 目標等級，必須依序設定為 1、2、3…… |
| `Construction Cost` | 開始施工時保留、完成時扣除的成本 |
| `Required Construction Progress` | 完成此等級需要的施工進度 |
| `Progress Per Card Play` | 每次打出相關建造卡增加的進度 |
| `Construction Card` | 此等級使用的建造卡資料 |
| `Construction Card Count` | 開始施工後放入倉庫的建造卡數量 |
| `Free Integration Count` | 事件後可免費整合進正規牌組的上限 |
| `Manufacturing Bonus` | 此等級提供給既有製作列的製造力加成 |
| `Reward Description` | 建築詳情顯示的等級效果說明 |

Runtime 使用「目前等級 + 1」尋找下一級，因此等級不可跳號。

## 4. 擴充既有建築

### 增加新配方

1. 建立新的 `Card Craft Recipe`。
2. 打開目標 `Card Craft Line`。
3. 把新配方加入 `Recipes`。

### 升級後解鎖第二條產線

1. 建立第二個 `Card Craft Line`。
2. 將其 `Required Building Level` 設為解鎖等級，例如 2。
3. 把第二條製作列加入建築的 `Interactions`。
4. 在建築的 `Levels` 新增對應 Lv2 資料。

第二條製作列會與第一條完全獨立，代表真正增加平行產能。

### 增加建築等級

在 `Levels` 後方加入下一級資料，並設定該級施工成本、需求進度、建造卡與獎勵。升級施工期間，原等級已解鎖的製作列仍會運作；新製作列從升級完成後的下一輪開始取得製造力。

## 5. 鎖定與解鎖

未勾選 `Starts Unlocked` 的建築不會建立 Runtime 或 GameObject，也不會出現在基地畫面。

取得藍圖、倖存者或其他解鎖來源後，需要呼叫：

```csharp
buildingSystem.UnlockBuilding(buildingData.BuildingId);
```

`Unlock Requirement IDs` 目前只是預留的資料欄位。V1 尚未自動檢查藍圖或倖存者條件，解鎖來源必須主動呼叫上述 API。

## 6. 新增全新互動類型

目前已完成的通用互動是：

- `CardCraftInteractionData > CraftLineRuntime > CardCraftView`
- `FoodPlanInteractionData > FoodPlanInteractionView`
- `CardUpgradeInteractionData > CardUpgradeView`

另外，`FoodStorageInteractionData` 是被動互動資料，不會建立面板；建築達到指定等級後會直接增加 `FoodSystem.StorageCapacity`。

新增 `BluePrintView`、研究、倖存者派駐或其他新功能時，不能只建立資料資產，還需要：

1. 建立新的 `BuildingInteractionData` 子類別。
2. 建立保存進度與狀態的 Runtime 類別。
3. 建立對應 View Prefab 與顯示腳本。
4. 在 `BuildingRuntime` 建立對應 Runtime，或明確定義它是無 Runtime 的即時/被動互動。
5. 在 `BuildingDetailView` 加入辨識資料類型並建立 View 的規則。
6. 若功能需要事件後推進，在 `BuildingSystem.ResolveAfterEvent()` 加入結算。

View 只負責顯示與送出操作，真正狀態必須保存在 Runtime，避免關閉介面後遺失進度。

## 7. Setup Building V1 的定位

`Tools > Zombie Card Survive > Setup Building V1` 是建立 V1 工作台範例、共用 Prefab 與 SampleScene 接線的初始化工具。

新增一般建築時，不需要修改或重新依賴這個 Setup。應建立新的資料資產，並將 `BuildBaseData` 加入 `Building Catalog`。

只有共用 Prefab、SampleScene 接線方式或工作台 V1 預設內容改變時，才需要修改 `BuildingV1SetupUtility`。

`Tools > Zombie Card Survive > Setup Building Content V1` 是 V1-2 內容包工具，會建立：

- 倉庫、情報中心、防禦工事。
- 調度、改良調度、守備射擊、強化守備射擊。
- 12 個 10 天短局測試事件。
- `FoodPlanInteractionView` 與 `CardUpgradeView` Prefab，並接到 `BuildingDetailView`。

這個工具也是冪等設計；重跑會更新既有資產與場景引用，不應產生重複目錄項目。

## 8. 常見檢查

- 建築沒有出現：確認已加入 `Building Catalog`，並勾選 `Starts Unlocked` 或呼叫解鎖 API。
- 點擊空地不能施工：確認目前是 `Turn` 階段、資源足夠，且下一級資料有指定建造卡。
- 製作列表沒有出現：確認建築等級已達 `Required Building Level`，且製作列已加入 `Interactions`。
- 配方按鈕沒有出現：確認配方已加入製作列的 `Recipes`。
- 製作無法排隊：確認目前是 `Turn`、佇列未滿，而且可用資源扣除保留量後仍足夠。
- 建造卡無法替換：建造卡只能替換 `Unrestricted` 欄位。
- 新增互動資產後沒有畫面：確認 `BuildingDetailView` 已有對應 Prefab 引用，並且該互動類型已在 `BuildingDetailView` 中被辨識。
