"""Extract axle end-cap centres from original meshes; never require readable meshes in game."""
from pathlib import Path
import csv
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler

ROOT = Path(__file__).resolve().parents[1]
env = UnityPy.load(str(ROOT.parent / 'Assets/_unpacked/sharedassets1.assets'))
ends = {}
for obj in env.objects:
    if obj.type.name != 'Transform':
        continue
    t = obj.read()
    name = t.m_GameObject.read().m_Name.lower()
    if 'suspension' not in name or not any(a in name for a in ('front', 'rear')):
        continue
    for c in t.m_GameObject.read().m_Components:
        o = c.deref()
        if o.type.name != 'MeshFilter':
            continue
        mesh = o.read().m_Mesh.read()
        if mesh.m_Name in ends:
            continue
        handler = MeshHandler(mesh)
        handler.process()
        vertices = handler.m_Vertices
        if not vertices:
            continue
        points = []
        for side in (-1, 1):
            x = min(v[0] for v in vertices) if side < 0 else max(v[0] for v in vertices)
            cap = [v for v in vertices if abs(v[0] - x) < .006]
            points.append((x, (min(v[1] for v in cap) + max(v[1] for v in cap)) / 2,
                           (min(v[2] for v in cap) + max(v[2] for v in cap)) / 2))
        ends[mesh.m_Name] = points
with (ROOT / 'Models/AxleEndpoints.csv').open('w', newline='', encoding='utf8') as f:
    w = csv.writer(f)
    w.writerow(['mesh', 'leftX', 'leftY', 'leftZ', 'rightX', 'rightY', 'rightZ'])
    for name, points in sorted(ends.items()):
        w.writerow([name, *points[0], *points[1]])
print('Measured', len(ends), 'axle meshes.')
for name, points in sorted(ends.items()):
    print(name, '; '.join(', '.join(f'{v:.4f}' for v in p) for p in points))
