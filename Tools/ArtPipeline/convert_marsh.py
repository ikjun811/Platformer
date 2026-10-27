"""marsh-sci-fi 원본 팩을 게임용 에셋으로 변환한다 (GDD v2 8.1).

원본: Docs/ArtSources/marsh-sci-fi/scifi/
출력: Assets/_Project/Art/ 아래
  Characters/Marine/   플레이어 시트 (서기, 달리기, 사격, 사망) + 흰 실루엣
  Enemies/<적>/        Runner, Brute, Exploder, Infected 시트 + 흰 실루엣
  Tiles/Corridor/      복도 단면 1~6행 타일 (64px)
  Tiles/Shaft/         샤프트 7~9행 타일 (64px)
  Tiles/Stair/         계단 블록 (64px)
  Props/               콘솔, 얇은 발판, 상자, 테두리
  FX/                  블래스터 탄, 착탄, 총구 화염, 폭발(임시), 혈흔, 체력 조각

가공 내용 (라이선스 3(b) 표기 대상, Docs/Credits.md에 기록):
  - 캐릭터 프레임을 가로 시트로 이어붙임
  - 모든 캐릭터에 1px 외곽선 추가
  - 적 색 변형 (팔레트 색 지정 교체)
  - 흰 실루엣, 관통탄(가로 2배), 폭발(착탄 3배 + 주황) 생성

실행: python Tools/ArtPipeline/convert_marsh.py   (저장소 루트에서)
같은 이름 파일은 덮어쓴다. .meta(GUID)를 지키기 위해 기존 파일을 지우지 않는다.
"""
import os
import shutil
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "Docs", "ArtSources", "marsh-sci-fi", "scifi")
OUT = os.path.join(ROOT, "Assets", "_Project", "Art")

# 원본 → 변형 색 (미리보기에서 확정, 2026-10-27)
BRUTE = {(7, 8, 36): (46, 6, 12), (18, 19, 57): (78, 12, 20), (61, 45, 80): (168, 44, 40),
         (88, 29, 118): (120, 20, 30), (146, 38, 144): (190, 60, 50)}
EXPLODER = {(7, 8, 36): (58, 24, 6), (18, 19, 57): (92, 40, 10), (61, 45, 80): (255, 176, 48),
            (88, 29, 118): (200, 110, 20), (146, 38, 144): (255, 220, 90), (194, 12, 106): (255, 236, 120)}
INFECTED = {(88, 29, 118): (28, 72, 44), (146, 38, 144): (58, 118, 58), (174, 64, 153): (96, 150, 70),
            (192, 83, 119): (120, 168, 84), (224, 153, 130): (176, 196, 140), (194, 12, 106): (206, 224, 70)}

# 캐릭터별 외곽선 색 (몸 색과 같은 계열. 어두운 배경에서 형태가 보이고 밝은 복도에서는 튀지 않게)
OUTLINE = {
    "Marine": (120, 80, 170),
    "Runner": (96, 72, 150),
    "Brute": (150, 44, 40),
    "Exploder": (176, 96, 24),
    "Infected": (64, 124, 72),
}

# 캐릭터 동작: 출력 이름 → (원본 접두어, 프레임 수)
MARINE_CLIPS = {"Idle": ("marine_stand", 2), "Run": ("marine_run", 6),
                "Shoot": ("marine_shoot", 2), "Die": ("marine_die", 5)}
ALIEN_CLIPS = {"Idle": ("alien_idle", 2), "Run": ("alien_run", 5),
               "Bite": ("alien_bite", 5), "Die": ("alien_die", 5)}

# 적: 이름 → (원본 동작 표, 색 변형)
ENEMIES = {
    "Runner": (ALIEN_CLIPS, None),
    "Brute": (ALIEN_CLIPS, BRUTE),
    "Exploder": (ALIEN_CLIPS, EXPLODER),
    "Infected": (MARINE_CLIPS, INFECTED),
}

written = []


def load(rel):
    return Image.open(os.path.join(SRC, rel)).convert("RGBA")


