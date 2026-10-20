"""Gemini가 만든 동작 이미지(마젠타 배경)를 게임용 스프라이트 시트로 바꾼다.

처리 순서
  1. 마젠타 배경과 가이드 선을 투명하게 지우고, 가장자리에 섞인 마젠타 색을 걷어 낸다
  2. 이어진 픽셀 덩어리 단위로 프레임을 찾는다 (칸 경계를 넘는 캐논도 잘리지 않도록)
     작은 조각은 가까운 프레임에 붙이고, 멀리 떨어진 조각(워터마크 등)은 버린다
  3. 크기를 통일한다. 기준 시트(Idle)의 서 있는 키를 288px(2.25타일, PPU 128)로 맞추고,
     다른 시트는 바이저(청록색 헬멧 유리) 높이를 기준 시트와 비교해 같은 배율이 되게 한다
  4. 프레임마다 384x384 캔버스에 놓는다. 피벗은 하단 중앙이고 바닥선이 맨 아래 줄이다
     가로 위치는 골반 높이 픽셀의 중앙값, 세로 위치는 시트의 바닥선 기준 (점프 중인 프레임은 떠 있는 높이 유지)
  5. 가로 시트 PNG와 확인용 GIF를 저장한다

실행: python Tools/ArtPipeline/process_sprites.py
필요: Pillow (python -m pip install pillow)
"""
import os
import statistics
import sys

from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))
RAW_DIR = os.path.join(ROOT, "Docs", "ArtSamples", "Gemini", "make")
OUT_DIR = os.path.join(ROOT, "Docs", "ArtSamples", "Gemini", "Processed")

# 게임 규격 (GDD 8장)
FRAME_SIZE = 384
STANDING_HEIGHT = 288

# (원본 파일, 출력 이름, 프레임 수, 미리보기 초당 프레임). 첫 줄이 크기 기준 시트
SHEETS = [
    ("Idle.png", "Player_Idle", 4, 6),
    ("Run.png", "Player_Run", 6, 12),
]

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
            # 섞인 마젠타 비율(f)은 m / 248로 추정해서 색에서 빼 낸다
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
    """불투명 픽셀 덩어리를 찾는다 (가로 구간 단위 union-find). 반환: [(면적, x0, y0, x1, y1, 구간 목록)]."""
    w, h = img.size
    alpha = img.getchannel("A").tobytes()
    runs = []          # (y, x0, x1)
    parent = []
    prev_row = []      # 이전 줄의 (x0, x1, id)

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
                    if px0 <= x - 1 + 1 and px1 >= start - 1:   # 대각선 포함 연결
                        a, b = find(idx), find(pid)
                        if a != b:
                            parent[b] = a
                row.append((start, x - 1, idx))
            else:
                x += 1
        prev_row = row

    groups = {}
    for i, (y, x0, x1) in enumerate(runs):
        groups.setdefault(find(i), []).append((y, x0, x1))

    comps = []
    for group in groups.values():
        area = sum(x1 - x0 + 1 for _, x0, x1 in group)
        xs0 = min(r[1] for r in group)
        xs1 = max(r[2] for r in group)
        ys0 = min(r[0] for r in group)
        ys1 = max(r[0] for r in group)
        comps.append([area, xs0, ys0, xs1, ys1, group])
    return comps


def box_distance(a, b):
    dx = max(0, max(a[1], b[1]) - min(a[3], b[3]))
    dy = max(0, max(a[2], b[2]) - min(a[4], b[4]))
    return (dx * dx + dy * dy) ** 0.5


def split_frames(img, count):
    """프레임 수만큼 큰 덩어리를 고르고, 작은 조각은 가까운 프레임에 붙인다."""
    comps = sorted(find_components(img), key=lambda c: -c[0])
    if len(comps) < count:
        sys.exit(f"프레임 {count}개를 찾지 못함 (덩어리 {len(comps)}개)")
    frames = comps[:count]
    largest = frames[-1][0]
    if largest < frames[0][0] * 0.3:
        sys.exit(f"프레임 크기가 너무 달라서 분리가 의심됨: {[c[0] for c in frames]}")

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


