# 지하 시설 협동 탈출 스크립트

현재 프로젝트(Unity 6000.4, Built-in Render Pipeline, Legacy Input)에 맞춘 게임 로직입니다.
`Assets/Scene/MultiScene1.unity`에 임시 오브젝트와 스크립트 연결을 완료했습니다.

## 같은 Wi-Fi에서 두 노트북으로 실행

1. 두 노트북을 같은 Wi-Fi에 연결합니다. `Builds/FacilityEscape-LAN` 폴더 전체를 두 번째 노트북으로 복사하고, 각각 `FacilityEscape.exe`를 실행합니다. exe 파일만 복사하면 실행되지 않습니다.
2. 첫 번째 노트북은 **HOST / PLAYER A**를 누릅니다. 화면에 표시된 IP 중 Wi-Fi의 IPv4 주소를 친구에게 알려 줍니다. VPN/유선랜이 함께 연결되어 여러 주소가 보이면 Windows Wi-Fi 속성의 IPv4와 일치하는 주소를 사용하세요.
3. 두 번째 노트북은 **Host IPv4** 칸에 그 주소를 입력하고 **JOIN / PLAYER B**를 누릅니다. `127.0.0.1`은 같은 컴퓨터에서 두 실행을 시험할 때만 사용합니다.
4. 두 명이 연결되면 방장에게 START 버튼이 표시됩니다. 방장만 Space/START로 시작하고 R/RESTART로 새 라운드를 시작할 수 있습니다.
5. A는 흑백 화면에서 버튼·다이얼·레버를 조작합니다. B는 컬러 화면과 벽 안내판을 보고 설명합니다. 각 노트북은 자기 역할만 조종하며 Tab으로 바뀌지 않습니다.
6. 마우스로 360도 둘러보고, 중앙 `+`를 조작할 장치에 맞춰 좌클릭합니다. Esc는 커서를 풀고, 방 안의 빈 화면을 클릭하면 시점 조작으로 돌아갑니다.
7. 서로의 임시 캐릭터와 고개 방향을 볼 수 있습니다. 탈출하면 두 노트북 모두 타이머가 사라지고, WASD로 각자 열린 출구를 향해 이동합니다.
8. LEAVE ROOM 또는 상대의 연결 종료 시 양쪽 모두 대기 상태로 초기화합니다. 다시 HOST/JOIN으로 연결할 수 있습니다. 접속 중 취소는 LEAVE / CANCEL입니다.

Windows 방화벽이 첫 실행 시 묻는 경우 사용하는 사설 네트워크에서 게임의 통신을 허용하세요. 연결 포트는 UDP 7777입니다. 같은 Wi-Fi라도 게스트 네트워크나 AP 격리가 켜져 있으면 노트북끼리 통신할 수 없습니다. 이 버전은 LAN 직접 접속용이며 인터넷 중계나 내장 음성 채팅은 포함하지 않습니다. 설명은 서로 대화하거나 별도 음성 통화를 사용하세요.

Unity에서는 `Assets/Scene/MultiScene1.unity`를 열고 Play하면 같은 접속 화면이 나옵니다. 에디터 하나와 실행 파일 하나로도 테스트할 수 있습니다. 명령행 바로가기는 `-escape-host` 또는 `-escape-join 192.168.0.10`입니다.

현재 씬의 버튼 배치는 왼쪽부터 **파랑 / 노랑 / 빨강 / 초록**이며, **정답은 매 라운드 랜덤**입니다. B의 안내판에서 이번 정답을 읽어야 합니다.
색상은 네 색을 한 번씩 쓰는 순서로, 기호는 6개 기호의 3자리 조합으로 생성합니다. 두 퍼즐 모두 직전 라운드와 같은 정답은 제외하며, 기호 `O/O/O`도 제외합니다. 오답 재시도로는 정답이 바뀌지 않습니다.
탈출 성공 시 타이머가 사라지고, 재시작하면 다시 표시됩니다. 고정 정답으로 테스트하려면 Game Inspector의 `Randomize Answers`를 끕니다.

## 에셋 교체

