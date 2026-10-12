"""Read original prefab collider layouts and native attachment FSM recipes, without launching the game."""
from pathlib import Path
import csv
import json
import UnityPy

root = Path(__file__).resolve().parents[1]
source = root.parent / 'itemadjustment/research'
names = (source/'fsm-names.txt').read_text(encoding='utf8').splitlines()
removable = {line.rsplit(' [', 1)[0] for line in names if line.endswith(' [de_Attach]') and '/' not in line}
env = UnityPy.load(str(root.parent/'Assets/_unpacked/sharedassets1.assets'))
def walk(t):
    go = t.m_GameObject.read()
    for c in go.m_Components:
        o = c.deref()
        if o.type.name.endswith('Collider'):
            shape = o.read()
            yield o.type.name, bool(shape.m_Enabled), bool(shape.m_IsTrigger)
    for p in t.m_Children: yield from walk(p.read())
rows=[]
for obj in env.objects:
    if obj.type.name != 'Transform': continue
    t=obj.read(); name=t.m_GameObject.read().m_Name
    if t.m_Father.path_id or name not in removable: continue
    colliders=list(walk(t))
    rows.append([name, ';'.join(f'{kind}:enabled={enabled}:trigger={trigger}' for kind,enabled,trigger in colliders)])
report=root/'research/native-part-colliders.csv'
with report.open('w',newline='',encoding='utf8') as f:
    w=csv.writer(f);w.writerow(['part','prefab_colliders']);w.writerows(sorted(rows))
print('Audited',len(rows),'native removable part prefabs. Collider report:',report)
data=json.loads((source/'fsms.json').read_text(encoding='utf8'))
slots=[]; unsupported=[]
for record in data:
    fsm=record['data']['fsm']
    if fsm['name'] != 'vehPart_Attach': continue
    states={s['name'] for s in fsm['states']}
    recipe='Attach' if 'Attach' in states else 'check type' if 'check type' in states and any(s in states for s in ('Attach turbo','Attach supercharger')) else 'unsupported'
    slots.append([record['path'],recipe,';'.join(sorted(states))])
    if recipe=='unsupported':unsupported.append(record['path'])
with (root/'research/native-slot-recipes.csv').open('w',newline='',encoding='utf8') as f:
    w=csv.writer(f);w.writerow(['socket','recipe','states']);w.writerows(slots)
print('Audited',len(slots),'native slot FSMs;',sum(r[1]=='check type' for r in slots),'forced-induction dispatch sockets.')
print('Unsupported recipes:',unsupported)
assert not unsupported, unsupported
