# Event Content V1 - 30 Day Schedule Report

## Simulation Scope

- This is a schedule-only simulation. Event choices and gameplay effects are not executed.
- Canonical seed: `14014`.
- Canonical duration: `30` days.
- Distribution sample: `100` fixed-seed runs.
- Baseline morale is 50, building levels are 0, and MaxEnergy is 1/2/3 across days 1-10/11-20/21-30.
- Unsupported special rules and blocked event effects are intentionally ignored in this preliminary report.

## Canonical Sequence

| Day | Event ID | Event | Mood | Content | Final Weight | Eligible |
|---:|---|---|---|---|---:|---:|
| 1 | EVT-013 | 雲朗天清 | Positive | Effect | 10 | 2 |
| 2 | EVT-002 | 飢餓倖存者 | Mixed | Survivor | 40 | 2 |
| 3 | EVT-003 | 夜間低溫 | Mixed | Resource | 30 | 3 |
| 4 | EVT-009 | 路線錯判 | Mixed | Effect | 30 | 5 |
| 5 | EVT-005 | 工具損耗 | Negative | Resource | 37.5 | 7 |
| 6 | EVT-004 | 發現補給品 | Positive | Resource | 40 | 6 |
| 7 | EVT-013 | 雲朗天清 | Positive | Effect | 10 | 5 |
| 8 | EVT-001 | 食物受潮 | Negative | Resource | 15 | 6 |
| 9 | EVT-008 | 屍群逼近 | Mixed | Zombie | 70 | 6 |
| 10 | EVT-012 | 廢棄地圖 | Positive | Story | 50 | 9 |
| 11 | EVT-013 | 雲朗天清 | Positive | Effect | 10 | 10 |
| 12 | EVT-007 | 遠處槍聲 | Negative | Zombie | 31.25 | 10 |
| 13 | EVT-009 | 路線錯判 | Mixed | Effect | 30 | 9 |
| 14 | EVT-004 | 發現補給品 | Positive | Resource | 40 | 10 |
| 15 | EVT-005 | 工具損耗 | Negative | Resource | 30 | 9 |
| 16 | EVT-002 | 飢餓倖存者 | Mixed | Survivor | 40 | 8 |
| 17 | EVT-014 | 屍群來臨 | Negative | Zombie | 50 | 7 |
| 18 | EVT-010 | 可疑信號 | Positive | Story | 50 | 9 |
| 19 | EVT-001 | 食物受潮 | Negative | Resource | 15 | 9 |
| 20 | EVT-008 | 屍群逼近 | Mixed | Zombie | 70 | 9 |
| 21 | EVT-014 | 屍群來臨 | Negative | Zombie | 50 | 9 |
| 22 | EVT-014 | 屍群來臨 | Negative | Zombie | 25 | 10 |
| 23 | EVT-006 | 臨時修繕 | Mixed | Resource | 30 | 10 |
| 24 | EVT-012 | 廢棄地圖 | Positive | Story | 50 | 9 |
| 25 | EVT-002 | 飢餓倖存者 | Mixed | Survivor | 40 | 10 |
| 26 | EVT-003 | 夜間低溫 | Mixed | Resource | 30 | 9 |
| 27 | EVT-011 | 破損路障 | Negative | Effect | 45 | 8 |
| 28 | EVT-014 | 屍群來臨 | Negative | Zombie | 25 | 7 |
| 29 | EVT-007 | 遠處槍聲 | Negative | Zombie | 12.5 | 9 |
| 30 | EVT-006 | 臨時修繕 | Mixed | Resource | 30 | 8 |

## Canonical Distribution

### Mood

| Category | Count |
|---|---:|
| Mixed | 11 |
| Negative | 11 |
| Positive | 8 |

### Content

| Category | Count |
|---|---:|
| Effect | 6 |
| Resource | 10 |
| Story | 3 |
| Survivor | 3 |
| Zombie | 8 |

## Event Frequency Across 100 Runs

| Event ID | Event | Appearances | Share |
|---|---|---:|---:|
| EVT-001 | 食物受潮 | 159 | 5.30% |
| EVT-002 | 飢餓倖存者 | 255 | 8.50% |
| EVT-003 | 夜間低溫 | 251 | 8.37% |
| EVT-004 | 發現補給品 | 229 | 7.63% |
| EVT-005 | 工具損耗 | 220 | 7.33% |
| EVT-006 | 臨時修繕 | 241 | 8.03% |
| EVT-007 | 遠處槍聲 | 166 | 5.53% |
| EVT-008 | 屍群逼近 | 234 | 7.80% |
| EVT-009 | 路線錯判 | 235 | 7.83% |
| EVT-010 | 可疑信號 | 93 | 3.10% |
| EVT-011 | 破損路障 | 167 | 5.57% |
| EVT-012 | 廢棄地圖 | 165 | 5.50% |
| EVT-013 | 雲朗天清 | 236 | 7.87% |
| EVT-014 | 屍群來臨 | 349 | 11.63% |

## Findings

- Fallback days in canonical run: `0`.
- Every event appeared at least once in the canonical run.
- Every event appeared in the 100-run distribution sample.
- Implementation-ready events: `1` of `14`.
- Economy balance, affordability, dynamic event weights, event chains, and threat outcomes remain outside PB-P1-014A.

## Validation Warnings

- Only 1 event is currently implementation-ready. The formal pool is not yet suitable for economy balance testing.
