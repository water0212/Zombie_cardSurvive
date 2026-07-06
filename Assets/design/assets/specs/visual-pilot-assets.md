# Asset Specs - System: Visual Pilot

> **Source**: design/art/art-bible.md, design/gdd/game-concept.md
> **Art Bible**: design/art/art-bible.md
> **Generated**: 2026-07-05
> **Review Mode**: Full
> **Status**: 6 assets specced / 5 need redesign / 0 in production / 1 done

> **V2 Revision 2026-07-06:** 卡牌改採徽記式剪影，事件與地區改採場景式
> 剪影。ASSET-003 V2 是目前唯一正式基準；其餘舊提示詞與輸出僅供歷史比較，
> 不可繼續用於正式生產。

本文件定義第一批六件視覺試產資產。每件資產必須以單獨一次生圖呼叫製作，
不得生成接觸表、拼貼或內含多個候選版本的單張圖片。

## 共通美術規格

- 風格為粗墨孔版套色的「末日勤務手冊」插畫。
- 世界為災變後 1 至 3 年的北美五大湖鏽帶虛構城市。
- 固定色盤為檔案紙 #E6DDC5、炭黑墨 #252622、結構灰 #5E625D，
  並且最多再使用兩種指定彩墨。
- 使用乾澀閉合粗線、平面大色塊、45 度單色網點與克制磨損。
- 禁止漸層、柔光、照片噪點、假文字、浮水印、額外色彩和具名藝術家風格。
- 血腥尺度為低至中度；允許撕裂衣物、紅墨污跡和破裂輪廓，
  禁止傷口細節、內臟、寫實血肉及肢解斷面。

## 共通 Unity 匯入規格

- Texture Type：Sprite (2D and UI)
- Sprite Mode：Single
- Color Space：sRGB
- Wrap Mode：Clamp
- Read/Write：Off
- Generate Physics Shape：Off
- Mesh Type：Full Rect
- Mipmap：Off
- Filter Mode：Bilinear
- Compression：None
- 透明素材使用 straight alpha、Input Texture Alpha 與 Alpha Is Transparency On。
- 不透明素材使用 RGB PNG，Alpha Source None。
- PPU 100 與 Atlas 名稱為本次試產建議，尚未定義為全專案正式標準。

## ASSET-001 - 廢棄超市

| Field | Value |
|---|---|
| Category | Environment / Region Illustration |
| Work Size | 1064×856 px |
| Unity Size | 266×214 px |
| Aspect | 133:107 |
| Format | RGB 8-bit sRGB PNG |
| Alpha | No |
| Naming | environment_abandoned_supermarket.png |
| Max Size | 512 |
| Atlas | atlas_card_art_visual_pilot |
| Status | Needs Redesign |
| Archived Output | `Art/VisualPilot/Regions/environment_abandoned_supermarket.png` |

**Visual Description**

低矮箱型超市是唯一主地標，破損入口雨棚與停車格線將視線由前景引向
昏暗入口。前景只保留傾斜購物車棚，中景為超市，遠景為冷清天空；
補給黃暗示物資，入口附近以極少感染紅留下風險。

**Art Bible Anchors**

- Section 1.2：每份希望都必須留下風險痕跡。
- Section 2.3：探索地區選擇。
- Section 3.4：地區主地標與三層構圖。
- Sections 4.1、4.6、4.11 至 4.17：色盤、地區色溫與生圖規格。

**Generation Prompt**

> 單張橫式探索地區插畫，粗墨孔版套色的「末日勤務手冊」風格。
> 災變後 1 至 3 年、北美五大湖鏽帶城市郊區的廢棄超市；低矮箱型建築
> 為唯一主地標，占畫面約 60%，入口雨棚斷裂一角，無文字招牌；
> 前景停車格線形成通往入口的單一視覺動線，最多加入一座傾斜購物車棚
> 和一輛廢棄皮卡。平視、輕微四分之三視角，前景停車場、中景超市、
> 遠景天空三層；天空與左側停車場保留至少 30% 連續負空間。
> 檔案紙 #E6DDC5、炭黑墨 #252622、結構灰 #5E625D，
> 彩墨僅補給黃 #C79A24 與極少感染紅 #B53A32。
> 乾澀粗線、平面大色塊、45 度單色網點、克制邊緣磨損、清晨冷光，
> 無漸層、無文字、無卡框。

**Avoid**

商場全景、成排小店、人物群、殭屍群、可讀招牌、過多車輛、乾淨營業狀態、
戲劇廣角、照片感、柔光、假文字和額外色彩。

**Acceptance**

- 一秒內辨識為超市。
- 只有一個主地標與最多兩個支撐物。
- 連續留白至少 30%，風險痕跡不遮蔽地標。
- 縮至 133×107 仍可辨識入口與視覺動線。

