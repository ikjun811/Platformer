# Platformer — 프로젝트 규칙

Unity 2D 횡스크롤 플랫포머 게임 프로젝트.

## 환경
- Unity 6000.3.26f1, URP 2D (Renderer2D)
- 입력: 새 Input System 전용 — 프로젝트 전역 액션 `Assets/Settings/InputSystem_Actions.inputactions`
- 설치 패키지: 2D Animation, Aseprite Importer, PSD Importer, SpriteShape, Tilemap (+Extras), Timeline, uGUI

## 1. 작업 프로세스
- 진행 순서: **규칙 → 기획(`Docs/GDD.md`) → 제작**.
- 모든 기능은 GDD에 근거한다. 기획에 없는 기능은 먼저 문서에 추가한 뒤 제작한다.
- **모든 작업은 사용자 확인 후 진행한다.** 제안 → 승인 → 실행 → 결과 보고 → 다음 제안.
- 제안 시 만들 것, 변경할 파일, 예상 결과를 미리 알린다.
- 제작은 마일스톤 단위로 진행한다.

## 2. 폴더 구조
```
Assets/_Project/
  Scripts/   (Player, Enemies, Gameplay, UI, Core, Editor)
  Art/  Audio/  Prefabs/  Scenes/  Data/ (ScriptableObject)
Docs/        (GDD.md, Credits.md, 기술 문서)
```

## 3. 코드 규칙
- 네임스페이스는 `Platformer.*`, 어셈블리 정의(asmdef)로 분리한다.
- 네이밍: 클래스·메서드·프로퍼티 `PascalCase`, private 필드 `camelCase`.
- 인스펙터 노출 필드는 `[SerializeField] private`으로 선언한다. public 필드는 금지.
- 주석은 한국어로 쓴다.
- 밸런스 수치(속도, 점프 높이, 체력 등)는 ScriptableObject로 분리한다. 하드코딩 금지.
- 물리 처리는 `FixedUpdate`, 입력 읽기는 `Update`에서 한다.
- Unity 6 API를 쓴다(`Rigidbody2D.linearVelocity`, `FindObjectsByType` 등).
- `Update` 안에서 `Find*`/`GetComponent`를 호출하지 않는다. 참조는 `Awake`에서 캐싱한다.

## 4. 씬·에셋 규칙
- Claude는 `.unity`, `.prefab`, `.asset` YAML 파일을 직접 수정하지 않는다. 설정은 에디터 스크립트로 자동화하거나 수동 설정 절차를 안내한다.
- 레이어·태그 목록과 스프라이트 PPU(Pixels Per Unit)는 기획 단계에서 정하고 통일한다.

## 5. Git
- **커밋과 푸시는 사용자가 직접 한다.** Claude는 git commit/push를 실행하지 않는다.
- Claude는 작업마다 커밋 단위와 메시지만 제안한다. 형식: `[분류] 내용` (예: `[Player] 점프 버퍼 추가`).
- `.meta` 파일을 함께 커밋해야 하는 경우 따로 짚어준다.
- `main`은 항상 실행 가능한 상태로 유지한다.

## 6. 소통
- 응답은 한국어로 한다.
- 결정이 필요한 사항은 임의로 정하지 않고 사용자에게 묻는다.

## 7. Unity 에디터 작업 안내
- 코드만으로 끝나지 않는 작업은 매번 **"Unity에서 할 일"**로 따로 안내한다.
- 번호 순서, 정확한 메뉴 경로(예: `Edit > Project Settings > Tags and Layers`), 컴포넌트 이름, 인스펙터 입력값을 포함한다.

## 8. 작업 완료 보고 형식
1. 변경·추가된 파일 목록
2. Unity에서 할 일
3. 테스트 방법 (Play 모드에서 확인할 것)
4. 커밋 메시지 제안

## 9. 컴파일·에러 확인
- Claude는 Unity 에디터를 실행해 검증할 수 없다. 코드 작성 후 사용자가 Console 에러·경고를 확인한다.
- 에러 메시지는 그대로 붙여넣어 받는다. 에러를 해결한 뒤에 다음 작업으로 넘어간다.

## 10. 작업 범위 제한
- 승인받은 범위 밖의 코드는 수정하지 않는다. 리팩터링이 필요하면 제안만 한다.
- 작업 한 번에는 기능 하나만 다룬다.

## 11. 패키지·설정 변경
- 패키지 추가·삭제, Project Settings 변경, Unity 버전 변경은 반드시 먼저 승인받는다.

## 12. 외부 에셋과 라이선스
- 외부 에셋(스프라이트, 사운드, 폰트)은 출처와 라이선스를 `Docs/Credits.md`에 기록한다.

## 13. 문서 갱신
- 기능이 완료되면 GDD의 진행 현황(마일스톤 체크리스트)을 갱신한다.
- 기획이 바뀌면 문서를 먼저 수정한 뒤 제작한다.