def save(img, rel):
    path = os.path.join(OUT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    written.append(rel)


def copy(src_rel, out_rel):
    path = os.path.join(OUT, out_rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    shutil.copyfile(os.path.join(SRC, src_rel), path)
    written.append(out_rel)


def swap(img, mapping):
    """팔레트 색을 지정한 색으로 바꾼다. 알파는 유지한다."""
    img = img.copy()
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            p = px[x, y]
            if p[3] and p[:3] in mapping:
                px[x, y] = mapping[p[:3]] + (p[3],)
    return img


def outline(img, color):
    """불투명 픽셀의 상하좌우에 맞닿은 투명 픽셀을 외곽선 색으로 칠한다."""
    w, h = img.size
    src = img.load()
    out = Image.new("RGBA", img.size)
    px = out.load()
    for y in range(h):
        for x in range(w):
            if src[x, y][3]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and src[nx, ny][3]:
                    px[x, y] = color + (255,)
                    break
    out.alpha_composite(img)
    return out


def white(img):
    """피격 깜빡임·공격 신호용 흰 실루엣."""
    img = img.copy()
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            if px[x, y][3]:
                px[x, y] = (255, 255, 255, px[x, y][3])
    return img


def sheet(frames):
    """프레임을 왼쪽부터 가로로 이어붙인다 (PlayerAnimator의 시트 형식)."""
    w, h = frames[0].size
    s = Image.new("RGBA", (w * len(frames), h))
    for i, f in enumerate(frames):
        s.alpha_composite(f, (i * w, 0))
    return s


def build_character(folder, name, clips, mapping, line):
    for clip, (prefix, count) in clips.items():
        frames = [load(f"anims/scifi_{prefix}_{i}.png") for i in range(1, count + 1)]
        if mapping:
            frames = [swap(f, mapping) for f in frames]
        frames = [outline(f, line) for f in frames]
        save(sheet(frames), f"{folder}/{name}_{clip}.png")
        save(sheet([white(f) for f in frames]), f"{folder}/{name}_{clip}_White.png")


def build_characters():
    build_character("Characters/Marine", "Marine", MARINE_CLIPS, None, OUTLINE["Marine"])
    for name, (clips, mapping) in ENEMIES.items():
        build_character(f"Enemies/{name}", name, clips, mapping, OUTLINE[name])


def build_tiles():
    for f in sorted(os.listdir(SRC)):
        if not (f.startswith("scifi_") and f.endswith(".png")):
            continue
        code = f[len("scifi_"):-len(".png")]
        group = "Corridor" if code[0] in "123456" else "Shaft"
        copy(f, f"Tiles/{group}/{group}_{code}.png")
    for f in sorted(os.listdir(os.path.join(SRC, "transparancies"))):
        if f.startswith("scifi_stair_"):
            copy(f"transparancies/{f}", f"Tiles/Stair/Stair_{f[len('scifi_stair_'):]}")


def build_props():
    for f in sorted(os.listdir(os.path.join(SRC, "transparancies"))):
        for kind, out_name in (("console", "Console"), ("platform", "Platform")):
            if f.startswith(f"scifi_{kind}_"):
                copy(f"transparancies/{f}", f"Props/{out_name}_{f[len(f'scifi_{kind}_'):]}")
    for f in sorted(os.listdir(os.path.join(SRC, "32x32"))):
        for kind, out_name in (("boxes", "Box"), ("edging", "Edging")):
            if f.startswith(f"scifi_{kind}_"):
                copy(f"32x32/{f}", f"Props/{out_name}_{f[len(f'scifi_{kind}_'):]}")

    save(gas_tank(), "Props/GasTank.png")


def gas_tank():
    """X1 가스탱크 소품 (원본에 따로 없어 만든다).
    가스탱크가 그려진 벽 타일(4j1a)에서 같은 자리의 민 벽(4i1a)과 다른 픽셀만 남겨 탱크를 떼어 내고,
    가장자리 잡티와 바닥 그림자 줄을 지운 뒤 받침이 그림 바닥에 닿도록 내린다."""
    tank = load("scifi_4j1a.png")
    wall = load("scifi_4i1a.png")
    pt, pw = tank.load(), wall.load()
    out = Image.new("RGBA", tank.size)
    po = out.load()
    for y in range(tank.height):
        for x in range(tank.width):
            # 탱크 몸통 영역(x 13~50, y 0~60) 밖은 잡티라 버린다
            if 13 <= x <= 50 and y <= 60 and pt[x, y] != pw[x, y]:
                po[x, y] = pt[x, y]
    bottom = out.getbbox()[3]
    shifted = Image.new("RGBA", out.size)
    shifted.alpha_composite(out, (0, out.height - bottom))
    return shifted


def build_fx():
    fire = [load(f"anims/scifi_blasterfire_{i}.png") for i in (1, 2)]
    save(sheet(fire), "FX/Blaster_Bolt.png")
    # W2 관통탄: 가로만 2배로 늘린다
    save(sheet([f.resize((f.width * 2, f.height), Image.NEAREST) for f in fire]), "FX/Blaster_Pierce.png")

    impact = [load(f"anims/scifi_blasterimpact_{i}.png") for i in range(1, 5)]
    save(sheet(impact), "FX/Blaster_Impact.png")
    save(sheet([load(f"anims/scifi_muzzleflash_{i}.png") for i in (1, 2)]), "FX/Muzzle_Flash.png")

    # 가스탱크 폭발 (임시): 착탄 이펙트를 3배로 키우고 주황 계열로 바꾼다
    boom = []
    for f in impact:
        f = f.resize((f.width * 3, f.height * 3), Image.NEAREST)
        px = f.load()
        for y in range(f.height):
            for x in range(f.width):
                r, g, b, a = px[x, y]
                if a:
                    v = max(r, g, b)
                    px[x, y] = (255, min(255, int(v * 0.75) + 40), int(v * 0.25), a)
        boom.append(f)
    save(sheet(boom), "FX/Explosion.png")

    for f in sorted(os.listdir(os.path.join(SRC, "transparancies"))):
        if f.startswith("scifi_blood_"):
            copy(f"transparancies/{f}", f"FX/Blood_{f[len('scifi_blood_'):]}")

    save(health_shard(), "FX/Health_Shard.png")


def health_shard():
    """카운터 처치 때 떨어지는 체력 조각 (원본에 없어 새로 그림). 10x10 마름모."""
    img = Image.new("RGBA", (10, 10))
    px = img.load()
    edge, body, light = (30, 90, 60), (90, 220, 130), (220, 255, 230)
    for y in range(10):
        for x in range(10):
            d = abs(x - 4.5) + abs(y - 4.5)
            if d <= 4.0:
                px[x, y] = (edge if d > 3.0 else body) + (255,)
    for x, y in ((3, 3), (4, 3), (3, 4)):
        px[x, y] = light + (255,)
    return img


def main():
    if not os.path.isdir(SRC):
        raise SystemExit(f"원본 폴더가 없습니다: {SRC}")
    build_characters()
    build_tiles()
    build_props()
    build_fx()
    groups = {}
    for rel in written:
        key = os.path.dirname(rel)
        groups[key] = groups.get(key, 0) + 1
    for key in sorted(groups):
        print(f"{key:24s} {groups[key]:3d}개")
    print(f"합계 {len(written)}개 → {OUT}")


if __name__ == "__main__":
    main()
