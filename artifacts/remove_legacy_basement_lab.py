"""Retire the old basement lab from SoloScene1 and Unity's Assets directory."""
from pathlib import Path
import shutil

root = Path(r"C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL").resolve()
scene = root / 'Assets/Scene/SoloScene1.unity'
old_asset = root / 'Assets/Blender/abandoned_basement_lab.blend'
old_meta = root / 'Assets/Blender/abandoned_basement_lab.blend.meta'
archive = root / 'artifacts/backups'
archive.mkdir(exist_ok=True)
for path in (scene, old_asset, old_meta):
    assert path.resolve().is_relative_to(root) and path.is_file(), path

data = scene.read_bytes()
newline = b'\r\n' if b'\r\n' in data else b'\n'
text = data.decode('utf-8').replace('\r\n','\n')
start = '--- !u!1001 &179034587\n'
assert text.count(start) == 1
block_start = text.index(start)
block_end = text.index('--- !u!',block_start+len(start))
block = text[block_start:block_end]
assert 'guid: 3a5cde0f03536ae46b495ae0ae8b6178' in block
text = text[:block_start] + text[block_end:]
root_entry = '  - {fileID: 179034587}\n'
assert text.count(root_entry) == 1
text = text.replace(root_entry,'')
assert '3a5cde0f03536ae46b495ae0ae8b6178' not in text

backup_scene = archive / 'SoloScene1_before_legacy_lab_removal_20261108.unity'
backup_blend = archive / 'abandoned_basement_lab_legacy_20261108.blend'
backup_meta = archive / 'abandoned_basement_lab_legacy_20261108.blend.meta'
for path in (backup_scene,backup_blend,backup_meta):
    if path.exists():
        raise RuntimeError(f'Refusing to overwrite backup: {path}')
shutil.copy2(scene,backup_scene)
scene.write_bytes(text.replace('\n','\r\n').encode('utf-8') if newline==b'\r\n' else text.encode('utf-8'))
shutil.move(str(old_asset),str(backup_blend))
shutil.move(str(old_meta),str(backup_meta))
print('RETIRED',old_asset)
print('UPDATED',scene)
