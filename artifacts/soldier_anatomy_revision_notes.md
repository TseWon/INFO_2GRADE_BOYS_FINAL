# Soldier anatomy revision — 2026-10-21

The previous separate cylindrical sleeves and legs had flat shoulder caps and hard breaks at the elbows and pelvis. The approved equipment and nine material assets were preserved.

- Rebuilt the jacket as a continuous shoulder, underarm, torso and sleeve surface.
- Rebuilt trousers around a continuous pelvic saddle, fuller upper thighs, tapered knees and posterior calves.
- Added localized elbow, knee and ankle compression folds, retaining the existing camouflage and textile maps.
- Rebuilt gloves with a tapered palm, varied finger lengths, curled fingertips and opposing thumbs.
- Rebuilt boot uppers and a rounded toe/heel outsole outline.
- Added blended skin weights around shoulders, elbows, hips and knees. All Blender vertices passed normalized-weight and UV checks.

Files: `Assets/Blender/TacticalSoldier/tactical_soldier.blend`, `Assets/Characters/TacticalSoldier/TacticalSoldierVisual.prefab`, `Assets/Scene/Scene1.unity`.

Original model backup: `artifacts/tactical_soldier_before_anatomy.blend`. Comparison: `artifacts/soldier_anatomy_comparison.png`.

Unity validation: nine skinned renderers, nine materials, 17 bones, 135,828 imported vertices, 20,594 camouflage vertices with blended weights. The source GUID and external material remaps were retained. Final import and Scene1/prefab save succeeded.

Play mode verification before the final sole silhouette adjustment passed: moved 1.39999771 m, leg changed 8.328295 degrees, third-person camera distance 2.80000257 m, first-person eye height 1.67 m. Materials, UVs and bone bindings passed. Final sole adjustment was reimported and checked in the saved Unity preview.

This remains a custom procedural game character, not a scan-based or hand-sculpted photoreal character. The existing simple procedural gait and 17-bone rig are retained; individual fingers are not independently rigged.
