# HOLLOW CREEK 솔로 캠페인

Unity에서 `Assets/Scene/SoloScene1.unity`를 열고 Play를 누른 뒤 **수색 시작**을 선택합니다. 저장된 진행이 있으면 **체크포인트 계속**이 표시됩니다. 제목은 확정하지 않고 연구시설 이름을 화면 표제로 사용했습니다.

솔로 전용 Windows 실행 파일은 `Builds/HollowCreek/HollowCreek.exe`입니다. 다른 PC로 옮길 때는 `Builds/HollowCreek` 폴더 전체를 복사하세요. 이 실행 파일은 SoloScene1~4만 포함하며 멀티게임으로 전환하지 않습니다.

Unity의 **Game Builds → Solo → Select Scene Group**에서 `SoloWindows` 프로필을 선택하고, **Game Builds → Solo → Build Windows**로 솔로만 빌드합니다. Build Profiles 창에서도 `Assets/BuildProfiles/SoloWindows.asset`을 선택할 수 있습니다. 각 프로필은 전역 씬 목록을 별도로 덮어씁니다. 멀티는 `MultiplayerWindows` 프로필과 별도 실행 파일을 사용합니다.

## 씬 구성

| 씬 | 내용 |
| --- | --- |
| SoloScene1 | 기존 설원·트럭·철문·군인 이동 유지. 동료 4명, 연구자 프로필, 출입 인증, 한국어 튜토리얼 |
| SoloScene2 | 기존 B1/B2 연구소 모델 유지, 충돌체 추가. 승강기 사건과 2.5초 X 구조 분기. 상층 서약 퍼즐 또는 지하 부상자·무른 벽 탈출 |
| SoloScene3 | 기존 지하 모델을 이용한 분기별 수색. 상층 숫자 퍼즐·벽장 놀람, 지하 3×3 성조기 퍼즐. 연구일지 4개, 노트북과 무전 채널 |
| SoloScene4 | 동료 사망 연출, 장애물 추격, 방화문 잠금, 비상구 붕괴, 낮은 환풍구 추격, 연구시설 침하·생존 엔딩 |

상층과 지하의 소품·동료·진입 위치는 분기에 맞춰 선택됩니다. B2 관찰 구역으로 향하는 막힌 벽은 SoloScene3에서만 원본 오브젝트를 비활성 오버라이드로 보존하고, 같은 재질의 벽 양쪽과 퍼즐 연동 출입문으로 구성했습니다. 원본 Blender 모델은 수정하지 않았습니다.

## 조작

- WASD/방향키: 이동
- Shift: 무제한 달리기
- Space: 점프
- E: 청록색 표시가 있는 대상을 바라보고 상호작용
- X: 승강기 사고 중 손 잡기
- J: 확보한 문서 다시 읽기
- Escape: 문서 닫기 또는 일시정지
- V: 기존 1/3인칭 전환 유지. 환풍구에서는 자동으로 1인칭·낮은 자세 적용

단서 표시, 목표, 자막, 퍼즐 입력창, 오답 안내, 재시작 UI를 포함합니다. 문서를 보는 동안 이동이 차단됩니다. 일시정지에서는 QTE와 추격 시간도 멈춥니다. 자동 저장은 씬 진입, 구조 결과, 퍼즐 완료, 문서 수집, 환풍구 진입 시 적용됩니다. 세이브 키는 `HollowCreek.Checkpoint.v1`입니다.

## 퍼즐 정답 (제작자용)

- 정문: 연구자 프로필의 시설 창설년도 `1953`
- CASE 1 서약: 각 줄의 첫 글자 `OPEN`
- 디지털 숫자: `4`. 기획의 수열이 일치하도록 **7의 왼쪽 위 막대도 켜지는 4획 표시기**를 UI에 직접 그렸습니다. 실제 켜진 막대 수는 8→7개, 7→4개, 4→4개입니다. 이 예외는 퍼즐 화면에도 설명합니다.
- CASE 2 성조기: 숫자 1~8을 순서대로 배열하고 오른쪽 아래 빈칸. 완성 상태에서 합법적인 이동을 반복해 섞으므로 항상 풀 수 있습니다.
- 무전기: 시설 서약의 철수일 월일 `1224`

## 오디오·연출 자산

18개의 임시 한국어 대사는 Windows의 Microsoft Heami 음성으로 생성했습니다. `HollowCreekSpeech.lines`에서 최종 성우 녹음으로 교체할 수 있습니다. 무전 재생 시간은 클립 길이를 반영합니다. 낮은 기계음, 금속 발소리·충격, 추격 위협음, 직접 합성한 뒤틀린 겨울 오르골 음악을 포함합니다.

N은 기능 검증용 절차형 모델이며, 동료 사망과 붕괴는 스크립트 연출입니다. 최종 캐릭터 아트·모션캡처·성우 녹음은 포함되지 않습니다. 제공된 원문에는 성조기 이미지 파일 자체가 없어 게임용 이미지를 직접 제작했습니다.

## 유지보수

- `HollowCreekRules`: 퍼즐 정답 및 단계 전이
- `HollowCreekSession`: 씬 간 상태와 체크포인트
- `HollowCreekCampaign`: 사건·상호작용·추격·엔딩
- `HollowCreekHud`: UI Toolkit 화면과 입력
- `HollowCreekTarget`: 조사 대상과 표시
- `HollowCreekAtmosphere`, `HollowCreekSpeech`: 환경음과 대사
- `artifacts/BuildHollowCreek.cs`: 최초 배치 기록. 이미 설치된 프로젝트에서는 재실행을 차단합니다.
- `artifacts/TestHollowCreek.cs`: 규칙, 두 분기, UI 버튼, 추격 동선, 체크포인트 검증

기존 SoloScene1/2의 변경 전 사본은 `artifacts/backups/*_before_campaign_20261109_175716.unity`에 있습니다.
