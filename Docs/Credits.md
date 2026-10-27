# Credits — 외부 에셋과 라이선스

외부에서 가져온 에셋의 출처와 라이선스를 기록한다 (CLAUDE.md 규칙 12).

## Creative Commons 에셋
| 에셋 | 제작자 | 출처 | 라이선스 | 위치 | 쓰는 곳 |
|---|---|---|---|---|---|
| Sci-Fi art pack (PROCJAM) | **Marsh Davies** (PROCJAM 커뮤니티 후원) | https://procjam.itch.io | **CC BY-NC 3.0** (저작자 표시, 비상업). 원문 `Assets/marsh-sci-fi/license.txt` | `Assets/marsh-sci-fi/` | 타일, 소품, 플레이어(해병), 적(외계 생물), 이펙트 전부 (GDD v2) |

- 게임 크레딧 화면에 "Art by Marsh Davies (PROCJAM), CC BY-NC 3.0"을 표기한다.
- 색 변형, 크기 변경 등으로 가공한 파일은 "원본을 수정함"을 이 문서와 크레딧에 밝힌다 (라이선스 3(b)).
- 비상업 조건이므로 판매·유료 배포·광고 수익 등 상업적 이용은 하지 않는다.

## Unity Asset Store 에셋
> GDD v2(2026-10-27)부터 쓰지 않는다. 정리(N0) 후 이 표에서 뺀다.

Asset Store 표준 라이선스(Standard Unity Asset Store EULA)를 따른다. 게임에 포함해 배포하는 것은 허용되지만 원본 파일 재배포는 금지이므로, 원본 폴더는 저장소에서 제외한다(`.gitignore`). 다른 PC에서는 Asset Store에서 다시 임포트한다.

| 에셋 | 제작자 | 출처 | 위치 | 쓰는 곳 |
|---|---|---|---|---|
| Dynamic Space Background Lite | DinV | https://assetstore.unity.com/packages/2d/textures-materials/dynamic-space-background-lite-104606 | `Assets/DinV/` | 우주 배경 (원본을 그대로 반복 배치) |
| Sci-Fi Texture Pack 2 | Asset Store 페이지 참고 | https://assetstore.unity.com/packages/2d/textures-materials/sci-fi-texture-pack-2-42026 | `Assets/Sci-Fi Texture Pack 2/` | 지형 타일·배경 벽의 원재료. `Tools/ArtPipeline/make_environment.py`로 축소·색 입힘·테두리 추가·창문 가공한 결과만 `Assets/_Project/Art/Environment/`에 둔다 |
| Sci-Fi GUI Skin | Asset Store 페이지 참고 | https://assetstore.unity.com/packages/2d/gui/sci-fi-gui-skin-15606 | `Assets/Sci-Fi UI/` | HUD·메뉴 그림 (M5에서 가공해 `Assets/_Project/Art/UI/`에 둔다). 데모 폴더는 삭제함 |

## AI 생성 이미지
> GDD v1의 플레이어 아트. v2에서 교체된다.

| 대상 | 도구 | 비고 |
|---|---|---|
| 플레이어 캐릭터 | Google Gemini (이미지 생성) | 명령서 `Docs/ArtSamples/GeminiSpec.md`. 생성 결과의 이용 조건은 Google 약관을 따른다 |

## 폰트
| 폰트 | 제작자 | 포함된 곳 | 라이선스 |
|---|---|---|---|
| Zekton (free) | Typodermic | Sci-Fi GUI Skin | 확인 필요 (원작자 라이선스가 따로 적용될 수 있음). 한글 글리프 없음 |
