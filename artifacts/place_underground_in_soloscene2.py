"""Place the two separate underground blockouts in SoloScene2 without overlap."""
import re
from pathlib import Path

root = Path(r"C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL")
scene = root / 'Assets/Scene/SoloScene2.unity'
data = scene.read_bytes()
newline = b'\r\n' if b'\r\n' in data else b'\n'
text = data.decode('utf-8')

# A Unity Blender import keeps each .blend as its own prefab instance.  Place
# the B2 plan beside B1 for blockout review; the models remain separate floors.
placements = {
    '271ff5d2c41f4b14e9ad065c092698aa': (0, 0, 0),
    'ad4a46b6eec5cce469173a5aa88bb617': (85, 0, 0),
}
for guid, xyz in placements.items():
    for axis, value in zip('xyz', xyz):
        pattern = (r'(    - target: \{fileID: -8679921383154817045, guid: '
                   + guid + r', type: 3\}\n'
                   + r'      propertyPath: m_LocalPosition\.' + axis + r'\n'
                   + r'      value: )[^\n]+')
        text, n = re.subn(pattern, lambda m: m.group(1) + str(value), text)
        if n != 1:
            raise RuntimeError(f'{guid} position {axis}: matched {n} blocks')

encoded = text.replace('\n', '\r\n').encode('utf-8') if newline == b'\r\n' else text.encode('utf-8')
scene.write_bytes(encoded)
print('UPDATED', scene)
for guid, xyz in placements.items():
    print(guid, xyz)
