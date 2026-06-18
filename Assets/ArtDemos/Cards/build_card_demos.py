from pathlib import Path
import shutil

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parent
GENERATED = Path(
    r"C:\Users\water\.codex\generated_images\019ecf1b-8dd9-7271-b55a-223ded869c06"
)
ILLUSTRATIONS = ROOT / "illustrations"
FRONTS = ROOT / "card_fronts"

ITEMS = [
    {
        "slug": "combat_wooden_bat",
        "title": "木棍",
        "type": "戰鬥卡",
        "cost": "1",
        "subtitle": "造成 2 點傷害",
        "body": "本回合獲得 2 點可分配傷害。可分配給任意殭屍威脅。",
        "color": (155, 54, 48),
        "icon": "DMG",
    },
    {
        "slug": "food_canned_food",
        "title": "罐頭",
        "type": "食物卡",
        "cost": "1",
        "subtitle": "獲得 2 食物",
        "body": "打出後獲得 2 食物。簡單、可靠，能撐過下一輪消耗。",
        "color": (69, 128, 82),
        "icon": "FOOD",
    },
    {
        "slug": "resource_scavenge_scrap",
        "title": "搜刮廢料",
        "type": "資源卡",
        "cost": "1",
        "subtitle": "獲得 2 資源",
        "body": "獲得 2 資源。可用於建築、製作或升級據點。",
        "color": (136, 105, 63),
        "icon": "RES",
    },
    {
        "slug": "zombie_alley_threat",
        "title": "巷口威脅",
        "type": "殭屍卡",
        "cost": "!",
        "subtitle": "抽到時生成威脅",
        "body": "抽到此卡時，將它移出卡組並生成 1 個殭屍威脅。輪末仍存活則造成壓力。",
        "color": (95, 78, 130),
        "icon": "THREAT",
    },
    {
        "slug": "event_suburban_houses",
        "title": "郊區住宅",
        "type": "事件卡",
        "cost": "DAY",
        "subtitle": "低風險探索",
        "body": "獲得 1 張基礎補給卡，並獲得 1 資源。可選擇加入 1 張危險卡以額外獲得 2 食物。",
        "color": (73, 111, 125),
        "icon": "EVENT",
    },
    {
        "slug": "event_supermarket_run",
        "title": "超市補給",
        "type": "事件卡",
        "cost": "DAY",
        "subtitle": "高食物收益",
        "body": "從 3 張食物卡中選 1 張加入卡組。獲得 2 食物，然後加入 1 張殭屍污染卡。",
        "color": (84, 124, 94),
        "icon": "EVENT",
    },
    {
        "slug": "event_police_station",
        "title": "警局軍火",
        "type": "事件卡",
        "cost": "DAY",
        "subtitle": "武器與彈藥",
        "body": "從 3 張戰鬥或裝備卡中選 1 張。生成 1 個較強威脅，若清除則額外獲得 1 彈藥。",
        "color": (60, 82, 137),
        "icon": "EVENT",
    },
    {
        "slug": "event_food_spoilage",
        "title": "食物腐敗",
        "type": "危機事件",
        "cost": "DAY",
        "subtitle": "資源壓力",
        "body": "失去 2 食物。若食物不足，改為失去 2 生命，並在下輪開始時少抽 1 張牌。",
        "color": (113, 93, 56),
        "icon": "CRISIS",
    },
]


def load_font(path: Path, size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(path), size)


FONT_REGULAR = Path(r"C:\Windows\Fonts\NotoSansTC-VF.ttf")
FONT_BOLD = Path(r"C:\Windows\Fonts\msjhbd.ttc")
if not FONT_REGULAR.exists():
    FONT_REGULAR = Path(r"C:\Windows\Fonts\msjh.ttc")

F_TITLE = load_font(FONT_BOLD, 58)
F_TYPE = load_font(FONT_REGULAR, 28)
F_COST = load_font(FONT_BOLD, 34)
F_SUB = load_font(FONT_BOLD, 30)
F_BODY = load_font(FONT_REGULAR, 30)
F_SMALL = load_font(FONT_REGULAR, 22)

CARD_W, CARD_H = 750, 1050


def fit_crop(img: Image.Image, size: tuple[int, int]) -> Image.Image:
    target_w, target_h = size
    image_w, image_h = img.size
    scale = max(target_w / image_w, target_h / image_h)
    resized = img.resize(
        (int(image_w * scale), int(image_h * scale)), Image.Resampling.LANCZOS
    )
    left = (resized.width - target_w) // 2
    top = (resized.height - target_h) // 2
    return resized.crop((left, top, left + target_w, top + target_h))


def wrap_text(draw: ImageDraw.ImageDraw, text: str, font: ImageFont.FreeTypeFont, max_w: int):
    lines = []
    current = ""
    for char in text:
        test = current + char
        if draw.textbbox((0, 0), test, font=font)[2] <= max_w:
            current = test
        else:
            if current:
                lines.append(current)
            current = char
    if current:
        lines.append(current)
    return lines


