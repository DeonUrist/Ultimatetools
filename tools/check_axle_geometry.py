"""Small offline geometry check/report; no game launch and no save access."""
from pathlib import Path
import csv
import UnityPy

ROOT = Path(__file__).resolve().parents[1]
with (ROOT / 'Models/AxleEndpoints.csv').open(encoding='utf8') as f:
    points = {r['mesh']: [(float(r['leftX']), float(r['leftY']), float(r['leftZ'])),
                          (float(r['rightX']), float(r['rightY']), float(r['rightZ']))] for r in csv.DictReader(f)}
env = UnityPy.load(str(ROOT.parent / 'Assets/_unpacked/sharedassets1.assets'))
def name(t): return t.m_GameObject.read().m_Name
def children(t): return [p.read() for p in t.m_Children]
def mesh(t):
    for c in t.m_GameObject.read().m_Components:
        obj = c.deref()
        if obj.type.name == 'MeshFilter': return obj.read().m_Mesh.read()
def project(t, p):
    q, s, v = t.m_LocalRotation, t.m_LocalScale, t.m_LocalPosition
    x, y, z = [p[i] * getattr(s, 'xyz'[i]) for i in range(3)]
    tx, ty, tz = 2*(q.y*z-q.z*y), 2*(q.z*x-q.x*z), 2*(q.x*y-q.y*x)
    return (x+q.w*tx+q.y*tz-q.z*ty+v.x, y+q.w*ty+q.z*tx-q.x*tz+v.y, z+q.w*tz+q.x*ty-q.y*tx+v.z)
def walk(t, ancestors=()):
    yield t, ancestors
    for c in children(t): yield from walk(c, (t, *ancestors))
rows=[]; swaps=0; chassis={}
for obj in env.objects:
    if obj.type.name != 'Transform': continue
    car=obj.read()
    if car.m_Father.path_id: continue
    model=next((c for c in children(car) if name(c)=='suspension_model'),None)
    if model is None or not model.m_GameObject.read().m_IsActive: continue
    chassis[name(car)]=[]
    for variant in children(model):
        axles={}
        for t, ancestors in walk(variant):
            n=name(t).lower(); m=mesh(t)
            if m is None: continue
            if n in ('suspension_lift','undershield','shield','lifted bottom'): chassis[name(car)].append(n)
            if 'suspension' not in n or not any(a in n for a in ('front','rear')): continue
            assert m.m_Name in points, (name(car), m.m_Name)
            ends=[]
            for p in points[m.m_Name]:
                for transform in (t, *ancestors, model): p=project(transform,p)
                ends.append(p)
            axle='front' if 'front' in n else 'rear'
            axles[axle]=(t,m)
            for side, p in zip(('left','right'),ends): rows.append([name(car),name(variant),axle,side,*p])
        if name(car) in ('Poloska','PipeRat','TinyTyrant'):
            assert 'front' in axles and 'rear' in axles
            t, rm=axles['rear']; fm=axles['front'][1]
            fl,fr=points[fm.m_Name]; rl,rr=points[rm.m_Name]
            factor=(rr[0]-rl[0])/(fr[0]-fl[0])
            # Replacement matches original rear endpoint centre and width.
            for i in range(3):
                shift=(rl[i]+rr[i])*.5-(fl[i]+fr[i])*.5*(factor if i==0 else 1)
                for old,new in ((rl,fl),(rr,fr)):
                    assert abs(old[i]-(new[i]*(factor if i==0 else 1)+shift)) < .001, (name(car), i)
            swaps+=1
assert len(chassis)==9, chassis
assert 'lifted bottom' in chassis['Duke']
out=ROOT/'research/axle-endpoints-car-xyz.csv'
with out.open('w',newline='',encoding='utf8') as f:
    w=csv.writer(f);w.writerow(['vehicle','variant','axle','side','x','y','z']);w.writerows(rows)
print('Checked',len(chassis),'supported car geometries,',len(rows),'axle endpoints,',swaps,'reversible rear swaps.')
for car,parts in chassis.items(): print(car, 'fixed chassis pieces:', ', '.join(parts) or 'none')
print('Report:',out)