**Technical Notes**

主要地標位於中央 85%，四邊各保留約 7.5% 可犧牲裁切區。可供地區選擇
與地點卡圖共用；若未來新增大型地區詳情頁，需另訂較高輸出級距。

## ASSET-002 - 普通殭屍

| Field | Value |
|---|---|
| Category | Character / Threat Portrait |
| Work Size | 1024×1024 px |
| Unity Size | 256×256 px |
| Aspect | 1:1 |
| Format | RGBA 8-bit sRGB PNG |
| Alpha | Yes |
| Naming | character_common_zombie_portrait.png |
| Max Size | 256 |
| Atlas | atlas_shared_portraits_visual_pilot |
| Status | Needs Redesign |
| Archived Output | `Art/VisualPilot/Characters/character_common_zombie_portrait.png` |

**Visual Description**

方形胸像以頭部前伸、中度 C 形脊線、高低肩和單側下垂手臂建立普通殭屍
剪影。破舊工作外套只有一處鋸齒缺口，感染紅只作少量乾燥墨污，
不描繪傷口內部。

**Art Bible Anchors**

- Section 2.4：殭屍威脅與戰鬥配置。
- Sections 3.1、3.3、3.8：破裂剪影與灰階辨識。
- Sections 4.1、4.3、4.11 至 4.17：感染紅、血腥尺度與透明素材規格。

**Generation Prompt**

> 單張 1:1 普通殭屍肖像素材，粗墨孔版套色的「末日勤務手冊」風格。
> 單一殭屍胸像，頭部向前偏離肩軸，中度 C 形背部，左右肩不同高，
> 單臂下垂，頭部是剪影最前端的小凸塊；破舊工作外套只有一處鋸齒缺口，
> 低至中度血腥。平視、輕微四分之三視角，主體占 68%，
> 頭部與肩膀周圍保留乾淨空間。主體只使用檔案紙 #E6DDC5、
> 炭黑墨 #252622、結構灰 #5E625D、感染紅 #B53A32。
> 背景必須是完全均勻、無紋理、無陰影、可移除的純色鍵色 #00FF00；
> 主體不得出現任何綠色或綠色溢邊。乾澀閉合粗輪廓、平面色塊、
> 45 度網點、無文字、無框。

**Avoid**

直立對稱姿勢、穩定雙肩、英雄姿態、張嘴特寫、寫實腐肉、外露骨骼、
內臟、斷肢、多餘肢體、複雜衣紋、背景物件與綠色反光。

**Acceptance**

- 灰階和 66×54 縮圖仍可辨識為殭屍。
- 高低肩、前伸頭部和 C 形背部都清楚。
- 沒有傷口內部、寫實血肉或錯誤肢體。
- 鍵色完全平坦，去背後沒有綠色溢邊。

**Technical Notes**

四邊至少保留 8% 透明空間，供殭屍卡與 100×100 基地威脅共用。
現有 CardView 的 Preserve Aspect 為關閉，正方形圖放進橫式卡圖槽會變形；
使用前必須改為等比例顯示，或另製橫向殭屍卡圖。

## ASSET-003 - 烹飪

| Field | Value |
|---|---|
| Category | Card Illustration |
| Work Size | 1064×856 px |
| Unity Size | 266×214 px |
| Aspect | 133:107 |
| Format | RGB 8-bit sRGB PNG |
| Alpha | No |
| Naming | card_cooking.png |
| Max Size | 512 |
| Atlas | atlas_card_art_visual_pilot |
| Status | Done |
| Selected Output | `Art/VisualPilot/Cards/card_cooking_silhouette_v2.png` |
| Visual Core | V2 emblem silhouette reference |

**Visual Description**

一名完整直立的倖存者以四分之三側姿攪拌大鍋，長柄勺形成清楚斜線。
人物、大鍋與勺構成單一動作群，背景只保留腰高工作面和少量結構灰。

**Art Bible Anchors**

- Section 1.1：規則必須像命令一樣先被看懂。
- Section 2.5：製作完成的踏實進展。
- Sections 3.7、4.1 至 4.3、4.9、4.11 至 4.17：卡圖預算與補給黃。

**Generation Prompt**

> 單張 266×214 橫式卡牌插畫母稿，不含卡框與文字，
> 粗墨孔版套色的「末日勤務手冊」風格。一名倖存者直立於簡化工作面後，
> 以長柄勺攪拌一只大鍋；人物為唯一主體，道具只有大鍋與勺，
> 蒸氣以兩條粗弧線表示。平視、輕微四分之三側面，
> 主體動作群占 62%，前景無物件，中景人物與鍋，
> 背景只有一塊低對比結構灰；右上方保留至少 25% 連續負空間。
> 檔案紙 #E6DDC5、炭黑墨 #252622、結構灰 #5E625D，
> 唯一彩墨為補給黃 #C79A24。乾澀粗線、平面色塊、45 度網點、
> 暖色工作感、克制磨損、無漸層、無文字、無邊框。

