# Card Art Demo

This folder contains first-pass card front demos based on `Docs/GameDesign/ZombieSurvivalCard_GDD.md`.

## Output

- `card_demo_contact_sheet.png`: overview sheet for quick review.
- `card_fronts/`: finished demo card fronts.
- `card_base_contact_sheet.png`: overview sheet for blank structural card bases.
- `card_bases/`: blank card bases with no text, no illustration, and no energy number.
- `illustrations/`: raw generated illustration crops used by the card fronts.
- `build_card_demos.py`: local compositor for rebuilding the card fronts from the generated illustrations.
- `build_card_bases.py`: local compositor for rebuilding the blank structural card bases.

## Demo Cards

- `combat_wooden_bat_card.png`: 木棍, battle card, 1 action, 2 distributable damage.
- `food_canned_food_card.png`: 罐頭, food card, 1 action, gain 2 food.
- `resource_scavenge_scrap_card.png`: 搜刮廢料, resource card, 1 action, gain 2 resource.
- `zombie_alley_threat_card.png`: 巷口威脅, zombie/threat card, triggers when drawn.
- `event_suburban_houses_card.png`: 郊區住宅, low-risk exploration event.
- `event_supermarket_run_card.png`: 超市補給, high food reward with deck pollution.
- `event_police_station_card.png`: 警局軍火, combat/equipment reward with stronger threat.
- `event_food_spoilage_card.png`: 食物腐敗, crisis event targeting food pressure.

## Visual Direction

- Mood: gritty zombie-apocalypse survival, tense but not excessively graphic.
- Layout: portrait card frame with title, cost badge, art window, type strip, and rules text.
- Palette: muted survival colors with card-type accent colors.
- Text: Traditional Chinese rules text is composited locally for readability.