- 씬의 `MultiEscapePrototype` 아래에 방, 의자, 플레이어, 조작 패널, 안내판, 출구, HUD를 분리했습니다.
- 버튼/다이얼의 `Visual_ReplaceMesh`, 의자의 `Visual_ReplaceWithChairAsset` 아래 임시 외형을 교체하세요.
- `KeepLogicRoot`로 표시된 버튼·레버의 부모, Collider와 `MultiEscapeInteractable`은 유지해야 합니다. 교체 모델의 불필요한 Collider가 버튼 앞을 막지 않도록 하세요.
- 구속장치 오브젝트는 `MultiEscapeView`에서 직접 참조합니다. 해당 오브젝트를 유지하고 Renderer/Mesh를 바꾸거나, 새 오브젝트로 교체했다면 View 참조도 변경하세요.
- 다이얼 회전축은 Z이며, 표시 기호는 `SelectedSymbol` TextMesh입니다. 실제 기호 모델/텍스처를 사용한다면 이 표시를 교체할 수 있습니다.
- 카메라/캐릭터 리그는 `03_PlayerRigs`, 게임 설정과 사운드는 `00_GameSystems`에 있습니다.
- 새 씬에서 사용하려면 `Assets/MultiplayerPrototype/Prefabs/FacilityEscapePrototype.prefab`을 배치하세요. 기존 카메라/AudioListener의 중복 활성화는 피해야 합니다.
- 임시 머티리얼·경고음·충격음·해제음은 `Assets/MultiplayerPrototype`에 있습니다. 외부 에셋 다운로드 없이 기본 메시와 생성한 효과음을 사용했습니다.
- HUD는 `UI/MultiEscapeHud.uxml`, `UI/MultiEscapeHud.uss`, `UI/MultiEscapeLocalSession.cs`로 구성됩니다. A에게 컬러 정보가 새지 않도록 HUD는 무채색입니다.

## 기본 규칙

- 제한 시간 60초, A/B 각각 하트 3개.
- 기획의 `피해 간격 40초`와 `20·40·60초 피해`가 상충하여 **시간표 기준 20초**를 기본값으로 사용합니다. `Damage Interval`에서 변경할 수 있습니다.
- 20초: 각자 2개, 40초: 각자 1개, 60초: 각자 0개 및 팀 실패.
- 피해는 동시에 적용합니다. 오답으로 HP가 감소하지 않습니다.
- 색상 오답: 입력 진행 초기화, 3초 잠금, 경고음. 잠금 중 입력은 무시합니다.
- 색상 정답: 양쪽 손목 해제, 기호 다이얼 패널 표시.
- 다이얼은 3개이며 각각 6개 기호를 순환합니다. 레버 오답은 세 다이얼 모두 원으로 초기화합니다.
- 두 장치 해제 시 성공. 시간이 끝나거나 어느 쪽이든 HP가 0이면 실패하며 이후 입력/피해는 중지됩니다.
- 시간은 실제 경과 시간 기준입니다. `Time.timeScale = 0`이나 Game 컴포넌트 비활성화는 타이머를 일시정지하지 않습니다.

## 파일 역할

| 파일 | 역할 |
|---|---|
| `MultiEscapeRules.cs` | 역할 검증, 퍼즐 판정, HP/시간, 재시작을 담당하는 순수 C# 규칙 |
| `MultiEscapePuzzleGenerator.cs` | 색상 순서와 기호 정답 생성, 직전 정답 반복 방지 |
| `MultiEscapeGame.cs` | 두 참가자 연결, 자동 시작, 효과음 및 UnityEvent 발행 |
| `MultiEscapePlayer.cs` | 역할별 카메라, 조작 권한, 마우스 입력, 이동 제한 |
| `MultiEscapeInteractable.cs` | 물리 버튼, 다이얼 회전 버튼, 확인 레버 |
| `MultiEscapeView.cs` | 구속장치/패널/하트 표시, 다이얼 회전, 남은 시간 표시 |
| `MultiEscapeClueBoard.cs` | B에게만 보이는 정답 안내판 |
| `MultiGrayscaleCamera.cs` | A 카메라 전용 흑백 효과 |
| `MultiEscapeWalk.cs` | 성공 후 이동 허용, 일어나는 위치와 재시작 위치 복구 |
| `MultiEscapeFeedback.cs` | 출구 문 열림, 레버 기울기, 양쪽 의자의 충격 플래시 |
| `MultiEscapeWorldText.cs` | 벽 뒤의 글자가 비치지 않도록 TextMesh 깊이 검사 적용 |
| `UI/MultiEscapeLocalSession.cs` | LAN 접속 UI, 역할별 로컬 카메라, 시작/재시작, 커서 관리 |
| `Resources/MultiEscapeGrayscale.shader` | 빌드에 포함되는 흑백 셰이더 |

## 다른 씬에 수동으로 연결하는 경우

