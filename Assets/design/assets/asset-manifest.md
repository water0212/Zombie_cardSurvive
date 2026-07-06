# Asset Manifest

> Last updated: 2026-07-06

## Progress Summary

| Total | Needed | In Progress | Done | Approved |
|---:|---:|---:|---:|---:|
| 6 | 5 | 0 | 1 | 0 |

## Assets by Context

### System: Visual Pilot

| Asset ID | Name | Category | Status | Spec File |
|---|---|---|---|---|
| ASSET-001 | 廢棄超市 | Environment / Region Illustration | Needs Redesign | design/assets/specs/visual-pilot-assets.md |
| ASSET-002 | 普通殭屍 | Character / Threat Portrait | Needs Redesign | design/assets/specs/visual-pilot-assets.md |
| ASSET-003 | 烹飪 | Card Illustration | Done | design/assets/specs/visual-pilot-assets.md |
| ASSET-004 | 工作台 | Building Sprite | Needs Redesign | design/assets/specs/visual-pilot-assets.md |
| ASSET-005 | 探索者倖存者 | Character Portrait | Needs Redesign | design/assets/specs/visual-pilot-assets.md |
| ASSET-006 | 末日檔案館 | UI Background | Needs Redesign | design/assets/specs/visual-pilot-assets.md |

## Technical Follow-ups

- 新視覺核心：卡牌使用徽記式剪影；事件與地區使用場景式剪影。
- ASSET-003 已鎖定：`Art/VisualPilot/Cards/card_cooking_silhouette_v2.png`。
- ASSET-001、002、004、005、006 的現有圖檔保留為舊畫風試作，不可作為正式基準。
- ASSET-002：正方形殭屍肖像不可直接拉伸至橫式 CardView 圖槽。
- ASSET-006：正式主選單需定義 16:9、16:10、超寬與 4K 裁切策略。
- PPU、Atlas 分組與壓縮策略目前為 Visual Pilot 暫定值。
- 六件試產完成後仍須執行 asset audit 與 Unity 實機縮圖驗證。
