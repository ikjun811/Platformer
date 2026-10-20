"""Gemini가 만든 동작 이미지(마젠타 배경)를 게임용 스프라이트 시트로 바꾼다.

처리 순서
  1. 마젠타 배경과 가이드 선을 투명하게 지우고, 가장자리에 섞인 마젠타 색을 걷어 낸다
  2. 이어진 픽셀 덩어리 단위로 프레임을 찾는다 (칸 경계를 넘는 캐논도 잘리지 않도록)
     작은 조각은 가까운 프레임에 붙이고, 멀리 떨어진 조각(워터마크 등)은 버린다
  3. 크기를 통일한다. 기준 시트(Idle)의 서 있는 키를 288px(2.25타일, PPU 128)로 맞추고,
     다른 시트는 바이저(청록색 헬멧 유리) 높이를 기준 시트와 비교해 같은 배율이 되게 한다
  4. 프레임마다 칸에 놓는다. 피벗(칸 아래쪽 pivot_x 위치)이 게임 속 충돌체 바닥 중앙이다
     - ground: 골반 x → 피벗, 시트의 바닥선 → 칸 맨 아래 (달리기 중 뜬 프레임은 뜬 높이 유지)
     - air:    바이저 중심 → 서 있을 때(Idle)의 바이저 중심 위치 (AIR_LIFT만큼 위)
     - ledge:  프레임마다 지정한 원본 좌표의 모서리 지점 → 충돌체 오른쪽 위 모서리
               ("stand"로 지정한 프레임은 모서리 위에 선 자세: 발 → 모서리 높이, 골반 → 기어오른 뒤 위치)
  5. 가로 시트 PNG를 Assets에, 확인용 GIF를 Docs에 저장한다

실행: python Tools/ArtPipeline/process_sprites.py
필요: Pillow (python -m pip install pillow)
"""
import os
import statistics
import sys

from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))
RAW_DIR = os.path.join(ROOT, "Docs", "ArtSamples", "Gemini", "make")
SHEET_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "Characters")
PREVIEW_DIR = os.path.join(ROOT, "Docs", "ArtSamples", "Gemini", "Processed")

# 게임 규격 (GDD 8장, PPU 128)
STANDING_HEIGHT = 288     # 서 있는 키 = 충돌체 높이 2.25타일
COLLIDER_HALF_WIDTH = 48  # 충돌체 폭 0.75타일의 절반
CLIMB_END_OFFSET = 109    # 기어오른 뒤 충돌체 중심이 매달린 위치보다 오른쪽으로 가는 거리 (0.85타일, PlayerLedgeGrab 기준)
AIR_LIFT = 8              # 공중 동작을 서 있을 때보다 살짝 올린다 (다리를 뻗은 낙하 자세가 칸 아래로 잘리지 않도록)

# 시트 목록. 첫 줄이 크기 기준 시트
#   cell: 칸 크기, pivot_x: 칸 안의 피벗 x, order: 출력할 프레임 순서 (원본 프레임 번호, 0부터)
#   anchors (ledge 전용): 프레임별 모서리 지점의 원본 좌표 (x, y) 또는 "stand"
SHEETS = [
    dict(src="Idle.png", out="Player_Idle", frames=4, mode="ground", fps=6),
    dict(src="Run.png", out="Player_Run", frames=6, mode="ground", fps=14),
    dict(src="Jump.png", out="Player_Jump", frames=4, mode="air", fps=6),
    dict(src="Crouch.png", out="Player_Crouch", frames=1, mode="ground", fps=1),
    # 4번 프레임은 캐논이 잘려서 1번 프레임으로 대체 (시작·끝이 같은 쪼그린 자세)
    dict(src="Slide.png", out="Player_Slide", frames=4, order=[0, 1, 2, 0], mode="ground", fps=10),
    # 매달리기는 기어오르기 1번 프레임을 쓴다 (두 동작이 자연스럽게 이어진다)
    dict(src="LedgeClimb.png", out="Player_LedgeHang", frames=4, order=[0], mode="ledge",
         anchors=[(530, 510), None, None, None], fps=1),
    dict(src="LedgeClimb.png", out="Player_LedgeClimb", frames=4, mode="ledge", cell=(512, 768),
         anchors=[(530, 510), (1100, 515), (1770, 852), "stand"], fps=12),
]
DEFAULT_CELL = (384, 384)
PIVOT_X = 192

