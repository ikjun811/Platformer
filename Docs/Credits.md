# Credits — 외부 에셋과 라이선스

외부에서 가져온 에셋의 출처와 라이선스를 기록한다 (CLAUDE.md 규칙 12).

## Unity Asset Store 에셋
Asset Store 표준 라이선스(Standard Unity Asset Store EULA)를 따른다. 게임에 포함해 배포하는 것은 허용되지만 원본 파일 재배포는 금지이므로, 원본 폴더는 저장소에서 제외한다(`.gitignore`). 다른 PC에서는 Asset Store에서 다시 임포트한다.

| 에셋 | 제작자 | 출처 | 위치 | 쓰는 곳 |
|---|---|---|---|---|
| Dynamic Space Background Lite | DinV | https://assetstore.unity.com/packages/2d/textures-materials/dynamic-space-background-lite-104606 | `Assets/DinV/` | 우주 배경 (원본을 그대로 반복 배치) |
| Sci-Fi Texture Pack 2 | Asset Store 페이지 참고 | https://assetstore.unity.com/packages/2d/textures-materials/sci-fi-texture-pack-2-42026 | `Assets/Sci-Fi Texture Pack 2/` | 지형 타일·배경 벽의 원재료. `Tools/ArtPipeline/make_environment.py`로 축소·색 입힘·테두리 추가·창문 가공한 결과만 `Assets/_Project/Art/Environment/`에 둔다 |
| Sci-Fi GUI Skin | Asset Store 페이지 참고 | https://assetstore.unity.com/packages/2d/gui/sci-fi-gui-skin-15606 | `Assets/Sci-Fi UI/` | HUD·메뉴 그림 (M5에서 가공해 `Assets/_Project/Art/UI/`에 둔다). 데모 폴더는 삭제함 |

## AI 생성 이미지
| 대상 | 도구 | 비고 |
|---|---|---|
| 플레이어 캐릭터 | Google Gemini (이미지 생성) | 명령서 `Docs/ArtSamples/GeminiSpec.md`. 생성 결과의 이용 조건은 Google 약관을 따른다 |

## 폰트
| 폰트 | 제작자 | 포함된 곳 | 라이선스 |
|---|---|---|---|
| Zekton (free) | Typodermic | Sci-Fi GUI Skin | 확인 필요 (원작자 라이선스가 따로 적용될 수 있음). 한글 글리프 없음 |