def centered_text(draw, box, text, font, fill):
    x1, y1, x2, y2 = box
    bbox = draw.textbbox((0, 0), text, font=font)
    draw.text(
        (x1 + (x2 - x1 - (bbox[2] - bbox[0])) / 2, y1 + (y2 - y1 - (bbox[3] - bbox[1])) / 2),
        text,
        font=font,
        fill=fill,
    )


def build_card(src: Path, meta: dict):
    raw = ILLUSTRATIONS / f"{meta['slug']}_art.png"
    shutil.copy2(src, raw)

    accent = meta["color"]
    art_src = Image.open(raw).convert("RGB")
    card = Image.new("RGB", (CARD_W, CARD_H), (20, 22, 24))
    draw = ImageDraw.Draw(card)

    for y in range(CARD_H):
        t = y / CARD_H
        base = tuple(int(18 * (1 - t) + accent[i] * 0.20 * t) for i in range(3))
        draw.line([(0, y), (CARD_W, y)], fill=base)

    draw.rounded_rectangle(
        (18, 18, CARD_W - 18, CARD_H - 18),
        radius=34,
        fill=(28, 30, 31),
        outline=accent,
        width=6,
    )
    draw.rounded_rectangle(
        (38, 38, CARD_W - 38, CARD_H - 38),
        radius=24,
        fill=(37, 38, 38),
        outline=(78, 78, 74),
        width=2,
    )

    draw.rounded_rectangle(
        (60, 58, CARD_W - 60, 150),
        radius=18,
        fill=(28, 28, 27),
        outline=accent,
        width=3,
    )
    draw.text((88, 72), meta["title"], font=F_TITLE, fill=(237, 233, 220))

    cost_fill = tuple(min(255, c + 32) for c in accent)
    draw.ellipse(
        (CARD_W - 152, 55, CARD_W - 62, 145),
        fill=cost_fill,
        outline=(235, 226, 198),
        width=3,
    )
    centered_text(
        draw,
        (CARD_W - 152, 55, CARD_W - 62, 145),
        meta["cost"],
        F_COST,
        (20, 20, 19),
    )

    art = fit_crop(art_src, (630, 520))
    card.paste(art, (60, 176))
    draw.rectangle((60, 176, 690, 696), outline=(226, 216, 188), width=4)

    draw.rounded_rectangle(
        (60, 718, CARD_W - 60, 768),
        radius=12,
        fill=accent,
        outline=(226, 216, 188),
        width=2,
    )
    draw.text((86, 727), meta["type"], font=F_TYPE, fill=(246, 241, 224))
    icon_bbox = draw.textbbox((0, 0), meta["icon"], font=F_SMALL)
    draw.text(
        (CARD_W - 86 - (icon_bbox[2] - icon_bbox[0]), 735),
        meta["icon"],
        font=F_SMALL,
        fill=(246, 241, 224),
    )

    draw.rounded_rectangle(
        (60, 790, CARD_W - 60, 982),
        radius=14,
        fill=(228, 222, 204),
        outline=(71, 68, 60),
        width=3,
    )
    draw.text((86, 812), meta["subtitle"], font=F_SUB, fill=(38, 36, 31))

    y = 858
    for line in wrap_text(draw, meta["body"], F_BODY, CARD_W - 172)[:4]:
        draw.text((86, y), line, font=F_BODY, fill=(39, 38, 34))
        y += 39

    draw.text(
        (70, 1000),
        "Zombie Survival Card - demo",
        font=F_SMALL,
        fill=(154, 150, 136),
    )

    card.save(FRONTS / f"{meta['slug']}_card.png", quality=95)


def build_contact_sheet():
    thumb_w, thumb_h = 300, 420
    sheet = Image.new("RGB", (thumb_w * 4 + 50, thumb_h * 2 + 45), (22, 24, 25))
    for idx, meta in enumerate(ITEMS):
        img = Image.open(FRONTS / f"{meta['slug']}_card.png").convert("RGB")
        img.thumbnail((thumb_w, thumb_h), Image.Resampling.LANCZOS)
        x = 10 + (idx % 4) * (thumb_w + 10)
        y = 10 + (idx // 4) * (thumb_h + 15)
        sheet.paste(img, (x, y))
    sheet.save(ROOT / "card_demo_contact_sheet.png", quality=95)


def main():
    ILLUSTRATIONS.mkdir(parents=True, exist_ok=True)
    FRONTS.mkdir(parents=True, exist_ok=True)
    files = sorted(GENERATED.glob("*.png"), key=lambda p: p.stat().st_mtime)[-8:]
    if len(files) != len(ITEMS):
        raise RuntimeError(f"Expected {len(ITEMS)} generated images, found {len(files)}")
    for src, meta in zip(files, ITEMS):
        build_card(src, meta)
    build_contact_sheet()
    print(f"Created {len(ITEMS)} card fronts")
    print(FRONTS)
    print(ROOT / "card_demo_contact_sheet.png")


if __name__ == "__main__":
    main()
