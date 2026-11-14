# 연구소 소품 1차 검증 — Batch 01

검증일: 2026-11-14

Meshy 7.1 이미지 기반 PBR 생성 결과를 Blender 5.2.2에 불러와 실제 크기로 정규화하고, 텍스처를 포함한 개별 `.blend` 파일로 저장했다.

| 에셋 | Meshy 작업 ID | 기준 크기 | 면 수 | 파일 크기 | 1차 판정 |
|---|---|---:|---:|---:|---|
| LaboratoryMicroscope | `01a1d7ad-68de-75dd-a878-c3af74e1eabe` | 0.45 m | 792,478 | 51.8 MB | 통과. 쌍안 현미경 형태와 재질이 명확함 |
| ReceptionCounter | `01a1d7ad-2406-75cd-ad24-ff32b0cf1b3b` | 3.0 m | 3,512,479 | 187.5 MB | 조건부 통과. 형태는 양호하나 반드시 리토폴로지 필요 |
| IsolationFence | `01a1d7ad-35a5-7354-851f-cc3b2354195e` | 2.2 m | 1,151,385 | 86.1 MB | 조건부 통과. 격리 구역 배치 가능, LOD 필요 |
| DamagedVentDuct | `01a1d7ad-7965-7473-9215-f17870d1c24b` | 2.5 m | 2,684,552 | 165.6 MB | 조건부 통과. 파손 형상 적합, 리토폴로지 필수 |
| RescueEquipment | `01a1d7ad-85a3-757d-a688-9cfd96a86124` | 1.0 m | 881,998 | 85.8 MB | 통과. 들것과 응급가방 세트로 사용 가능 |
| SpecimenTank | `01a1d7ae-e540-7736-b1cb-7de2ec5f8727` | 2.4 m | 445,489 | 72.7 MB | 용도 변경. 관찰형 시편 탱크보다 극저온 보관 탱크에 적합 |
| ExperimentBench | `01a1d7ae-f146-70bb-9c81-ac20b33dbecb` | 2.0 m | 86,124 | 63.0 MB | 조건부 통과. 높이를 약 0.9 m로 보정 권장 |
| ControlConsole | `01a1d7af-34e2-7399-9dbf-0b6763680726` | 1.8 m | 529,718 | 88.7 MB | 통과. 계기판과 조작부 디테일 양호 |

## Unity 적용 전 필수 작업

1. 10만 면을 초과하는 모델은 리토폴로지 또는 Meshy Smart Topology 버전으로 교체한다.
2. LOD0/LOD1/LOD2를 만들고 충돌체는 단순 박스 또는 캡슐 조합으로 별도 제작한다.
3. 텍스처를 프로젝트 기준에 맞춰 2K 이하로 정리하고 URP 재질을 확인한다.
4. SpecimenTank는 극저온 보관 구역에 배치하고, 내부가 보이는 관찰형 탱크는 별도 제작한다.
5. ExperimentBench의 높이를 0.9 m 전후로 조정한다.

## 저장 위치

- Blender 원본: `Assets/Blender/LabProps/<AssetName>/<AssetName>_Textured.blend`
- 검증 이미지: `Assets/Verification/LabProps/`
- 대용량 `.blend` 파일은 Git LFS 대상으로 등록됨