1. 항상 활성 상태인 빈 오브젝트에 `MultiEscapeGame`을 붙입니다.
2. 플레이어 오브젝트 두 개에 각각 `MultiEscapePlayer`를 붙입니다.
   - A: Role = `Blindfolded`. B: Role = `Observer`.
   - 두 컴포넌트의 Game을 1번 오브젝트에 연결하고, 서로 다른 View Camera를 지정합니다.
   - Game의 Player A / Player B도 각각 연결합니다.
   - 이 컴퓨터에서 입력을 받을 플레이어 하나만 `Local Input`을 켭니다. 카메라 활성화, 분할 화면 Rect, AudioListener 선택은 씬에서 설정합니다.
   - 의자에 묶여 있는 동안 비활성화할 기존 이동 스크립트를 `Movement While Free`에 넣습니다. 성공하면 원래 활성 상태로 복구됩니다. 게임 시작 전에 켜져 있어야 성공 시 다시 켜집니다.
   - 이동 스크립트가 카메라/마우스도 제어한다면 성공 후 그 스크립트가 입력을 이어받도록 설정합니다.
3. 빨강/파랑/노랑/초록 버튼에 각각 `MultiEscapeInteractable`을 붙입니다.
   - Control = `ColorButton`, Color = 실제 버튼의 색, Game 연결.
   - 버튼 또는 자식에 일반 Collider를 붙입니다. 버튼의 머티리얼 색상은 직접 지정합니다.
   - A에게 색 이름을 노출하지 말고 위치/번호로 구분합니다. B는 실제 버튼 배치를 보고 A에게 위치를 설명합니다.
4. 다이얼마다 시계/반시계 버튼 두 개를 만듭니다.
   - Control = `DialClockwise` 또는 `DialCounterclockwise`.
   - Dial Index는 왼쪽부터 0, 1, 2. Game과 Collider 연결.
   - 기호 순환은 원 → 삼각형 → 사각형 → 별 → 마름모 → 십자가입니다.
   - 확인 레버는 Control = `ConfirmLever`로 설정합니다.
5. 별도의 항상 활성 상태인 오브젝트에 `MultiEscapeView`를 붙입니다.
   - 양쪽 의자의 손목 오브젝트를 Wrist Restraints에, 나머지를 Remaining Restraints에 넣습니다.
   - Symbol Panel에는 다이얼과 레버를 묶은 루트, Dial Visuals에는 회전할 다이얼 3개를 넣습니다.
   - 최초 다이얼 회전은 원 기호가 선택된 상태여야 합니다. 회전축은 기본 로컬 Z축이며 모델에 맞춰 변경합니다.
   - 각 플레이어의 하트 오브젝트를 왼쪽부터 3개 연결합니다. 필요하면 Timer Text, Status Text, Dial Texts에 월드 공간 TextMesh를 연결합니다.
   - `MultiEscapeView`와 Game 자체를 Symbol Panel/하트/구속장치/성공·실패 패널 아래에 두지 마세요. 패널을 숨기면 갱신도 멈춥니다.
6. 벽 앞에 정답 문구 전용 TextMesh 오브젝트를 만들고 `MultiEscapeClueBoard`를 붙여 Game을 연결합니다.
   - Clue Layer는 기본 30입니다. 프로젝트에서 사용하지 않는 8~31 레이어를 정하고, 두 Player와 안내판에서 같은 값을 사용합니다.
   - 안내판 전용 레이어에 벽/버튼 등 다른 물체를 넣지 마세요. A의 카메라는 이 레이어를 렌더링하지 않습니다.
   - A에게 문구가 반사로 보이지 않도록 거울/실시간 Reflection Probe의 Culling Mask에서도 안내판 레이어를 제외합니다.
   - 안내판은 Game에 지정한 정답을 그대로 표시하므로 정답을 따로 편집할 필요가 없습니다.
7. Game의 Audio Source와 경고/충격/해제 AudioClip을 연결합니다. 두 의자의 충격 애니메이션/이펙트는 `On Electric Shock` 이벤트에 모두 연결할 수 있습니다.
8. Game의 `On Escaped`에 문 열기/탈출 애니메이션을, `On Failed`에 실패 연출을 연결합니다. 다시 시작하려면 `StartRound()`를 호출합니다. 외부 애니메이션/문도 `On Round Started`에서 초기화합니다.

커서를 고정한 상태에서 마우스로 둘러보고 중앙 조준점으로 장치를 조작합니다. 다른 씬에서도 로컬 세션 또는 입력 관리자가 커서 고정을 담당해야 합니다. `Interaction Distance` 기본값은 3m입니다. Interaction Mask에는 버튼뿐 아니라 시야를 가리는 벽도 포함해야 벽 너머 조작을 막을 수 있습니다. Trigger Collider는 상호작용 대상에서 제외됩니다.

`Randomize Answers`를 끈 경우에만 Inspector의 Color Answer / Symbol Answer를 고정 정답으로 사용합니다.

## 표시 및 확장

