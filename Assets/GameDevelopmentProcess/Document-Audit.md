# 文件盤點與重複內容處理

整理：2026-10-31。範圍為原 Assets/design、Assets/Docs、Assets/production：35 Markdown、2 YAML、1 Word、4 JSON、3 PNG，共 45 個非 .meta 檔。盤點文件分工、相關章節與狀態，不是程式驗收；Word 只做文字抽取，未做視覺／版面驗證，沒有修改 Word。

## 使用者已確認的決策

1. GDD 管玩法，Design 管 UX／美術，Story 管實作／驗收；重複細則採摘要與引用，保留歷史。
2. 新增唯一 Story 主線，保留既有 ID；父項與子項不重複做。
3. 取消高士氣全域事件 +10% 及自動提高好事件機率，統一由 Profile 定義。
4. 新文件集中 GameDevelopmentProcess，舊三個資料夾移入 Abandoned-plan。這是使用者要求的歸檔，沒有刪除歷史原文。

## 重複與衝突對照

| 問題 | 原位置 | 整理方式 |
|---|---|---|
| 遊戲概念重述 GDD、仍寫失敗未定與純天數勝利 | game-concept／GDD 舊章／handoff | 新 GDD 唯一玩法入口；選入新士氣、終局、固定三 Turn 規則，舊概念留歷史 |
| 卡種／欄位規則整段複製 | GDD §26.11、Backlog 尾段、game-concept、active、handoff | 正文集中新 GDD §26.11；新日誌／技術債只摘要連結 |
| 高士氣所有事件 +10% 與提高好事件機率 | GDD §20.6／24.4、PB-P1-007 | 使用者明確取消，新 GDD 寫決策；舊原文完整歸檔，PB-P1-007 改列效果邊界稽核 |
| 5n／n Turn 與固定三 Turn | 舊概念／GDD §21.1 對 §28.3 | 新 GDD 使用固定三 Turn；未導入牌組擴張 Runtime 前不宣稱已可切換 |
| 食物緊縮、溢出加士氣與新標準／鼓舞 | 舊 §20／24 對 §30.5、quick-spec 014B | 新 GDD 使用 §30；食物細則不再分散維護 |
| 一般動畫仍寫必須鎖操作 | hud.md 一般多數值變化 對 10/30 新決策 | 新 Design/HUD 更新該條；集中輪末與跨階鎖維持 |
| 014E／E2 還寫未實作 | Backlog、母計畫、早期 active 對 E2 Story／證據 | 新序列 E1 Complete、E2 Implemented/manual QA pending、E3 Planned；沒有把所有驗收勾滿 |
| 多個 Next 與過期 Blocked | active、handoff、Backlog 推薦順序、Sprint | 全部歸檔，只維護新 Story-Sequence |
| 事件仍寫一個 Ready／屍群尚未支援 | PB-P1-014A／015 對較新 009D | 標版本差異；009D 歷史紀錄為 3 playable，但本次未重查資產，不推斷今天精確數量；先盤點再續接 |
| 空白士氣 UX 模板 | morale-feedback-flow.md | 不作現行規格；已有內容集中新 HUD／E3 Story |
| 舊 Art Bible HUD 紫色禁綠 | art-bible 早期章與其暫停規則 | 新 Art-Guide 明確採 HUD Stage 色與形狀；不重新核准舊配色 |
| ADR 登錄不完整（缺 ADR-0003） | architecture.yaml 對已 Accepted 的 ADR-0003 | 新 Architecture 保留三份 ADR 狀態，由 README 列入口；舊 registry 歸檔，不用舊 YAML 作未核准判定 |
| 架構文件、API 與證據重述 E2 | 三份 E2 architecture、API、QA | 新 E2 API 作程式快照，舊架構／原始測試作歷史證據；數量與驗證版本不加總 |
| 工作簿尚有舊待問題目 | Word 問卷 | 抽取文字顯示未填答；當設計工具參考，不當已確認需求或另一個待辦清單 |

## 不是重複而應保留的資料

ADR 記錄「為何選擇」、Story 記錄「怎樣驗收」、JSON／截圖記錄「當時做過什麼」，不可因都有同個系統名字而刪掉。美術資產規格與 manifest 也有獨有細節，歸檔保留且可由新入口追溯；採用前需核對版本。事件表位於 Assets/outputs，本次不移動或重寫；個別內容數值仍按原 GDD §28.1 的資料分工。

## 遷移與相容性

