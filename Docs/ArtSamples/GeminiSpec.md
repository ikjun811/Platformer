# 플레이어 캐릭터 — Gemini 이미지 생성 명령서

GDD v1.1 기준: 고해상도 2D 일러스트, 프레임마다 한 장씩 그리는 애니메이션, 드레드 같은 현대적인 느낌.
디자인은 **기존 시험본(픽셀아트, GPT 시트)을 참고하지 않고 새로** 만듭니다.

---

## 0. 먼저 알아 둘 것

### 진행 순서
| 단계 | 내용 | 결과 |
|---|---|---|
| 1 | 디자인 시안 4종 생성 → 하나 선택 | `Concept_*.png` |
| 2 | 선택한 디자인으로 **기준 이미지**(서 있는 옆모습) 확정 | `Reference.png` |
| 3 | 기준 이미지 + 템플릿을 첨부해 동작별로 생성 | `Player_*.png` |
| 4 | Claude가 배경 제거, 크기 맞춤, 정렬, 시트 조립 | 게임용 파일 |

처음에는 3단계에서 **Idle과 Run만** 만들어 게임에 넣어 보고, 괜찮으면 나머지를 이어 갑니다.

### 템플릿 이미지 (`Docs/ArtSamples/Gemini/Templates/`)
마젠타 배경에 가이드 선이 그려진 이미지입니다. Gemini가 이 위에 그리면 **모든 프레임의 크기와 위치가 맞춰집니다.**

| 파일 | 비율 | 칸 수 | 용도 |
|---|---|---|---|
| `Template_1x.png` | 1:1 | 1 | 기준 이미지, 한 장짜리 자세 |
| `Template_4x.png` | 16:9 | 4 | 2~4프레임 동작 |
| `Template_6x.png` | 21:9 | 6 | 5~6프레임 동작 (달리기 등) |
| `Template_Ball4x.png` | 16:9 | 4 | 모프볼 |

- **실선(아래 가로)** = 바닥선. 서 있을 때 발바닥 위치
- **점선(가로)** = 머리선. 서 있을 때 헬멧 끝 위치
- **점선(세로)** = 칸마다 몸 중심
- 칸이 여러 개면 **왼쪽부터 1, 2, 3… 프레임 순서**입니다

### 사용 규칙
1. Gemini 앱에서 이미지 생성 모델을 고르고, 첨부 이미지는 **기준 이미지 → 템플릿 순서**로 넣습니다.
2. 결과 비율이 템플릿과 다르게 나오면, 프롬프트 끝에 `Keep the exact aspect ratio and layout of the template.`를 붙여 다시 생성합니다.
3. 같은 동작은 **한 번에 한 장(모든 프레임을 한 이미지에)** 생성합니다. 프레임을 따로따로 만들면 디자인이 흔들립니다.
4. 마음에 들지 않는 프레임이 하나뿐이면, 결과 이미지를 다시 첨부하고 `Redraw only frame 3, keep everything else identical.`처럼 부분 수정을 요청합니다.
5. 결과는 **원본 그대로** `Docs/ArtSamples/Gemini/Raw/`에 저장합니다. 파일 이름은 아래 표의 이름을 씁니다. 잘라 내거나 크기를 바꾸지 마세요.
6. 무료 요금제 등에서는 이미지 모서리에 보이는 워터마크가 들어갈 수 있습니다. 템플릿은 캐릭터가 모서리에 닿지 않게 만들어져 있어 처리할 때 잘라 냅니다.
7. 생성 결과의 상업적 이용 조건(요금제, 약관)을 확인해 주세요. 채택하면 `Docs/Credits.md`에 기록합니다.

---

## 1단계 — 디자인 시안 (템플릿 없이)

**첨부: 없음** · 비율 16:9 · 저장: `Concept_1.png`, `Concept_2.png` … (여러 번 생성해도 됨)

```
Design 4 different original characters for a 2D side-scrolling sci-fi action game,
shown side by side, each in full body side view (profile) facing right, standing.

The character: a lone explorer in a sleek, modern powered exosuit, sent to investigate
an abandoned deep-space research facility. Athletic, agile proportions (not bulky, not chibi).
The forearm of the arm nearest the viewer is a built-in arm cannon. The helmet fully covers
the face with a visor. The silhouette must be instantly readable when small on screen.

Style: high-quality 2D game character art, clean digital painting with cel shading,
crisp clean edges, strong color blocking with one glowing accent color,
modern AAA side-scroller look. NOT pixel art.

Make the 4 designs clearly different in color scheme, helmet shape, and armor layout.

Rules:
- Plain flat light gray background, no scenery, no text, no logos.
- Do not use magenta, pink, or purple anywhere on the characters.
- Must be an original design. Avoid an orange/red/yellow color scheme, large round shoulder
  pads, and anything resembling existing famous game characters.
```