MAGENTA = (248, 0, 248)
# 마젠타 정도 m = min(R, B) - G. 이 범위에서 불투명 → 투명으로 바뀐다
KEY_LOW, KEY_HIGH = 20, 140


def remove_background(src):
    """마젠타를 투명으로 바꾸고 가장자리 색을 되돌린다 (배경과 섞인 비율만큼 마젠타를 빼 낸다)."""
    src = src.convert("RGB")
    out = Image.new("RGBA", src.size)
    pixels = []
    for r, g, b in src.get_flattened_data():
        m = min(r, b) - g
        f = (m - KEY_LOW) / (KEY_HIGH - KEY_LOW)
        if f <= 0:
            pixels.append((r, g, b, 255))
        elif f >= 1:
            pixels.append((0, 0, 0, 0))
        else:
            keep = 1 - f
            # 섞인 마젠타 비율은 m / 248로 추정해서 색에서 빼 낸다
            mix = min(m / 248, 0.95)
            rr = (r - mix * MAGENTA[0]) / (1 - mix)
            bb = (b - mix * MAGENTA[2]) / (1 - mix)
            gg = g / (1 - mix)
            pixels.append((clamp(rr), clamp(gg), clamp(bb), int(keep * 255)))
    out.putdata(pixels)
    return out


def clamp(v):
    return max(0, min(255, int(round(v))))


def find_components(img, threshold=128):
    """불투명 픽셀 덩어리를 찾는다 (가로 구간 단위 union-find). 반환: [면적, x0, y0, x1, y1, 구간 목록]."""
    w, h = img.size
    alpha = img.getchannel("A").tobytes()
    runs = []
    parent = []
    prev_row = []

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    for y in range(h):
        row = []
        line = alpha[y * w:(y + 1) * w]
        x = 0
        while x < w:
            if line[x] >= threshold:
                start = x
                while x < w and line[x] >= threshold:
                    x += 1
                idx = len(runs)
                runs.append((y, start, x - 1))
                parent.append(idx)
                for px0, px1, pid in prev_row:
                    if px0 <= x and px1 >= start - 1:   # 대각선 포함 연결
                        a, b = find(idx), find(pid)
                        if a != b:
                            parent[b] = a
                row.append((start, x - 1, idx))
            else:
                x += 1
        prev_row = row

    groups = {}
    for i, run in enumerate(runs):
        groups.setdefault(find(i), []).append(run)

    comps = []
    for group in groups.values():
        area = sum(x1 - x0 + 1 for _, x0, x1 in group)
        comps.append([area,
                      min(r[1] for r in group), min(r[0] for r in group),
                      max(r[2] for r in group), max(r[0] for r in group),
                      group])
    return comps


def box_distance(a, b):
    dx = max(0, max(a[1], b[1]) - min(a[3], b[3]))
    dy = max(0, max(a[2], b[2]) - min(a[4], b[4]))
    return (dx * dx + dy * dy) ** 0.5


def split_frames(img, count):
    """프레임 수만큼 큰 덩어리를 고르고, 작은 조각은 가까운 프레임에 붙인다. 왼쪽부터 정렬."""
    comps = sorted(find_components(img), key=lambda c: -c[0])
    if len(comps) < count:
        sys.exit(f"프레임 {count}개를 찾지 못함 (덩어리 {len(comps)}개)")
    frames = comps[:count]
    if frames[-1][0] < frames[0][0] * 0.3:
        sys.exit(f"프레임 크기가 너무 달라서 분리가 의심됨 (프레임끼리 붙었을 수 있음): {[c[0] for c in frames]}")

    dropped = []
    for c in comps[count:]:
        nearest = min(frames, key=lambda f: box_distance(c, f))
        if box_distance(c, nearest) <= 30:
            nearest[5].extend(c[5])
            nearest[1], nearest[2] = min(nearest[1], c[1]), min(nearest[2], c[2])
            nearest[3], nearest[4] = max(nearest[3], c[3]), max(nearest[4], c[4])
        elif c[0] > 30:
            dropped.append((c[0], c[1], c[2]))
    if dropped:
        print(f"  버린 조각 (면적, x, y): {dropped}")

    frames.sort(key=lambda f: f[1])
    return frames