原目錄與 .meta 成對搬移，不重新產生舊資產 GUID。新文件由 Unity 在後續刷新產生自己的 .meta。舊內文不改寫以保存原始雜湊；來源證據連結經根目錄追溯。唯一必要的程式文字調整是 EventScheduleSimulationUtility 報告輸出路徑，避免再生舊 Assets/Docs；未修改 Runtime／場景／玩法。

## 原始檔完整清單

以下每個來源均位於 Abandoned-plan，路徑可追溯原 Assets 下位置；原 SHA256 清單一併保存在本目錄 source-inventory.json，檔案移動後應與原雜湊相同。

| 原位置 | 歷史參考 |
|---|---|
| `Assets/production/session-state/active.md` | [production/session-state/active.md](Abandoned-plan/production/session-state/active.md) |
| `Assets/Docs/ScriptCodeReview/E2_API_Review.md` | [Docs/ScriptCodeReview/E2_API_Review.md](Abandoned-plan/Docs/ScriptCodeReview/E2_API_Review.md) |
| `Assets/design/ux/morale-feedback-flow.md` | [design/ux/morale-feedback-flow.md](Abandoned-plan/design/ux/morale-feedback-flow.md) |
| `Assets/design/ux/hud.md` | [design/ux/hud.md](Abandoned-plan/design/ux/hud.md) |
| `Assets/production/epics/PB-P0-014E-plan.md` | [production/epics/PB-P0-014E-plan.md](Abandoned-plan/production/epics/PB-P0-014E-plan.md) |
| `Assets/Docs/GameDesign/殭屍生存卡牌_離線設計決策工作簿.docx` | [Docs/GameDesign/殭屍生存卡牌_離線設計決策工作簿.docx](Abandoned-plan/Docs/GameDesign/殭屍生存卡牌_離線設計決策工作簿.docx) |
| `Assets/Docs/GameDesign/ZombieSurvivalCard_Scrum_Backlog.md` | [Docs/GameDesign/ZombieSurvivalCard_Scrum_Backlog.md](Abandoned-plan/Docs/GameDesign/ZombieSurvivalCard_Scrum_Backlog.md) |
| `Assets/Docs/GameDesign/ZombieSurvivalCard_GDD.md` | [Docs/GameDesign/ZombieSurvivalCard_GDD.md](Abandoned-plan/Docs/GameDesign/ZombieSurvivalCard_GDD.md) |
| `Assets/Docs/CodexHandoff/p0-gameplay-hud-integration-slice.md` | [Docs/CodexHandoff/p0-gameplay-hud-integration-slice.md](Abandoned-plan/Docs/CodexHandoff/p0-gameplay-hud-integration-slice.md) |
| `Assets/Docs/CodexHandoff/current-context.md` | [Docs/CodexHandoff/current-context.md](Abandoned-plan/Docs/CodexHandoff/current-context.md) |
| `Assets/production/epics/PB-P0-014E-E1.md` | [production/epics/PB-P0-014E-E1.md](Abandoned-plan/production/epics/PB-P0-014E-E1.md) |
| `Assets/Docs/Architecture/tr-registry.yaml` | [Docs/Architecture/tr-registry.yaml](Abandoned-plan/Docs/Architecture/tr-registry.yaml) |
| `Assets/Docs/Architecture/sample-e1-e2-integration.md` | [Docs/Architecture/sample-e1-e2-integration.md](Abandoned-plan/Docs/Architecture/sample-e1-e2-integration.md) |
| `Assets/Docs/Architecture/e2-value-transactions.md` | [Docs/Architecture/e2-value-transactions.md](Abandoned-plan/Docs/Architecture/e2-value-transactions.md) |
| `Assets/Docs/Architecture/e2-responsive-value-presentation.md` | [Docs/Architecture/e2-responsive-value-presentation.md](Abandoned-plan/Docs/Architecture/e2-responsive-value-presentation.md) |
| `Assets/Docs/Architecture/adr-0003-morale-preview-and-warning.md` | [Docs/Architecture/adr-0003-morale-preview-and-warning.md](Abandoned-plan/Docs/Architecture/adr-0003-morale-preview-and-warning.md) |
| `Assets/Docs/Architecture/adr-0002-building-bookmark-presentation.md` | [Docs/Architecture/adr-0002-building-bookmark-presentation.md](Abandoned-plan/Docs/Architecture/adr-0002-building-bookmark-presentation.md) |
| `Assets/Docs/Architecture/adr-0001-sprite-renderer-base-world-and-canvas-hud-boundary.md` | [Docs/Architecture/adr-0001-sprite-renderer-base-world-and-canvas-hud-boundary.md](Abandoned-plan/Docs/Architecture/adr-0001-sprite-renderer-base-world-and-canvas-hud-boundary.md) |
| `Assets/production/qa/evidence/pb-p0-014e-e2-collapse-final.png` | [production/qa/evidence/pb-p0-014e-e2-collapse-final.png](Abandoned-plan/production/qa/evidence/pb-p0-014e-e2-collapse-final.png) |
| `Assets/production/qa/evidence/pb-p0-014e-e1-evidence.md` | [production/qa/evidence/pb-p0-014e-e1-evidence.md](Abandoned-plan/production/qa/evidence/pb-p0-014e-e1-evidence.md) |
| `Assets/production/qa/evidence/building-bookmark-panel-evidence.md` | [production/qa/evidence/building-bookmark-panel-evidence.md](Abandoned-plan/production/qa/evidence/building-bookmark-panel-evidence.md) |
| `Assets/production/qa/evidence/pb-p0-014e-e2-stage-play.png` | [production/qa/evidence/pb-p0-014e-e2-stage-play.png](Abandoned-plan/production/qa/evidence/pb-p0-014e-e2-stage-play.png) |
| `Assets/production/qa/evidence/pb-p0-014e-e2-evidence.md` | [production/qa/evidence/pb-p0-014e-e2-evidence.md](Abandoned-plan/production/qa/evidence/pb-p0-014e-e2-evidence.md) |
| `Assets/production/qa/evidence/pb-p0-014e-e2-summary-final.png` | [production/qa/evidence/pb-p0-014e-e2-summary-final.png](Abandoned-plan/production/qa/evidence/pb-p0-014e-e2-summary-final.png) |
| `Assets/production/qa/evidence/pb-p0-014e-e2-tests.json` | [production/qa/evidence/pb-p0-014e-e2-tests.json](Abandoned-plan/production/qa/evidence/pb-p0-014e-e2-tests.json) |
| `Assets/production/qa/evidence/pb-p0-014e-responsive-values-tests.json` | [production/qa/evidence/pb-p0-014e-responsive-values-tests.json](Abandoned-plan/production/qa/evidence/pb-p0-014e-responsive-values-tests.json) |
| `Assets/Docs/Playtest/EventScheduleV1_30Day_Report.md` | [Docs/Playtest/EventScheduleV1_30Day_Report.md](Abandoned-plan/Docs/Playtest/EventScheduleV1_30Day_Report.md) |
| `Assets/production/qa/evidence/pb-p0-014e-sample-integration-evidence.md` | [production/qa/evidence/pb-p0-014e-sample-integration-evidence.md](Abandoned-plan/production/qa/evidence/pb-p0-014e-sample-integration-evidence.md) |
| `Assets/production/qa/evidence/pb-p0-014e-sample-integration-play.json` | [production/qa/evidence/pb-p0-014e-sample-integration-play.json](Abandoned-plan/production/qa/evidence/pb-p0-014e-sample-integration-play.json) |
| `Assets/production/qa/evidence/pb-p0-014e-sample-integration-tests.json` | [production/qa/evidence/pb-p0-014e-sample-integration-tests.json](Abandoned-plan/production/qa/evidence/pb-p0-014e-sample-integration-tests.json) |
| `Assets/design/art/art-bible.md` | [design/art/art-bible.md](Abandoned-plan/design/art/art-bible.md) |
| `Assets/design/gdd/game-concept.md` | [design/gdd/game-concept.md](Abandoned-plan/design/gdd/game-concept.md) |
| `Assets/production/epics/pb-p0-014e-e2/story-001-round-result-presentation-flow.md` | [production/epics/pb-p0-014e-e2/story-001-round-result-presentation-flow.md](Abandoned-plan/production/epics/pb-p0-014e-e2/story-001-round-result-presentation-flow.md) |
| `Assets/production/epics/pb-p0-014e-e2/story-002-morale-stage-transition-presentation.md` | [production/epics/pb-p0-014e-e2/story-002-morale-stage-transition-presentation.md](Abandoned-plan/production/epics/pb-p0-014e-e2/story-002-morale-stage-transition-presentation.md) |
| `Assets/production/epics/pb-p0-014e-e2/story-003-collapse-report-and-failed-lock.md` | [production/epics/pb-p0-014e-e2/story-003-collapse-report-and-failed-lock.md](Abandoned-plan/production/epics/pb-p0-014e-e2/story-003-collapse-report-and-failed-lock.md) |
| `Assets/production/epics/building-ui/BUILDING-UI-001.md` | [production/epics/building-ui/BUILDING-UI-001.md](Abandoned-plan/production/epics/building-ui/BUILDING-UI-001.md) |
| `Assets/Docs/Registry/architecture.yaml` | [Docs/Registry/architecture.yaml](Abandoned-plan/Docs/Registry/architecture.yaml) |
| `Assets/design/assets/asset-manifest.md` | [design/assets/asset-manifest.md](Abandoned-plan/design/assets/asset-manifest.md) |
| `Assets/design/quick-specs/pb-p2-009c-event-spawn-horde-2026-08-17.md` | [design/quick-specs/pb-p2-009c-event-spawn-horde-2026-08-17.md](Abandoned-plan/design/quick-specs/pb-p2-009c-event-spawn-horde-2026-08-17.md) |
| `Assets/design/quick-specs/pb-p0-014d-morale-pyramid-profile-2026-08-24.md` | [design/quick-specs/pb-p0-014d-morale-pyramid-profile-2026-08-24.md](Abandoned-plan/design/quick-specs/pb-p0-014d-morale-pyramid-profile-2026-08-24.md) |
| `Assets/design/quick-specs/pb-p0-014c-next-round-effects-2026-08-23.md` | [design/quick-specs/pb-p0-014c-next-round-effects-2026-08-23.md](Abandoned-plan/design/quick-specs/pb-p0-014c-next-round-effects-2026-08-23.md) |
| `Assets/design/quick-specs/pb-p0-014b-morale-settlement-2026-08-21.md` | [design/quick-specs/pb-p0-014b-morale-settlement-2026-08-21.md](Abandoned-plan/design/quick-specs/pb-p0-014b-morale-settlement-2026-08-21.md) |
| `Assets/design/quick-specs/pb-p0-014a-morale-collapse-core-2026-08-18.md` | [design/quick-specs/pb-p0-014a-morale-collapse-core-2026-08-18.md](Abandoned-plan/design/quick-specs/pb-p0-014a-morale-collapse-core-2026-08-18.md) |
| `Assets/design/assets/specs/visual-pilot-assets.md` | [design/assets/specs/visual-pilot-assets.md](Abandoned-plan/design/assets/specs/visual-pilot-assets.md) |
| `Assets/design/assets/specs/gameplay-hud-assets.md` | [design/assets/specs/gameplay-hud-assets.md](Abandoned-plan/design/assets/specs/gameplay-hud-assets.md) |

