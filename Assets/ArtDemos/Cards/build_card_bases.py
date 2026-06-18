from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parent
BASES = ROOT / "card_bases"

ITEMS = [
    ("combat_wooden_bat", (155, 54, 48)),
    ("food_canned_food", (69, 128, 82)),
    ("resource_scavenge_scrap", (136, 105, 63)),
    ("zombie_alley_threat", (95, 78, 130)),
    ("event_suburban_houses", (73, 111, 125)),
    ("event_supermarket_run", (84, 124, 94)),
    ("event_police_station", (60, 82, 137)),
    ("event_food_spoilage", (113, 93, 56)),
]

CARD_W, CARD_H = 750, 1050


def blend(a, b, t):
    return tuple(int(a[i] * (1 - t) + b[i] * t) for i in range(3))


def draw_empty_art(draw: ImageDraw.ImageDraw, box, accent):
    x1, y1, x2, y2 = box
    height = y2 - y1
    for i in range(height):
        t = i / max(1, height - 1)
        color = blend((30, 32, 32), tuple(max(0, c - 45) for c in accent), t * 0.45)
        draw.line([(x1, y1 + i), (x2, y1 + i)], fill=color)

    draw.rectangle(box, outline=(226, 216, 188), width=4)
    inset = 18
    draw.rounded_rectangle(
        (x1 + inset, y1 + inset, x2 - inset, y2 - inset),
        radius=12,
        outline=blend((226, 216, 188), accent, 0.35),
        width=2,
    )


def build_base(slug: str, accent: tuple[int, int, int]):
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

    cost_fill = tuple(min(255, c + 32) for c in accent)
    draw.ellipse(
        (CARD_W - 152, 55, CARD_W - 62, 145),
        fill=cost_fill,
        outline=(235, 226, 198),
        width=3,
    )

    draw_empty_art(draw, (60, 176, 690, 696), accent)

    draw.rounded_rectangle(
        (60, 718, CARD_W - 60, 768),
        radius=12,
        fill=accent,
        outline=(226, 216, 188),
        width=2,
    )

    draw.rounded_rectangle(
        (60, 790, CARD_W - 60, 982),
        radius=14,
        fill=(228, 222, 204),
        outline=(71, 68, 60),
        width=3,
    )

    card.save(BASES / f"{slug}_base.png", quality=95)


def build_contact_sheet():
    thumb_w, thumb_h = 300, 420
    sheet = Image.new("RGB", (thumb_w * 4 + 50, thumb_h * 2 + 45), (22, 24, 25))
    for idx, (slug, _) in enumerate(ITEMS):
        img = Image.open(BASES / f"{slug}_base.png").convert("RGB")
        img.thumbnail((thumb_w, thumb_h), Image.Resampling.LANCZOS)
        x = 10 + (idx % 4) * (thumb_w + 10)
        y = 10 + (idx // 4) * (thumb_h + 15)
        sheet.paste(img, (x, y))
    sheet.save(ROOT / "card_base_contact_sheet.png", quality=95)


def main():
    BASES.mkdir(parents=True, exist_ok=True)
    for slug, accent in ITEMS:
        build_base(slug, accent)
    build_contact_sheet()
    print(f"Created {len(ITEMS)} card bases")
    print(BASES)
    print(ROOT / "card_base_contact_sheet.png")


if __name__ == "__main__":
    main()
