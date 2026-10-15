"""Bake the user's static GLB to a compact mesh embedded in the DLL.

Output is Unity left-handed space, long axis Y, 24 cm tip-to-handle.
The original GLB is retained under Models for future visual adjustments.
"""
from pathlib import Path
import json, struct
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
data = (ROOT / 'Models/Screwdriver.glb').read_bytes()
length = struct.unpack_from('<I', data, 12)[0]
doc = json.loads(data[20:20+length])
binary = data[28+length:]

def accessor(index):
    a = doc['accessors'][index]
    v = doc['bufferViews'][a['bufferView']]
    dtype = {5126:'<f4',5125:'<u4',5123:'<u2'}[a['componentType']]
    width = {'SCALAR':1,'VEC2':2,'VEC3':3}[a['type']]
    offset = v.get('byteOffset',0)+a.get('byteOffset',0)
    stride = v.get('byteStride',np.dtype(dtype).itemsize*width)
    return np.ndarray((a['count'],width),dtype=dtype,buffer=binary,offset=offset,strides=(stride,np.dtype(dtype).itemsize)).copy()

# This model contains one static mesh and one primitive, no skin.
p = doc['meshes'][0]['primitives'][0]
vertices = accessor(p['attributes']['POSITION']).astype(float)
normals = accessor(p['attributes']['NORMAL']).astype(float)
scale = np.array(doc['nodes'][1]['scale'])
vertices *= scale
normals /= scale
axes = np.argsort(np.ptp(vertices,axis=0))
vertices = vertices[:,[axes[1],axes[2],axes[0]]]
normals = normals[:,[axes[1],axes[2],axes[0]]]
vertices[:,0] *= -1
normals[:,0] *= -1
vertices -= (vertices.min(axis=0)+vertices.max(axis=0))/2
vertices *= 0.24/np.ptp(vertices[:,1])
# Put the palm at the middle of the handle, leaving the blade above it.
vertices[:,1] += 0.065
normals /= np.linalg.norm(normals,axis=1)[:,None]
uv = accessor(p['attributes']['TEXCOORD_0'])
uv[:,1] = 1-uv[:,1]
faces = accessor(p['indices']).reshape(-1,3)
# Axis normalization can itself reverse handedness. Reverse indices only when
# the complete coordinate transform has a negative determinant.
conversion = np.diag([-1,1,1]) @ np.eye(3)[[axes[1],axes[2],axes[0]]]
if np.linalg.det(conversion) < 0:
    faces = faces[:,[0,2,1]]
out = ROOT/'Models/Screwdriver.mesh'
with out.open('wb') as f:
    f.write(struct.pack('<III',0x55544D31,len(vertices),faces.size))
    f.write(np.hstack([vertices,normals,uv]).astype('<f4').tobytes())
    f.write(faces.astype('<i4').tobytes())
print(f'{out}: {len(vertices)} vertices, {len(faces)} triangles; 0.24 m long')