## 完成後核對

- 原 45 個非 .meta 文件／證據連同資料夾與檔案 .meta 共 111 個檔案，全部已歸檔且 SHA256 與搬移前一致。
- 新文件最初 66 個相對 Markdown 連結皆可定位；未改寫歷史原文以假造最新狀態。
- EventScheduleSimulationUtility.cs 與操作前備份逐字比較，只有 ReportAssetPath 與實際 Path.Combine 輸出的文件路徑字串改變。
- 本次未啟動 Unity Play、未重跑測試／模擬、未修改 Runtime、場景或套件；沒有宣稱新整理已通過遊戲驗收。


## PB 主線排序修正

先前未確認的 08～26 順位已撤回；現行 Story-Sequence 現已逐項列出原 54 個 PB 及子 Story。唯一執行佇列只到 E3 全閉環，其餘為未排程工作；不存在先欄位強化後事件的預設強制依賴。舊排序已另存歷史參考，不得覆蓋本次修正。


## 2026-11-02 E3 重開局與場景範圍確認

使用者要求 E3 作為重新開局手段與未來正式主畫面 UI 連接的部件／畫面；SampleScene 是遊戲內場景，主選單另存 Scene 並以切換 Scene 呈現。先以白畫面或簡易控制完成「士氣崩潰 → 返回主選單／整局清理 → 選設定 → 新遊戲」；使用者確認 Stage 指士氣 Profile。E3 四 Story 範圍已修訂，正式主選單 UI 拆至新 PB-P3-005（未排程、依賴 E3），不再把卡片布局與選單美術當 E3 前置。Run-Lifecycle 為現行邊界來源。此次只更新文件，沒有建立 Scene、改 Runtime 或執行新測試。
