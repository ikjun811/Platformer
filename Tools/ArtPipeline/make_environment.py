"""Sci-Fi Texture Pack 2 텍스처를 가공해 지형 타일과 배경 벽을 만든다.

만드는 것 (Assets/_Project/Art/Environment/)
  - Ground_Fill_XY.png (4장): 지형 바탕 타일 128x128. Floor_2의 금속판 4장을 한 장씩 잘라
    월드 좌표 (x mod 2, y mod 2)에 맞춰 칠하면 원본처럼 이음매가 이어진다
  - Ground_Edge_NN.png (15장): 지형 테두리 덧칠 타일 (투명 배경). NN = 노출된 면의 비트 합
    1 위, 2 오른쪽, 4 아래, 8 왼쪽. 위는 밟는 면이라 밝은 테두리, 나머지는 어두운 테두리
  - Wall_Background.png: 배경 벽 512x1024 (반복 배치용). 위는 Wall_4, 아래는 Panels_1이고
    Panels_1의 세로 홈 4개를 창문(투명)으로 뚫어 뒤쪽 우주 배경이 보이게 한다
원본은 회색이라 정비 구역 색(어두운 청회색)을 입히고, 배경 벽은 더 어둡게 해서 지형과 구분한다.

확인용 화면: Docs/ArtSamples/Environment/mockup_1080p.png

실행: python Tools/ArtPipeline/make_environment.py
필요: Pillow, Asset Store 에셋 (Sci-Fi Texture Pack 2, DinV Dynamic Space Background)
"""
import os

from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))
TEXTURE_DIR = os.path.join(ROOT, "Assets", "Sci-Fi Texture Pack 2", "Textures")
SPACE_DIR = os.path.join(ROOT, "Assets", "DinV", "Dynamic Space Background", "Sprites")
OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "Environment")
CHARACTER_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "Characters")
PREVIEW_DIR = os.path.join(ROOT, "Docs", "ArtSamples", "Environment")

TILE = 128  # PPU 128 → 1타일

# 색 입히기: 회색 밝기 0~255를 어두운 색 → 밝은 색으로 옮긴다 (정비 구역: 어두운 청회색)
TERRAIN_RAMP = ((18, 23, 34), (150, 168, 192))
WALL_RAMP = ((7, 10, 16), (46, 56, 76))

# 테두리 색
LIP_HIGHLIGHT = (214, 228, 242)
LIP_TOP = (128, 146, 170)
LIP_BOTTOM = (64, 77, 98)
EDGE_DARK = (10, 13, 20)
WINDOW_RIM = (40, 120, 130)

# Panels_1을 512px로 줄였을 때의 세로 홈(창문) 위치 (x0, y0, x1, y1)
WINDOW_SLOTS = [(41, 41, 86, 470), (169, 41, 214, 470), (297, 41, 342, 470), (425, 41, 470, 470)]

TOP, RIGHT, BOTTOM, LEFT = 1, 2, 4, 8


def tint(gray, ramp, contrast=1.0):
    """회색 이미지를 ramp 색 사이로 옮긴다. contrast < 1이면 중간 밝기 쪽으로 눌러 대비를 줄인다."""
    dark, light = ramp
    gray = gray.convert("L").point(lambda v: max(0, min(255, int(128 + (v - 128) * contrast))))
    channels = [gray.point(lambda v, d=d, l=l: int(d + (l - d) * v / 255)) for d, l in zip(dark, light)]
    return Image.merge("RGB", channels)


def load(name, size):
    return Image.open(os.path.join(TEXTURE_DIR, name)).convert("L").resize(size, Image.LANCZOS)


def make_fill_tiles():
    """Floor_2(금속판 2x2)를 256px로 줄여 판 한 장 = 타일 한 칸으로 자른다."""
    floor = tint(load("Floor_2_Diffuse.png", (TILE * 2, TILE * 2)), TERRAIN_RAMP, contrast=1.1)
    tiles = {}
    for qx in range(2):
        for qy in range(2):
            # 월드 y가 클수록 위쪽이므로 이미지 행은 뒤집는다
            row = 1 - qy
            tiles[f"Ground_Fill_{qx}{qy}"] = floor.crop((qx * TILE, row * TILE, (qx + 1) * TILE, (row + 1) * TILE))
    return tiles


def lerp(a, b, t):
    return tuple(int(x + (y - x) * t) for x, y in zip(a, b))


def make_edge_tile(mask):
    tile = Image.new("RGBA", (TILE, TILE), (0, 0, 0, 0))
    d = ImageDraw.Draw(tile)

    if mask & BOTTOM:
        for i in range(10):
            d.line((0, TILE - 1 - i, TILE, TILE - 1 - i), fill=EDGE_DARK + (int(255 * (1 - i / 10)),))
    if mask & RIGHT:
        for i in range(8):
            d.line((TILE - 1 - i, 0, TILE - 1 - i, TILE), fill=EDGE_DARK + (int(255 * (1 - i / 8)),))
    if mask & LEFT:
        for i in range(8):
            d.line((i, 0, i, TILE), fill=EDGE_DARK + (int(230 * (1 - i / 8)),))
        d.line((0, 0, 0, TILE), fill=LIP_BOTTOM + (255,))
    if mask & TOP:
        # 밟는 면: 밝은 윗선 → 금속 띠 → 어두운 아랫선 → 그림자
        d.rectangle((0, 0, TILE, 1), fill=LIP_HIGHLIGHT + (255,))
        for i in range(2, 12):
            d.line((0, i, TILE, i), fill=lerp(LIP_TOP, LIP_BOTTOM, (i - 2) / 9) + (255,))
        d.rectangle((0, 12, TILE, 13), fill=EDGE_DARK + (255,))
        for i in range(14, 22):
            d.line((0, i, TILE, i), fill=EDGE_DARK + (int(150 * (1 - (i - 14) / 8)),))
    return tile


