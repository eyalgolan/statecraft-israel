# Blender headless: each Poly Haven rock glTF -> one decimated mesh (about TARGET tris), OBJ with UVs, plus its diffuse
# and normal maps. blender -b -P decimate.py -- <polyhaven-rocks dir> <out dir> [target]
import bpy, sys, os, shutil, glob
argv = sys.argv[sys.argv.index('--') + 1:]
src, out = argv[0], argv[1]; target = int(argv[2]) if len(argv) > 2 else 700
for d in sorted(glob.glob(os.path.join(src, '*'))):
    name = os.path.basename(d)
    gl = glob.glob(os.path.join(d, '*.gltf'))
    if not gl: continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=gl[0])
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes: o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1: bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    before = sum(len(p.vertices) - 2 for p in o.data.polygons)
    after = before
    for _ in range(6):   # UV seams can stop a collapse short; repeat until near the target
        if after <= target * 1.3: break
        m = o.modifiers.new('dec', 'DECIMATE'); m.ratio = min(1.0, target / max(after, 1)); m.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier='dec')
        after = sum(len(p.vertices) - 2 for p in o.data.polygons)
    dst = os.path.join(out, name); os.makedirs(dst, exist_ok=True)
    bpy.ops.wm.obj_export(filepath=os.path.join(dst, name + '.obj'), export_selected_objects=False, export_materials=False,
                          export_uv=True, export_normals=True, forward_axis='NEGATIVE_Z', up_axis='Y')
    for kind, dstname in (('diff', 'albedo.jpg'), ('nor_gl', 'normal.jpg')):
        f = glob.glob(os.path.join(d, 'textures', f'*_{kind}_1k.jpg'))
        if f: shutil.copyfile(f[0], os.path.join(dst, dstname))
    print(f'ROCK {name} tris {before} -> {after}')