**Avoid**

第二人物、食材堆、餐具群、完整廚房背景、火焰爆發、魔法光效、照片食物、
細密磚紋、招牌、食譜文字、卡框、圖示和標題。

**Acceptance**

- 縮至 133×107 時一秒內辨識為烹飪。
- 只有一人、兩件道具與最多三層景深。
- 主體占 55% 至 70%，連續留白至少 25%。
- 只使用補給黃彩墨，沒有卡框或文字。

**Technical Notes**

重要內容距四邊至少 7.5%。針對目前 132.7×107.2 非整數 Canvas 縮放使用
Bilinear，並確認卡框遮罩不會吃掉關鍵輪廓。

## ASSET-004 - 工作台

| Field | Value |
|---|---|
| Category | Building Sprite |
| Work Size | 1024×1024 px |
| Unity Size | 256×256 px |
| Aspect | 1:1 |
| Format | RGBA 8-bit sRGB PNG |
| Alpha | Yes |
| Naming | building_workbench.png |
| Max Size | 256 |
| Atlas | atlas_base_buildings_visual_pilot |
| Status | Needs Redesign |
| Archived Output | `Art/VisualPilot/Buildings/building_workbench.png` |

**Visual Description**

方形建築素材呈現完成並運作中的小型工作坊模組，而不是孤立家具。
封閉矩形基座、工作間核心體塊與頂部工具架構成三段式剪影，
入口與功能頂件一眼可讀。

**Art Bible Anchors**

- Section 1.3：長期建設是藍圖。
- Sections 2.1、2.5：基地運作與踏實進展。
- Sections 3.1、3.4、4.1、4.3、4.11 至 4.17：建築模組與藍圖藍。

**Generation Prompt**

> 單張 1:1 基地工作台建築素材，粗墨孔版套色的「末日勤務手冊」風格。
> 一座完成、運作中的緊湊工作坊模組：完整矩形基座、封閉工作間核心體塊、
> 頂部單一工具架功能件；正面入口清楚，工具與檯面整合進建築剪影，
> 不散落零件，無人物。固定正交四分之三視角，無透視收斂，
> 主體占 72%，四周保留均勻切圖空間。主體只使用檔案紙 #E6DDC5、
> 炭黑墨 #252622、結構灰 #5E625D、藍圖藍 #2F7185。
> 背景必須是完全均勻、無紋理、無陰影、可移除的純色鍵色 #00FF00；
> 主體不得出現任何綠色或綠色溢邊。完整閉合輪廓、平面大色塊、
> 少量 45 度網點、無文字。

**Avoid**

單獨桌子、工具雜物牆、散落螺絲、人物、施工鷹架、破敗輪廓、歪斜建築、
等角遊戲地板、投影、透視畸變、假標牌與綠色反光。

**Acceptance**

- 縮至 72×72 仍能辨識為建築型工作台。
- 基座、核心體塊和功能頂件完整。
- 剪影方正閉合，不使用施工中的虛線語言。
- 鍵色平坦，去背後無投影或綠色溢邊。

**Technical Notes**

主體占畫面約 80%，四邊保留 10% 透明空間。現有 BuildingWorldView 使用
Canvas Image；若未來改用 SpriteRenderer，PPU 必須依基地世界單位重新決定。

## ASSET-005 - 探索者倖存者

| Field | Value |
|---|---|
| Category | Character Portrait |
| Work Size | 1024×1024 px |
| Unity Size | 256×256 px |
| Aspect | 1:1 |
| Format | RGBA 8-bit sRGB PNG |
| Alpha | Yes |
| Naming | character_explorer_survivor_bust.png |
| Max Size | 256 |
| Atlas | atlas_survivor_portraits_visual_pilot |
| Status | Needs Redesign |
| Archived Output | `Art/VisualPilot/Characters/character_explorer_survivor_bust.png` |

**Visual Description**

方形胸像呈現窄身、完整直立且略向前的探索者，肩線清楚、重心穩定。
背包形成背部圓凸，一根短撬棍斜跨胸前並連入主剪影；
臉部只保留三至五個墨色記號。

**Art Bible Anchors**

- Section 1.1：資訊先被看懂。
- Sections 3.1、3.2、3.8：完整倖存者剪影與縮圖辨識。
- Sections 4.1、4.3、4.11 至 4.17：未知紫與透明素材規格。

**Generation Prompt**