def make_wall():
    """위: Wall_4, 아래: Panels_1 (세로 홈을 창문으로 뚫음). 살짝 흐리게 해서 뒤로 물러나 보이게 한다."""
    upper = tint(load("Wall_4_Diffuse.png", (512, 512)), WALL_RAMP, contrast=0.9)
    lower = tint(load("Panels_1_Diffuse.png", (512, 512)), WALL_RAMP, contrast=0.9)
    wall = Image.new("RGBA", (512, 1024))
    wall.paste(upper, (0, 0))
    wall.paste(lower, (0, 512))
    wall = wall.filter(ImageFilter.GaussianBlur(1.0))

    d = ImageDraw.Draw(wall)
    for x0, y0, x1, y1 in WINDOW_SLOTS:
        d.rectangle((x0 - 2, 512 + y0 - 2, x1 + 2, 512 + y1 + 2), fill=WINDOW_RIM + (255,))
        d.rectangle((x0, 512 + y0, x1, 512 + y1), fill=(0, 0, 0, 0))
    return wall


def tile_fill(texture, width, height, offset=(0, 0)):
    out = Image.new("RGBA", (width, height))
    for y in range(-texture.height, height, texture.height):
        for x in range(-texture.width, width, texture.width):
            out.alpha_composite(texture.convert("RGBA"), (x + offset[0] % texture.width, y + offset[1] % texture.height))
    return out


# 확인용 화면: 테스트 룸 아래층 왼쪽 (x -10~9, y -6~5)
MOCKUP_LAYOUT = [
    "#.....................",
    "#.....................",
    "#............####.....",
    "#.....................",
    "#.....................",
    "#.......................",
    "#...........####........",
    "#.....................",
    "#...####..........####..",
    "#.....................",
    "#.....................",
    "######################",
    "######################",
]


def solid(x, y):
    if y < 0 or y >= len(MOCKUP_LAYOUT):
        return True
    row = MOCKUP_LAYOUT[y]
    return x < 0 or (x < len(row) and row[x] == "#")


def make_mockup(fills, edges, wall):
    cols, rows = 20, len(MOCKUP_LAYOUT)
    w, h = cols * TILE, rows * TILE
    scene = Image.new("RGBA", (w, h), (0, 0, 0, 255))

    nebula = os.path.join(SPACE_DIR, "Nebula Blue.png")
    if os.path.exists(nebula):
        space = Image.open(nebula).convert("RGBA").resize((2048, 2048), Image.LANCZOS)
        scene.alpha_composite(tile_fill(space, w, h))
        for name in ("Stars Small_1.png", "Stars-Big_1.png"):
            stars = Image.open(os.path.join(SPACE_DIR, name)).convert("RGBA").resize((2048, 2048), Image.LANCZOS)
            scene.alpha_composite(tile_fill(stars, w, h))

    scene.alpha_composite(tile_fill(wall, w, h, offset=(0, 300)))

    for y in range(rows):
        for x in range(cols):
            if not solid(x, y):
                continue
            world_x, world_y = x - 10, 5 - y
            fill = fills[f"Ground_Fill_{world_x % 2}{world_y % 2}"]
            scene.alpha_composite(fill.convert("RGBA"), (x * TILE, y * TILE))
            mask = (TOP if not solid(x, y - 1) else 0) | (RIGHT if not solid(x + 1, y) else 0) \
                | (BOTTOM if not solid(x, y + 1) else 0) | (LEFT if not solid(x - 1, y) else 0)
            if mask:
                scene.alpha_composite(edges[mask], (x * TILE, y * TILE))

    idle = os.path.join(CHARACTER_DIR, "Player_Idle.png")
    if os.path.exists(idle):
        player = Image.open(idle).crop((0, 0, 384, 384))
        feet_y = 11 * TILE   # 바닥 윗면 (y = -5 타일의 위)
        scene.alpha_composite(player, (3 * TILE - 192 + 64, feet_y - 384))

    # 화면에 보이는 높이 11.25타일만 잘라 1080p로 줄인다
    view = scene.crop((0, h - int(11.25 * TILE), w, h))
    return view.resize((1920, 1080), Image.LANCZOS).convert("RGB")


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    os.makedirs(PREVIEW_DIR, exist_ok=True)

    fills = make_fill_tiles()
    for name, image in fills.items():
        image.save(os.path.join(OUT_DIR, name + ".png"))

    edges = {mask: make_edge_tile(mask) for mask in range(1, 16)}
    for mask, image in edges.items():
        image.save(os.path.join(OUT_DIR, f"Ground_Edge_{mask:02d}.png"))

    wall = make_wall()
    wall.save(os.path.join(OUT_DIR, "Wall_Background.png"))

    make_mockup(fills, edges, wall).save(os.path.join(PREVIEW_DIR, "mockup_1080p.png"))
    print("타일·배경:", OUT_DIR)
    print("확인용 화면:", os.path.join(PREVIEW_DIR, "mockup_1080p.png"))


if __name__ == "__main__":
    main()