**고르는 기준:** 작게 줄여도 형태가 읽히는지, 오른쪽을 보는 옆모습이 자연스러운지, 캐논 팔이 분명한지.
골랐으면 이후 모든 단계에 그 시안 이미지를 첨부합니다.

---

## 2단계 — 기준 이미지 확정

**첨부: 고른 시안 → `Template_1x.png`** · 비율 1:1 · 저장: `Reference.png`

```
Using the character from the first image (the design I chose: <몇 번째인지, 예: the second one from the left>),
draw it on the second image, which is a layout template.

Pose: neutral standing idle, full body pure side view facing right, arm cannon held forward
at chest height, relaxed but ready.

Template rules:
- Keep the flat magenta background (#FF00FF) of the template unchanged. Do not add scenery or shadows.
- Feet (soles) rest exactly on the solid horizontal line near the bottom.
- The top of the helmet touches the dashed horizontal line.
- The body is centered on the dashed vertical line.

Style: high-quality 2D game character art, clean digital painting with cel shading,
crisp clean edges, light from the upper left. NOT pixel art.
No text, no logos, no motion blur, no visual effects. Do not use magenta, pink, or purple on the character.
```

**확인할 것:** 발이 바닥선에, 헬멧 끝이 머리선에 맞는지. 맞지 않으면 다시 생성합니다.
이 `Reference.png`가 **이후 모든 생성의 기준**입니다. 바꾸면 이미 만든 동작도 다시 만들어야 하니 신중하게 고르세요.

---

## 3단계 — 동작별 생성

### 공통 프롬프트 (매번 그대로 사용)
**첨부: `Reference.png` → 해당 템플릿**

```
The first image is the character reference. The second image is a layout template with <N> cells.
Draw the SAME character (identical design, colors, proportions, and armor details) in each cell,
one animation frame per cell, frames ordered left to right.

Animation: <동작 이름>
<프레임 설명>

Template rules:
- Keep the flat magenta background (#FF00FF) of the template unchanged. No scenery, no ground shadows.
- Same viewing angle as the reference, facing right, in every frame.
- Ground poses: feet rest exactly on the solid bottom line.
- The character is the same size in every frame. When standing upright, the helmet top reaches the dashed horizontal line.
- Each frame stays completely inside its own cell with a wide empty gap to the cell borders and image edges.
  No body parts may touch or cross the cell borders. Body centered on the cell's dashed vertical line.

Style: same as the reference, clean digital painting with cel shading, crisp clean edges,
light from the upper left. NOT pixel art.
No text, no numbers, no motion blur, no speed lines, no visual effects (muzzle flash, sparks, glow trails).
Do not use magenta, pink, or purple on the character.
```

`<N>`, `<동작 이름>`, `<프레임 설명>`을 아래 표의 내용으로 바꿉니다.
**공중 동작**은 `Ground poses` 줄을 `Airborne pose: keep the body at the same height as a standing pose, feet above the bottom line.`로 바꿉니다.

### 1순위 — 이동 (24프레임) ★ 먼저 Idle, Run만

| 저장 이름 | 템플릿 | 동작 이름 | 프레임 설명 |
|---|---|---|---|
| `Player_Idle.png` | 4x | Idle breathing loop | `Frame 1: neutral stand. Frame 2: chest rises slightly. Frame 3: top of breath. Frame 4: chest lowers. Very subtle motion, feet never move.` |
| `Player_Run.png` | 6x | Run cycle (fast, athletic) | `A full running cycle in 6 frames: 1 right foot contact, 2 passing pose, 3 left foot push-off airborne, 4 left foot contact, 5 passing pose, 6 right foot push-off airborne. Torso leans forward, the arm cannon stays aimed straight forward, the back arm swings.` |
| `Player_Jump.png` | 4x | Jump | `Frame 1: rising, knees tucked up. Frame 2: apex, body compact. Frame 3: starting to fall, legs extending. Frame 4: falling, legs extended down. Airborne pose for all frames.` |
| `Player_Crouch.png` | 1x | Crouch | `Deep crouch: both knees strongly bent, hips lowered close to the heels, torso upright, arm cannon aimed straight forward. The helmet top is at about two thirds of the standing height, far BELOW the dashed horizontal line. This is NOT a standing pose. Feet flat on the solid bottom line.` |
| `Player_Slide.png` | 4x | Slide | `Frame 1: dropping into the slide, knees bending, leaning back. Frames 2-3: sliding feet-first along the floor, sitting low and leaning back, legs extended forward, arm cannon aimed forward. Frame 4: rising out of the slide into a low crouch. Feet and hip touch the bottom line. Leave a wide gap between neighboring frames; feet and legs must not touch the next frame.` |
| `Player_LedgeHang.png` | 1x | Ledge hang | `The character is HANGING in the air from a ledge, NOT standing. Feet are far above the solid bottom line and dangle freely. Both arms are raised straight up above the head and both hands grip an invisible ledge corner at the upper right, at the height of the dashed horizontal line. The body hangs vertically, chest close to an invisible wall on the right. Do not draw any wall, ledge, line, or ground.` |
| `Player_LedgeClimb.png` | 4x | Ledge climb | `Frame 1 matches the attached hanging pose (both arms up, feet dangling), starting to pull up. Frame 2: elbows on the ledge, chest over it. Frame 3: one knee up on the ledge. Frame 4: standing up on the ledge. The body moves up and right across frames. Do not draw the wall or the ledge.` |