> 單張 1:1 探索者倖存者胸像，粗墨孔版套色的「末日勤務手冊」風格。
> 單一成人探索者，窄身直立、輕微向前但不駝背，頭與軀幹完整相連，
> 肩線清楚；緊貼身體的背包形成背部圓凸，唯一主要工具是一根斜跨胸前
> 的短撬棍。平視、輕微四分之三正面，主體占 68%，頭部、背包與工具
> 周圍保留清楚間隔。主體只使用檔案紙 #E6DDC5、炭黑墨 #252622、
> 結構灰 #5E625D、未知紫 #685B86。
> 背景必須是完全均勻、無紋理、無陰影、可移除的純色鍵色 #00FF00；
> 主體不得出現任何綠色或綠色溢邊。乾澀粗輪廓、平面色塊、
> 少量網點、低臉部細節、無文字。

**Avoid**

殭屍 C 形姿勢、高低肩、斷裂輪廓、多把武器、步槍、巨大背包、
背包零件展示、英雄披風、動漫臉、寫實皮膚、背景場景與綠色反光。

**Acceptance**

- 縮至 66×54 時仍能從背包與斜向工具辨識探索者。
- 人形完整穩定，不與殭屍剪影混淆。
- 只有一件主要工具。
- 鍵色平坦，角色與服裝完全不含綠色。

**Technical Notes**

四邊保留 8% 至 10% 透明空間。正式倖存者 UI 的顯示尺寸、胸像底部切線
及統一 framing 尚未定案，本資產先作為職業剪影基準。

## ASSET-006 - 末日檔案館

| Field | Value |
|---|---|
| Category | UI Background |
| Work Size | 3840×2160 px |
| Unity Size | 1920×1080 px |
| Aspect | 16:9 |
| Format | RGB 8-bit sRGB PNG |
| Alpha | No |
| Naming | background_main_menu_apocalypse_archive.png |
| Max Size | 2048 |
| Atlas | None - load independently |
| Status | Needs Redesign |
| Archived Output | `Art/VisualPilot/UI/background_main_menu_apocalypse_archive.png` |

**Visual Description**

畫面右側集中高書架、舊書桌、攤開的記錄冊與年長保管者，人物以側背面
呈現並保持身分不明。左側至少 40% 是低對比、連續乾淨的牆面與桌面區，
供主選單 UI 使用。

**Art Bible Anchors**

- Section 1：末日檔案館與紀錄保管者。
- Section 3.1：資訊輪廓優先。
- Sections 4.1、4.9、4.11、4.13 至 4.17：限定色、主選單與生圖規格。

**Generation Prompt**

> 單張 16:9 PC 主選單背景，粗墨孔版套色的「末日勤務手冊」風格。
> 安靜的私人檔案室：畫面右側 60% 集中高書架、舊木書桌和數本無字
> 記錄冊；一名年長紀錄保管者坐在或站於桌旁，以側背面呈現，
> 臉部被帽緣或陰影簡化，身分保持模糊，不看鏡頭。
> 左側至少 40% 保留為連續、低細節、低對比的乾淨選單區，
> 不得有角色、書脊、燈具或高對比線條穿越。
> 平視廣角但無透視誇張，前景桌緣、中景老人與書桌、後景書架三層。
> 檔案紙 #E6DDC5、炭黑墨 #252622、結構灰 #5E625D，
> 唯一彩墨為補給黃 #C79A24；工作燈以平面黃色塊表示。
> 乾澀粗線、平面大色塊、45 度網點、克制紙張磨損，
> 無漸層、無柔光、無任何文字或選單圖形。

**Avoid**

可讀書名、書脊文字、選單文字、巨大人物特寫、正面清楚肖像、英雄姿勢、
恐怖地窖、哥德裝飾、燭光光暈、滿版書架、左側雜物、高對比 UI 區與照片感。

**Acceptance**

- 左側連續乾淨 UI 區至少 35%。
- 書架、書桌、記錄冊與老人都可辨識。
- 老人身分模糊且不是畫面視覺主角。
- 沒有任何可讀或假文字、柔光與漸層。

**Technical Notes**

外圍 5% 不放關鍵元素，主選單背景獨立載入並在離開畫面後卸載。
目前 SampleScene Canvas 使用 Constant Pixel Size；正式主選單仍需決定
16:9、16:10、超寬裁切、4K 支援與 CanvasScaler 策略。

## 已知技術缺口

1. 普通殭屍的正方形肖像與 CardView 橫式圖槽比例不一致。
2. 主選單尚未建立響應式裁切與 CanvasScaler 規格。
3. PPU 100、Sprite Atlas 分組與壓縮策略目前只適用於本次試產。
4. 倖存者肖像尚未有正式 UI 顯示尺寸和統一胸像切線。
