"""Export original prefab transforms for native Unity suspension regression checks."""
from pathlib import Path
import csv
import UnityPy

root = Path(__file__).resolve().parents[1]
env = UnityPy.load(str(root.parent / 'Assets/_unpacked/sharedassets1.assets'))
def name(t): return t.m_GameObject.read().m_Name
def children(t): return [p.read() for p in t.m_Children]
def walk(t, path=''):
    path = (path + '/' if path else '') + name(t)
    go = t.m_GameObject.read()
    mesh = next((c.deref().read().m_Mesh.read().m_Name for c in go.m_Components
                 if c.deref().type.name == 'MeshFilter'), '')
    yield [path, go.m_IsActive, *[getattr(t.m_LocalPosition, k) for k in 'xyz'],
           *[getattr(t.m_LocalRotation, k) for k in 'xyzw'],
           *[getattr(t.m_LocalScale, k) for k in 'xyz'], mesh]
    for child in children(t): yield from walk(child, path)
rows = []
cars = set()
for obj in env.objects:
    if obj.type.name != 'Transform': continue
    car = obj.read()
    if car.m_Father.path_id: continue
    model = next((c for c in children(car) if name(c) == 'suspension_model'), None)
    if model is None or not model.m_GameObject.read().m_IsActive: continue
    cars.add(name(car))
    rows.extend([name(car), *row] for row in walk(model))
assert cars == {'Duke', 'Junker', 'PigPen', 'Rustallion', 'Vulture', 'Outrider', 'PipeRat', 'Poloska', 'TinyTyrant'}
output = root / 'verification/native/SuspensionFixtures.csv'
with output.open('w', newline='', encoding='utf8') as f:
    writer = csv.writer(f)
    writer.writerow(['car', 'path', 'active', 'x', 'y', 'z', 'qx', 'qy', 'qz', 'qw', 'sx', 'sy', 'sz', 'mesh'])
    writer.writerows(rows)
print(f'Exported {len(rows)} vanilla transforms from {len(cars)} cars to {output}')
