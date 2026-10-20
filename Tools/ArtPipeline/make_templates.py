"""Gemini에 첨부할 가이드 템플릿 PNG를 만든다 (외부 라이브러리 없이 동작).

배경은 크로마키용 단색 마젠타(#FF00FF), 가이드 선은 어두운 마젠타(#B400B4).
나중에 결과물을 처리할 때 마젠타 계열을 모두 투명으로 지우므로, 가이드 선이 그림에 남아도 함께 지워진다.

모든 템플릿은 높이 1080px이고 같은 위치에 선을 긋는다.
  - 바닥선(GROUND_Y): 서 있는 자세의 발바닥 위치
  - 머리선(HEAD_Y): 서 있는 자세의 머리 끝 위치
  - 두 선의 간격(560px)이 게임 속 키 2.25타일(288px)에 해당한다. 처리 스크립트가 이 간격으로 크기를 맞춘다

실행: python Tools/ArtPipeline/make_templates.py
"""
import os
import struct
import zlib

OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Docs", "ArtSamples", "Gemini", "Templates")

HEIGHT = 1080
GROUND_Y = 920
HEAD_Y = 360
BACKGROUND = (0xFF, 0x00, 0xFF)
GUIDE = (0xB4, 0x00, 0xB4)


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.rows = [bytearray(BACKGROUND * w) for _ in range(h)]

    def rect(self, x0, y0, x1, y1, color):
        x0, x1 = max(0, x0), min(self.w - 1, x1)
        y0, y1 = max(0, y0), min(self.h - 1, y1)
        if x0 > x1:
            return
        span = bytes(color) * (x1 - x0 + 1)
        for y in range(y0, y1 + 1):
            self.rows[y][x0 * 3:(x1 + 1) * 3] = span

    def dashed_hline(self, y, thickness, dash=24, gap=16):
        for x in range(0, self.w, dash + gap):
            self.rect(x, y, x + dash - 1, y + thickness - 1, GUIDE)

    def dashed_vline(self, x, y0, y1, thickness, dash=24, gap=16):
        for y in range(y0, y1, dash + gap):
            self.rect(x, y, x + thickness - 1, min(y + dash - 1, y1), GUIDE)

    def circle(self, cx, cy, r, thickness):
        for y in range(cy - r - thickness, cy + r + thickness + 1):
            for x in range(cx - r - thickness, cx + r + thickness + 1):
                d2 = (x - cx) ** 2 + (y - cy) ** 2
                if (r - thickness) ** 2 <= d2 <= r ** 2:
                    self.rect(x, y, x, y, GUIDE)

    def save(self, path):
        raw = b"".join(b"\x00" + bytes(row) for row in self.rows)

        def chunk(tag, data):
            return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

        png = b"\x89PNG\r\n\x1a\n"
        png += chunk(b"IHDR", struct.pack(">IIBBBBB", self.w, self.h, 8, 2, 0, 0, 0))
        png += chunk(b"IDAT", zlib.compress(raw, 9))
        png += chunk(b"IEND", b"")
        with open(path, "wb") as f:
            f.write(png)


def template(cells, cell_width, ball=False):
    c = Canvas(cells * cell_width, HEIGHT)
    # 바닥선 (실선)
    c.rect(0, GROUND_Y, c.w - 1, GROUND_Y + 5, GUIDE)
    if not ball:
        # 머리선 (점선)
        c.dashed_hline(HEAD_Y, 4)
    for i in range(cells):
        left = i * cell_width
        center = left + cell_width // 2
        # 칸 경계 (실선)
        if i > 0:
            c.rect(left - 2, 0, left + 1, c.h - 1, GUIDE)
        if ball:
            # 모프볼 지름 = 키의 약 44% (1타일 / 2.25타일)
            r = (GROUND_Y - HEAD_Y) * 44 // 200
            c.circle(center, GROUND_Y - r, r, 4)
        else:
            # 몸 중심선 (점선)
            c.dashed_vline(center - 2, HEAD_Y - 120, GROUND_Y, 4)
    return c


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    outputs = {
        "Template_1x.png": template(1, 1080),         # 1:1, 한 프레임
        "Template_4x.png": template(4, 480),          # 16:9, 4프레임
        "Template_6x.png": template(6, 420),          # 21:9, 6프레임
        "Template_Ball4x.png": template(4, 480, True),  # 16:9, 모프볼 4프레임
    }
    for name, canvas in outputs.items():
        canvas.save(os.path.join(OUT_DIR, name))
        print("saved", name, canvas.w, "x", canvas.h)


if __name__ == "__main__":
    main()