def visor_height(img, frame):
    """프레임 위쪽 40%에서 가장 큰 밝은 청록색 덩어리(바이저)의 높이 (시트 간 크기 비교용)."""
    _, x0, y0, x1, y1, _ = frame
    limit = y0 + (y1 - y0) * 2 // 5
    region = img.crop((x0, y0, x1 + 1, limit + 1))
    cyan = Image.new("RGBA", region.size, (0, 0, 0, 0))
    cyan.putdata([(0, 0, 0, 255) if a > 200 and g > 170 and b > 170 and r < 120 else (0, 0, 0, 0)
                  for r, g, b, a in region.get_flattened_data()])
    comps = find_components(cyan)
    if not comps:
        return None
    biggest = max(comps, key=lambda c: c[0])
    return biggest[4] - biggest[2] + 1


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
    from PIL import ImageFilter
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


def process(path, frames_count, reference=None):
    src = Image.open(path)
    img = remove_background(src)
    frames = split_frames(img, frames_count)

    ground_y = max(f[4] for f in frames)
    visors = [v for v in (visor_height(img, f) for f in frames) if v]
    visor = statistics.median(visors) if visors else None

    if reference is None:
        # 기준 시트: 바닥에 선 프레임들의 키 중앙값
        heights = [f[4] - f[2] + 1 for f in frames if ground_y - f[4] <= 4]
        source_height = statistics.median(heights)
    else:
        ref_height, ref_visor = reference
        source_height = ref_height * (visor / ref_visor)
    scale = STANDING_HEIGHT / source_height

    print(f"  원본 {src.size[0]}x{src.size[1]}, 바닥선 y={ground_y}, 서 있는 키 {source_height:.0f}px, "
          f"바이저 {visor}px, 배율 {scale:.3f}")

    sheet = Image.new("RGBA", (FRAME_SIZE * frames_count, FRAME_SIZE), (0, 0, 0, 0))
    warnings = []
    for i, frame in enumerate(frames):
        _, x0, y0, x1, y1, _ = frame
        if x0 <= 2 or x1 >= img.size[0] - 3:
            warnings.append(f"{i + 1}번 프레임이 원본 이미지 가장자리에 닿음 (잘렸을 수 있음)")

        piece = frame_mask(img, frame)
        anchor_x = hip_x(img, frame, ground_y, source_height)
        size = (max(1, round(piece.width * scale)), max(1, round(piece.height * scale)))
        piece = piece.convert("RGBa").resize(size, Image.LANCZOS).convert("RGBA")

        # 골반 x → 캔버스 가로 중앙, 바닥선 → 캔버스 맨 아래
        dx = round(FRAME_SIZE / 2 - (anchor_x - x0) * scale)
        dy = round(FRAME_SIZE - (ground_y + 1 - y0) * scale)
        if dx < 0 or dy < 0 or dx + piece.width > FRAME_SIZE or dy + piece.height > FRAME_SIZE:
            warnings.append(f"{i + 1}번 프레임이 {FRAME_SIZE}x{FRAME_SIZE} 칸을 벗어남 (x {dx}~{dx + piece.width}, y {dy}~{dy + piece.height})")
        cell = Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE), (0, 0, 0, 0))
        cell.alpha_composite(piece, (max(dx, 0), max(dy, 0)), (max(-dx, 0), max(-dy, 0)))
        sheet.alpha_composite(cell, (i * FRAME_SIZE, 0))

    for w in warnings:
        print("  경고:", w)
    return sheet, (source_height, visor)


def save_preview(sheet, count, fps, path):
    """어두운 배경과 바닥선 위에 재생하는 GIF (2배 축소 없이 원래 크기)."""
    frames = []
    for i in range(count):
        cell = sheet.crop((i * FRAME_SIZE, 0, (i + 1) * FRAME_SIZE, FRAME_SIZE))
        bg = Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE + 8), (26, 26, 46, 255))
        bg.paste((108, 122, 137, 255), (0, FRAME_SIZE, FRAME_SIZE, FRAME_SIZE + 8))
        bg.alpha_composite(cell)
        frames.append(bg.convert("RGB"))
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=int(1000 / fps), loop=0)


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    reference = None
    for file_name, out_name, count, fps in SHEETS:
        print(f"{file_name} → {out_name}")
        sheet, measured = process(os.path.join(RAW_DIR, file_name), count, reference)
        if reference is None:
            reference = measured
        sheet.save(os.path.join(OUT_DIR, out_name + ".png"))
        save_preview(sheet, count, fps, os.path.join(OUT_DIR, out_name + "_preview.gif"))
    print("완료:", OUT_DIR)


if __name__ == "__main__":
    main()