> 기어오르기는 4칸 템플릿에 3~4프레임이 들어갑니다 (4칸을 모두 쓰면 처리할 때 하나를 고릅니다).

### 2순위 — 전투 (26프레임, M2 진행 때)

| 저장 이름 | 템플릿 | 동작 이름 | 프레임 설명 |
|---|---|---|---|
| `Player_AimStand.png` | 4x | Standing aim poses | `Frame 1: aiming straight up. Frame 2: aiming diagonally up-forward. Frame 3: aiming diagonally down-forward. Frame 4: aiming forward (same as idle). Legs identical in all frames, only the upper body and cannon change.` |
| `Player_RunAimUp.png` | 6x | Run cycle aiming diagonally up | 1순위 Run과 같은 다리 동작 + `the arm cannon stays aimed diagonally up-forward in every frame.` |
| `Player_RunAimDown.png` | 6x | Run cycle aiming diagonally down | 1순위 Run과 같은 다리 동작 + `the arm cannon stays aimed diagonally down-forward in every frame.` |
| `Player_JumpAim.png` | 4x | Airborne aim poses | `Frame 1: aiming straight up. Frame 2: diagonally up-forward. Frame 3: diagonally down-forward. Frame 4: straight down. Airborne pose, legs slightly tucked, identical legs in all frames.` |
| `Player_CrouchAim.png` | 1x | Crouch aiming diagonally up | `Same crouch as before, arm cannon aimed diagonally up-forward.` |
| `Player_Counter.png` | 4x | Melee counter strike | `Frame 1: wind-up, cannon arm pulled back. Frame 2: fast forward swing with the cannon arm. Frame 3: impact, arm fully extended forward. Frame 4: recovery to ready stance. Feet planted.` |
| `Player_Hurt.png` | 1x | Hit reaction | `Knocked back, upper body bent backward, arms flung out, one foot lifted.` |

### 3순위 — 획득 능력 (최대 12프레임, M4 진행 때)

| 저장 이름 | 템플릿 | 동작 이름 | 프레임 설명 |
|---|---|---|---|
| `Player_MorphBall.png` | Ball4x | Morph ball rolling | `The character transformed into a compact armored sphere, filling the drawn circle in each cell. Frames show a rolling rotation in 4 steps (0, 90, 180, 270 degrees), the glowing accent rotating with it. No arms, legs, or head visible.` |
| `Player_MorphTransition.png` | 4x | Curling into a ball | `Frame 1: crouch. Frame 2: curling up tightly. Frames 3-4: almost a sphere, armor plates closing. Use only 2-4 cells as needed.` |
| `Player_DoubleJump.png` | 4x | Mid-air somersault | `A forward flip in 4 frames, body tucked into a tight ball-like curl, rotating 90 degrees each frame. Airborne.` |

### 4순위 — 나중

| 저장 이름 | 템플릿 | 내용 |
|---|---|---|
| `Player_Death.png` | 4x | 쓰러지기 4단계 |
| `Player_Save.png` | 1x | 화면 쪽을 보고 서서 세이브 스테이션 사용 (옆모습 예외) |
| `Player_WallSlide.png` | 1x | 오른쪽 벽에 붙어 미끄러짐 |
| `Player_Dash.png` | 4x | 대시 3프레임 |

**합계: 1순위 24, 2순위 26, 3순위 최대 12 = 약 62프레임.** 구역 1에 필요한 범위입니다.

> 빔, 차지 발광, 총구 불꽃, 슬라이드 먼지 같은 이펙트는 캐릭터에 그리지 않고 **나중에 따로** 만듭니다.

---

## 4단계 — Claude가 처리하는 것 (참고)

결과를 `Raw/`에 저장하고 알려 주시면 처리 스크립트를 만들어 다음을 합니다 (별도 작업으로 제안 예정).
- 마젠타 배경과 가이드 선 제거 (투명 처리)
- 바닥선과 머리선 간격을 기준으로 크기 통일. 게임 속 키는 288px (PPU 128에서 2.25타일)
- 칸별로 잘라 프레임마다 **384x384** 캔버스에 배치. 피벗은 하단 중앙(발 위치)
- 가로 시트로 조립해 `Assets/_Project/Art/Characters/`에 저장

> 처리 스크립트에는 Python 이미지 라이브러리(Pillow) 설치가 필요할 수 있습니다. 그때 설치 여부를 따로 묻겠습니다.