def visor_box(img, frame):
    """프레임 위쪽 40%에서 가장 큰 밝은 청록색 덩어리(바이저)의 범위 (원본 좌표 x0, y0, x1, y1)."""
    _, x0, y0, x1, y1, _ = frame
    limit = y0 + (y1 - y0) * 2 // 5
    region = img.crop((x0, y0, x1 + 1, limit + 1))
    cyan = Image.new("RGBA", region.size, (0, 0, 0, 0))
    cyan.putdata([(0, 0, 0, 255) if a > 200 and g > 170 and b > 170 and r < 120 else (0, 0, 0, 0)
                  for r, g, b, a in region.get_flattened_data()])
    comps = find_components(cyan)
    if not comps:
        return None
    _, bx0, by0, bx1, by1, _ = max(comps, key=lambda c: c[0])
    return bx0 + x0, by0 + y0, bx1 + x0, by1 + y0


def frame_mask(img, frame):
    """프레임 덩어리만 남긴 이미지 (옆 프레임의 픽셀 제외)."""
    _, x0, y0, x1, y1, group = frame
    crop = img.crop((x0, y0, x1 + 1, y1 + 1))
    mask = Image.new("L", crop.size, 0)
    mp = mask.load()
    for y, rx0, rx1 in group:
        for x in range(rx0, rx1 + 1):
            mp[x - x0, y - y0] = 255
    # 덩어리 가장자리의 반투명 픽셀도 살리기 위해 마스크를 2px 넓힌다
    mask = mask.filter(ImageFilter.MaxFilter(5))
    alpha = Image.composite(crop.getchannel("A"), Image.new("L", crop.size, 0), mask)
    crop.putalpha(alpha)
    return crop


def hip_x(img, frame, ground_y, height):
    """골반 높이(바닥에서 키의 45~55%) 불투명 픽셀 x의 중앙값."""
    _, x0, y0, x1, y1, _ = frame
    px = img.load()
    xs = []
    for y in range(int(ground_y - height * 0.55), int(ground_y - height * 0.45)):
        if y < y0 or y > y1:
            continue
        for x in range(x0, x1 + 1):
            if px[x, y][3] > 128:
                xs.append(x)
    return statistics.median(xs) if xs else (x0 + x1) / 2


class Reference:
    """기준 시트(Idle)에서 잰 값. 다른 시트의 크기와 위치를 맞출 때 쓴다."""
    source_height = None   # 원본에서 서 있는 키 (px)
    visor_height = None    # 원본 바이저 높이 (px)
    visor_center = None    # 출력 칸에서 바이저 중심의 피벗 기준 위치 (dx, 바닥에서의 높이)


