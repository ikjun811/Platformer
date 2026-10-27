# Credits — 외부 에셋과 라이선스

외부에서 가져온 에셋의 출처와 라이선스를 기록한다 (CLAUDE.md 규칙 12).

## Creative Commons 에셋
| 에셋 | 제작자 | 출처 | 라이선스 | 위치 | 쓰는 곳 |
|---|---|---|---|---|---|
| Sci-Fi art pack (PROCJAM) | **Marsh Davies** (PROCJAM 커뮤니티 후원) | https://procjam.itch.io | **CC BY-NC 3.0** (저작자 표시, 비상업). 원문 `Docs/ArtSources/marsh-sci-fi/license.txt` | 원본 `Docs/ArtSources/marsh-sci-fi/`, 변환본 `Assets/_Project/Art/` | 타일, 소품, 플레이어(해병), 적(외계 생물), 이펙트 전부 (GDD v2) |

- 게임 크레딧 화면에 "Art by Marsh Davies (PROCJAM), CC BY-NC 3.0"을 표기한다.
- 색 변형, 크기 변경 등으로 가공한 파일은 "원본을 수정함"을 이 문서와 크레딧에 밝힌다 (라이선스 3(b)).
- 수정 내용 (`Tools/ArtPipeline/convert_marsh.py`): 캐릭터 프레임을 가로 시트로 합침, 모든 캐릭터에 1px 외곽선 추가, 적 색 변형(브루트, 폭발체, 감염 병사), 흰 실루엣·관통탄·폭발 이펙트 생성, 가스탱크 벽 타일에서 탱크만 떼어 낸 소품 그림 생성. 체력 조각 아이콘은 새로 그림
- 비상업 조건이므로 판매·유료 배포·광고 수익 등 상업적 이용은 하지 않는다.

## UI 디자인 참고
| 에셋 | 제작자 | 출처 | 라이선스 | 쓰는 곳 |
|---|---|---|---|---|
| Godot Theme: Soft Retro | intergenic | https://intergenic.itch.io/godot-theme-soft-retro | 개인·상업 이용 무료, 출처 표기 권장(필수 아님). 재배포 조건은 명시 없음 | UI 색 팔레트와 상자 모양(둥근 모서리, 테두리)을 참고해 픽셀 9-slice 그림을 새로 만든다 (N5). 원본은 `Docs/ArtSources/soft_retro/`에 두고 저장소에는 올리지 않는다 |
| Curiosities 팔레트 | sukinipan | https://lospec.com/palette-list/curiosities | 페이지에 별도 표기 없음 | soft_retro가 바탕으로 한 팔레트 (#46425e, #15788c, #ff6973, #ffb0a3, #ffeecc) |

## 폰트
| 폰트 | 제작자 | 라이선스 | 쓰는 곳 |
|---|---|---|---|
| Righteous | Astigmatic (Brian J. Bonislawsky) | SIL Open Font License 1.1 (soft_retro에 포함된 `OFL.txt`) | UI 글자 (영어). 게임에 넣을 때 OFL 전문을 함께 둔다. 한글 글리프 없음 |

## 사용 중단한 에셋 (GDD v1, 2026-10-27 프로젝트에서 삭제)
v1 기록 보관용. 게임에는 포함되지 않는다.

| 에셋 | 출처 | 비고 |
|---|---|---|
| Dynamic Space Background Lite (DinV) | Unity Asset Store | 우주 배경 |
| Sci-Fi Texture Pack 2 | Unity Asset Store | 지형 타일 원재료 |
| Sci-Fi GUI Skin (Zekton 폰트 포함) | Unity Asset Store | 일부 파일이 저장소에 커밋된 적이 있음 (git 기록에 남아 있음) |
| 2D Space Backgrounds | 출처 기록 없음 | 저장소에 커밋된 적이 있음 |
| 플레이어 캐릭터 (Google Gemini 생성) | AI 생성 | 명령서·원본은 `Docs/ArtSamples/` |
