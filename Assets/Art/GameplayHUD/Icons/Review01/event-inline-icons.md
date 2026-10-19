# 事件文字圖示與代價排版

## 套用

停止 Play，執行 `Tools > Zombie Card Survive > Gameplay HUD > Apply Event Text Icons and Cost Layout`，再儲存場景。
此入口保留圖片、敘述、選項列的外部版面，只調整選項內的代價區及標題文字的可用寬度。
既有 `Apply Approved Resource Icons` 入口也會建立這些設定；它還會套用原有 HUD／結果頁設定。

## 敘述中插入圖示

在 EventData 的敘述、JournalText，或選項文字／結果敘述中輸入：

```text
找到 <sprite name="food">6 與 <sprite name="resource">4。
消耗 <sprite name="combat">3，恢復 <sprite name="energy">1。
```

可用名稱：`food`、`resource`、`combat`、`energy`。請用半形雙引號。
圖示跟著文字排列與換行；不是自動把「食物」兩個字換成圖片。
原有 `{magnitude:0}` 等數值標記可與 sprite 標記混用（索引須對應該段文字使用的效果清單）。
文字標記不會支付或發放資源；實際效果仍須由事件效果資料設定。

工具會以核准的四張 PNG 各建一個 TMP Sprite Asset。主引用為 `EventInline_food.asset`，
其 fallback 指向另外三個資源，因此只需綁定主引用，即可使用四個名稱。
沒有改動原始 PNG，也不改全專案的 TMP 預設資源。

## 代價

每個選項的 `CostLayout` 使用 Horizontal Layout Group 靠右排列。
各資源群組內以 Horizontal Layout Group 排列 Image 與 TMP 數字，間距 6；資源組之間間距 16。
圖示尺寸 56、文字字級 32，文字寬度由 TMP 內容決定。
無該代價時整組隱藏，不留下空位；無任何代價仍顯示「無代價」。
不可支付的選項仍停用，圖示變淡、數字與不足原因呈紅色。

## 驗證狀態

2026-09-14：程式離線編譯通過；本工作階段無可呼叫 Unity MCP，Sprite Asset 生成、場景套用與視覺驗證待 Unity 執行。
請確認雙代價、單代價、無代價、資源不足，以及四種 inline 圖示的換行與基準線。