def process(sheet, ref):
    img = remove_background(Image.open(os.path.join(RAW_DIR, sheet["src"])))
    frames = split_frames(img, sheet["frames"])
    cell_w, cell_h = sheet.get("cell", DEFAULT_CELL)
    mode = sheet["mode"]

    visors = [visor_box(img, f) for f in frames]
    visor_height = statistics.median(v[3] - v[1] + 1 for v in visors if v)
    ground_y = max(f[4] for f in frames)

    if ref.source_height is None:
        # 기준 시트: 바닥에 선 프레임들의 키 중앙값
        source_height = statistics.median(f[4] - f[2] + 1 for f in frames if ground_y - f[4] <= 4)
        ref.source_height, ref.visor_height = source_height, visor_height
    else:
        source_height = ref.source_height * (visor_height / ref.visor_height)
    scale = STANDING_HEIGHT / source_height
    print(f"  서 있는 키 환산 {source_height:.0f}px, 바이저 {visor_height}px, 배율 {scale:.3f}")

    order = sheet.get("order", list(range(len(frames))))
    out = Image.new("RGBA", (cell_w * len(order), cell_h), (0, 0, 0, 0))
    ledge_point = (PIVOT_X + COLLIDER_HALF_WIDTH, cell_h - STANDING_HEIGHT)
    warnings = []

    for slot, index in enumerate(order):
        frame = frames[index]
        _, x0, y0, x1, y1, _ = frame
        if x0 <= 2 or x1 >= img.size[0] - 3:
            warnings.append(f"원본 {index + 1}번 프레임이 이미지 가장자리에 닿음 (잘렸을 수 있음)")

        # 원본 좌표 (sx, sy)가 칸 좌표 (tx, ty)에 오도록 배치한다
        if mode == "ground":
            sx, sy = hip_x(img, frame, ground_y, source_height), ground_y + 1
            tx, ty = PIVOT_X, cell_h
        elif mode == "air":
            v = visors[index]
            sx, sy = (v[0] + v[2]) / 2, (v[1] + v[3]) / 2
            tx, ty = PIVOT_X + ref.visor_center[0], cell_h - ref.visor_center[1] - AIR_LIFT
        else:
            anchor = sheet["anchors"][index]
            if anchor == "stand":
                sx, sy = hip_x(img, frame, y1 + 1, source_height), y1 + 1
                tx, ty = PIVOT_X + CLIMB_END_OFFSET, ledge_point[1]
            else:
                (sx, sy), (tx, ty) = anchor, ledge_point

        piece = frame_mask(img, frame)
        size = (max(1, round(piece.width * scale)), max(1, round(piece.height * scale)))
        piece = piece.convert("RGBa").resize(size, Image.LANCZOS).convert("RGBA")
        dx = round(tx - (sx - x0) * scale)
        dy = round(ty - (sy - y0) * scale)
        if dx < 0 or dy < 0 or dx + piece.width > cell_w or dy + piece.height > cell_h:
            warnings.append(f"{slot + 1}번 칸을 벗어남 (x {dx}~{dx + piece.width}, y {dy}~{dy + piece.height}, 칸 {cell_w}x{cell_h})")

        cell = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
        cell.alpha_composite(piece, (max(dx, 0), max(dy, 0)), (max(-dx, 0), max(-dy, 0)))
        out.alpha_composite(cell, (slot * cell_w, 0))

        if ref.visor_center is None and mode == "ground" and slot == 0:
            v = visors[index]
            vx = dx + ((v[0] + v[2]) / 2 - x0) * scale
            vy = dy + ((v[1] + v[3]) / 2 - y0) * scale
            ref.visor_center = (vx - PIVOT_X, cell_h - vy)

    for w in warnings:
        print("  경고:", w)
    return out


def save_preview(sheet_image, sheet, path):
    """어두운 배경 위에서 재생하는 GIF. 충돌체(초록)와, 매달리기 시트는 모서리(회색)를 함께 그린다."""
    cell_w, cell_h = sheet.get("cell", DEFAULT_CELL)
    count = sheet_image.width // cell_w
    frames = []
    for i in range(count):
        bg = Image.new("RGBA", (cell_w, cell_h), (26, 26, 46, 255))
        d = ImageDraw.Draw(bg)
        if sheet["mode"] == "ledge":
            lx, ly = PIVOT_X + COLLIDER_HALF_WIDTH, cell_h - STANDING_HEIGHT
            d.rectangle((lx, ly, cell_w, cell_h), fill=(108, 122, 137, 255))
        else:
            d.rectangle((0, cell_h - 4, cell_w, cell_h), fill=(108, 122, 137, 255))
        bg.alpha_composite(sheet_image.crop((i * cell_w, 0, (i + 1) * cell_w, cell_h)))
        d = ImageDraw.Draw(bg)
        d.rectangle((PIVOT_X - COLLIDER_HALF_WIDTH, cell_h - STANDING_HEIGHT, PIVOT_X + COLLIDER_HALF_WIDTH, cell_h - 1),
                    outline=(80, 220, 120, 255))
        frames.append(bg.convert("RGB"))
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=int(1000 / sheet["fps"]), loop=0)


def main():
    os.makedirs(SHEET_DIR, exist_ok=True)
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    ref = Reference()
    for sheet in SHEETS:
        print(f"{sheet['src']} → {sheet['out']}")
        image = process(sheet, ref)
        image.save(os.path.join(SHEET_DIR, sheet["out"] + ".png"))
        save_preview(image, sheet, os.path.join(PREVIEW_DIR, sheet["out"] + "_preview.gif"))
    print("시트:", SHEET_DIR)
    print("미리보기:", PREVIEW_DIR)


if __name__ == "__main__":
    main()