- TextMesh는 기본 폰트 호환을 위해 영문/ASCII 기호로 표시합니다. 실제 기호 메시/텍스처를 사용하거나 `SymbolText`를 수정할 수 있습니다.
- 흑백 효과는 A의 카메라에 자동으로 추가됩니다. B의 카메라와 공용 머티리얼에는 영향을 주지 않습니다.
- Built-in 전용입니다. URP/HDRP로 이전하면 해당 파이프라인의 카메라별 후처리 구현이 필요합니다.
- Screen Space Overlay Canvas는 카메라 후처리 대상이 아닙니다. A의 UI는 무채색으로 만들거나 카메라/월드 공간에서 렌더링하세요.
- HUD 프레임워크를 사용한다면 Game의 `On Time Changed`, `On Player A/B Hearts Changed` 이벤트로 연결할 수 있습니다. 값은 상태 갱신 시 전달됩니다.

## LAN 동기화 구조

`MultiEscapeLanSession`은 Netcode for GameObjects와 Unity Transport로 두 명을 연결합니다. Unity 계정이나 클라우드 프로젝트 설정 없이 방장 IP로 직접 접속합니다. 방장은 A, 참가자는 B로 고정됩니다. 세 번째 참가자는 거부합니다.

- 방장만 정답 생성, 시간/피해 계산, 퍼즐 판정, 재시작을 실행합니다. B의 화면은 방장이 보내는 스냅샷을 표시합니다.
- 잠금 진행, 다이얼, HP, 타이머, 해제, 경고음, 충격, 탈출, 재시작 상태를 동기화합니다. 접속을 기다리는 동안 시간은 흐르지 않습니다.
- 각자 카메라와 입력을 갖고 상대 리그의 위치와 고개 회전을 복제합니다. 구속 중에는 위치를 옮길 수 없으며 탈출 후에만 각자 이동합니다.
- `MultiEscapeAvatar`는 자신의 임시 모델은 숨기고 상대 모델을 표시합니다. `03_PlayerRigs/Player_*/Avatar_ReplaceVisuals`의 외형을 교체할 수 있습니다. Head Look Pivot 및 seated/standing 참조를 유지하세요.
- 별도 NetworkObject 프리팹을 생성하지 않고 두 실행에 이미 있는 A/B 리그를 동기화합니다. 두 노트북은 같은 빌드를 사용해야 합니다.
- 화면상 정보 분리는 협동 퍼즐을 위한 것이며 변조 클라이언트에 대한 안티치트는 아닙니다. 호스트 마이그레이션과 인터넷 Relay는 포함하지 않습니다.

## 검증

`Editor/MultiEscapeChecks.cs`의 `FacilityEscape.Editor.MultiEscapeChecks.Run()`은 별도 테스트 패키지 없이 규칙의 경계 조건과 흑백 셰이더 출력을 확인합니다. Unity Pipeline의 `eval`에서 `return FacilityEscape.Editor.MultiEscapeChecks.Run();`으로 실행할 수 있습니다. 기존 씬은 변경하지 않습니다.

`artifacts/TestMultiEscapePrototype.cs`는 Play 모드에서 카메라 → 버튼 Collider까지 실제 레이캐스트, UI 가림 여부, 역할별 권한, 실제 3초 잠금, HP/실패, 해제/문 열림, 탈출 경로의 충돌과 재시작을 검사합니다. 20/40/60초 경계는 Game의 시계 기준점을 테스트에서 이동시켜 검증합니다. Play 모드에서 `run_script`의 진입점 `TestMultiEscapePrototype.Run`으로 실행하며 테스트 도중 게임 상태가 바뀝니다.

씬 생성 코드는 `artifacts/BuildMultiEscapePrototype.cs`, 초기 배치 조정은 `artifacts/TuneMultiEscapePrototype.cs`에 보관했습니다. 생성기는 중복 배치를 막기 위해 이미 프로토타입이 있는 씬에서는 실행을 거부합니다. 현재 씬에서 다시 실행할 필요는 없습니다.

LAN 검증: `artifacts/TestMultiEscapeLan.cs`는 에디터 호스트와 별도 Development Player 클라이언트 사이에서 실제 네트워크 연결, 역할 제한, 정답/타이머/HP, 물리 버튼 레이캐스트, 다이얼, 탈출, 위치/고개 복제, 재시작, 실패, 양쪽 연결 종료와 재접속을 확인합니다. Development Player에 `-escape-join 127.0.0.1 -escape-lan-probe <절대 경로>/artifacts/lan-client.json`을 전달한 뒤 Play 모드 에디터에서 `TestMultiEscapeLan.Run`을 실행합니다. `MultiEscapeLanProbe`는 명시적 테스트 인자가 있을 때만 작동하며 Release 빌드에서는 제외됩니다. 이전 `TestMultiEscapePrototype`은 네트워크 연결 전의 로컬 프로토타입용 테스트입니다.
