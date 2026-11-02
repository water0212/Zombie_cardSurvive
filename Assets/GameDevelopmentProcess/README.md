# GameDevelopmentProcess

更新：2026-11-02。這是本專案唯一現行開發文件入口。先讀 [Story 順序](Story-Sequence.md) 決定下一項，再讀該 Story 的規則、設計與 API。舊目錄已歸檔，不能再以它們的「下一步」排程。

| 文件 | 唯一職責 |
|---|---|
| [GDD](GDD.md) | 目前玩法、確認規則與明確待定事項 |
| [Story-Sequence](Story-Sequence.md) | 唯一執行佇列＋完整 54 項 PB 台帳＋子切片；未排程工作不具有順位 |
| Stories/ | 各切片範圍、依賴、驗收與證據連結 |
| [Design/Run-Lifecycle](Design/Run-Lifecycle.md) | E3 跨 Scene 重開、單局清理與未來正式主選單 UI 邊界 |
| [Design/HUD](Design/HUD.md) | UX、畫面資訊與互動；不重訂玩法公式 |
| [Design/Art-Guide](Design/Art-Guide.md) | 已確認美術方向與資產規格入口 |
| Architecture/ | ADR 與其狀態；決策接受不等於實作驗收 |
| [ScriptCodeReview/E2 API](ScriptCodeReview/E2_API_Review.md) | 實際程式接口、關係圖與版本驗證界線 |
| [Technical-Debt](Technical-Debt.md) | HUD-001 等已知問題與擴充候選；不另排隊 |
| [Development-Log](Development-Log.md) | 現況與已確認決策；不另寫下一步 |
| [Document-Audit](Document-Audit.md) | 去重／衝突處理與全部來源對照 |
| Playtest/ | 工具新產生的測試報告；不把排程模擬當實際遊玩 |
| Abandoned-plan/ | 原文、原 ID、歷史證據及空白工作簿，僅供追溯 |

使用者本次要求：GDD 管玩法，Design 管 UX／美術，Story 管實作／驗收；重複段落改用摘要與引用。衝突先看明確的新決策，不以檔名或修改時間猜測。沒有決定的規則標待定，不補造玩法。每個大進度完成後更新 ScriptCodeReview API 文件並附關係圖、問題、驗證及後續工作。

原 Assets/design、Assets/Docs、Assets/production 已移到 Abandoned-plan 同名子目錄；原相對位置與 .meta 保留。原文件中的路徑、Status、Next 與操作敘述屬當時紀錄，不是目前指令。未刪除測試證據、未將被中斷的驗收改為通過，也未回滾使用者程式／場景調整。

E3 現階段驗收使用獨立主選單占位 Scene；SampleScene 是遊戲內場景。正式主選單另見 Stories/PB-P3-005.md，不是 E3 的前置。所有 Story 仍是文件狀態，不能據此認定 Scene 已建立。
