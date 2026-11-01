# 소형 지상 연구소 — Surface Research Station

2026-10-29 제작. 첨부한 간략 도면의 배치와 상세 도면의 외장 부품을 참고한 실제 3D 외관 에셋입니다. 도면의 수치 축척이 명확하지 않아 출입문과 계단을 기준으로 크기를 설정했습니다.

## 파일

- `SurfaceResearchStation.blend`: 최종 Blender 원본. `Surface Research Station` 씬을 사용합니다.
- `Textures/`: 1024×1024 PBR 텍스처 12장. 도장 외장, 짙은 기단, 금속 지붕의 Base Color / Roughness / Normal / MetallicSmoothness.
- `AssetReport.json`: 실제 측정 치수, 메시별 삼각형 수, UV 및 FBX 재임포트 검사 기록.
- `MaterialManifest.json`: 재질별 색상, 금속성, 거칠기와 텍스처 경로.
- `../../Environment/SurfaceResearchStation/Models/SurfaceResearchStation.fbx`: 기본 게임 엔진용 모델.
- `../../Environment/SurfaceResearchStation/Models/SurfaceResearchStation_LOD1.fbx`: 거리용 단순화 모델.
- 프로젝트 루트 `artifacts/surface_station/SurfaceResearch_editable_parts.blend`: 부품을 묶기 전 편집용 구성 원본. 개별 창틀, 파이프, 난간 등을 변경할 때 사용합니다.
- 프로젝트 루트 `artifacts/surface_station/`: 정면, 후면, 평면 및 재질 미리보기 PNG와 제작 단계별 원본.

## 실제 규격과 구성

- 폭 30.760m × 깊이 29.399m × 최고 높이 8.024m. 안테나와 계단을 포함한 전체 경계입니다.
- Blender Z-up, 정면 -Y, 원점은 지면 높이의 시설 중심부입니다. FBX는 Y-up으로 내보냈습니다.
- 기본 모델 131개 구역/재질별 메시, 377,080 triangles.
- LOD1 모델 131개 메시, 170,251 triangles. LOD1 컬렉션은 기본적으로 숨겨져 있습니다.
- 중앙 출입동, 좌우 돔, 후방 연구동, 꺾인 연결 통로와 공통 기단.
- 실제 벽 개구부와 창유리, 창틀, 돔 리브, 출입 계단, 난간, 배관, 플랜지, 밸브, 냉각 설비, 안테나, 위성 접시, 서비스 패널 및 표지판.
- 새 건물 전용 PBR 재질. 주 외장에는 미세한 표면 요철과 색/거칠기 변화가 있습니다.

## 검수

- 정면/평면으로 도면의 건물 배치와 실루엣을 비교했습니다.
- 연결 통로의 바닥/지붕을 연속 형상으로 정리하고 후방 기단의 겹치는 면을 통합했습니다.
- 경사진 지붕 설비에는 지붕 각도를 따르는 받침대를 추가했습니다.
- 면적 0인 삼각형을 제거했습니다. 최종 기본 모델 검사 결과 0개입니다.
- 모든 기본 모델 메시에서 UV와 유한 좌표를 확인했습니다.
- FBX를 별도 Blender 검증 씬에 재임포트하여 메시 131개, UV, 연결 텍스처 9장의 경로를 확인했습니다.
- Unity에서 드러난 뒤집힌 면을 Blender 원본에서 재정렬하고 FBX와 LOD1을 다시 내보냈습니다. 작업 전 원본은 `artifacts/backups/SurfaceResearchStation_before_normal_fix.blend`에 보존했습니다.
- 실제 SoloScene1 군인 외형 높이 1.974m, 이동 충돌체 높이 1.88m/지름 0.60m를 확인했습니다. 문틀 안쪽은 중앙 1.56×2.36m, 돔 1.25×2.32m여서 크기를 변경하지 않았습니다.
- 세 출입구 계단에서 같은 규격의 CharacterController가 상부까지 올라가고 닫힌 문 앞에서 멈추는 것을 확인했습니다. 모든 교차 면을 자동으로 검사한 것은 아닙니다.
- 실제 플레이에서 프레임 295→318의 진행을 확인한 뒤 기존 SnowFirstPerson.Move를 사용해 중앙 계단을 올라가도록 검사했습니다. 상부 도달 높이는 약 0.94m이고 검사 중 오류는 0개였습니다. 검증 뒤 원래 플레이어 위치와 편집 상태, 백그라운드 실행 설정을 복원했습니다.

## Unity 적용

`Assets/Scene/SoloScene1.unity`의 `Research Facility - Snow Site` 인스턴스를 제거하고 `Surface Research Station - Snow Site`로 교체했습니다. 원본 대형 연구소 에셋은 삭제하지 않았습니다. 교체 전 현재 씬은 `artifacts/backups/SoloScene1_before_station_20261029_051502.unity`에 보존했습니다.

새 프리팹은 `Assets/Environment/SurfaceResearchStation/Prefabs/SurfaceResearchStation.prefab`입니다. 씬 위치는 (0, 0.015, -42), Y회전 180도입니다. 군용차와 경계 상자가 겹치지 않음을 확인했습니다. 80개의 정적 메시 충돌체, 두 단계 LODGroup, 9개 재질을 적용했습니다. 별도 `Environment/Surface Station Snow PBR` 셰이더로 지붕과 위쪽 면에 눈 효과를 추가했습니다. 기존 군용차/눈 바닥 셰이더는 수정하지 않았습니다.

BaseColor는 sRGB, Normal은 Normal Map으로 설정했습니다. MetallicSmoothness의 R은 금속성, A는 1−Roughness입니다. 재질명은 MaterialManifest를 참고합니다. Unity 검수 이미지와 결과는 `artifacts/surface_station/solo_*` 파일에 있습니다.

## 현재 제작 범위

지상 외관 에셋입니다. 문은 닫힌 상태이며 작동하는 문, 실내 시설, 지하 연구소, 지하 연결 계단/엘리베이터는 아직 제작하지 않았습니다. 프리팹의 `Future Vertical Core`와 `Future B1 Landing`은 위치 참고용 빈 오브젝트입니다. 계획은 `artifacts/surface_station/UndergroundPlan.md`에 있습니다. 군인과 군용차 원본은 수정하지 않았습니다. 미리보기용 바닥, 카메라, 조명, 숨긴 도면 레퍼런스는 FBX에 포함하지 않았습니다.
