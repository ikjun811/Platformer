"""HUD·메뉴용 픽셀 UI 그림을 만든다 (GDD v2 8.1 UI, N5).

soft_retro 테마(intergenic)의 팔레트와 상자 모양(둥근 모서리, 테두리)을 참고해 640x360 화면에 맞는 크기로 새로 그린다.
원본 테마 파일은 쓰지 않는다. 대부분 흰색으로 그려 두고 게임에서 색을 입힌다 (Image.color).

출력: Assets/_Project/Art/UI/
  파일 이름 끝의 _9sN 은 9-slice 테두리 N 픽셀 (임포터가 읽어 설정한다)
  Panel_9s4.png     둥근 패널 (바탕 #46425e, 테두리 #ffeecc)
  BarBack_9s2.png   막대 바탕 (어두운 칸 + 테두리)
  White.png         흰 사각형 (막대 채움, 표시, 색 칸)
  Pip.png           채운 원 (대시 남은 횟수)
  PipEmpty.png      빈 원 (쓴 대시)
  Fonts/Righteous-Regular.ttf, Fonts/OFL.txt   (soft_retro에 포함된 OFL 폰트를 복사)

실행: python Tools/ArtPipeline/make_ui.py   (저장소 루트에서)
"""
import os
import shutil
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "UI")
FONT_SRC = os.path.join(ROOT, "Docs", "ArtSources", "soft_retro", "Righteous")

# soft_retro 팔레트 (Curiosities)
BG = (0x46, 0x42, 0x5E)
CREAM = (0xFF, 0xEE, 0xCC)
# 막대 바탕용 어두운 색 (팔레트 바탕을 어둡게)
DARK = (0x2A, 0x27, 0x3A)
DARKER = (0x1C, 0x1A, 0x28)
WHITE = (255, 255, 255)


def save(img, name):
    os.makedirs(os.path.dirname(os.path.join(OUT, name)), exist_ok=True)
    img.save(os.path.join(OUT, name))
    print(name, img.size)


def rounded_panel(size, radius, fill, border, alpha=235):
    """둥근 모서리 패널. 모서리는 픽셀 원으로 깎고 1px 테두리를 두른다."""
    img = Image.new("RGBA", (size, size))
    px = img.load()

    def inside(x, y, r):
        # 모서리 원 바깥이면 False
        cx = min(max(x, r), size - 1 - r)
        cy = min(max(y, r), size - 1 - r)
        dx, dy = x - cx, y - cy
        return dx * dx + dy * dy <= r * r + r * 0.6

    for y in range(size):
        for x in range(size):
            if not inside(x, y, radius):
                continue
            edge = not (inside(x - 1, y, radius) and inside(x + 1, y, radius)
                        and inside(x, y - 1, radius) and inside(x, y + 1, radius))
            if x == 0 or y == 0 or x == size - 1 or y == size - 1:
                edge = True
            px[x, y] = (border + (255,)) if edge else (fill + (alpha,))
    return img


def bar_back():
    img = Image.new("RGBA", (6, 6), DARKER + (255,))
    px = img.load()
    for y in range(1, 5):
        for x in range(1, 5):
            px[x, y] = DARK + (255,)
    return img


def circle(size, filled):
    img = Image.new("RGBA", (size, size))
    px = img.load()
    c = (size - 1) / 2
    r = size / 2
    for y in range(size):
        for x in range(size):
            d = ((x - c) ** 2 + (y - c) ** 2) ** 0.5
            if d <= r - 0.3 and (filled or d >= r - 1.5):
                px[x, y] = WHITE + (255,)
    return img


def main():
    save(rounded_panel(12, 3, BG, CREAM), "Panel_9s4.png")
    save(bar_back(), "BarBack_9s2.png")
    save(Image.new("RGBA", (4, 4), WHITE + (255,)), "White.png")
    save(circle(7, True), "Pip.png")
    save(circle(7, False), "PipEmpty.png")

    fonts = os.path.join(OUT, "Fonts")
    os.makedirs(fonts, exist_ok=True)
    for name in ("Righteous-Regular.ttf", "OFL.txt"):
        shutil.copyfile(os.path.join(FONT_SRC, name), os.path.join(fonts, name))
        print("Fonts/" + name)


if __name__ == "__main__":
    main()
