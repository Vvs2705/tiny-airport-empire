"""elenco_ta.py - Blender headless: personagem HD do Tripo (rig Mixamo, 50 000 faces, 5 texturas 4K) -> .blend leve para o
render_sprites.py, com o uniforme num material proprio (a mascara que a View tinge com a cor do setor; ART_BIBLE §2).

Por que: o FBX HD traz 5 texturas 4K (base, metal, normal, rm, rugosidade); o Workbench so usa a base. No PC de 7,7 GB o
import + render dos 12 personagens de uma vez paginou ate travar (29/10). Aqui: 1 personagem por vez, so a textura base,
reduzida para --tex px e embutida no .blend; as outras imagens saem sem nunca carregar os pixels.

Uniforme: cada face cuja cor media na textura (amostrada pela UV) fica a menos de `tol` (distancia RGB 0..1) de uma das cores
da tabela UNIFORME vai para o material "TA_MASK"; o render_sprites.py --mask renderiza esse material em branco e o resto em preto.

Uso:
  blender.exe -b --factory-startup --python-exit-code 1 --python elenco_ta.py -- --src <arte/tripo> --out <pasta>
      [--only atendente,piloto] [--tex 1024] [--preview <pasta>]
Saida: <pasta>/<nome>.blend e, com --preview, <nome>.png (de frente, o uniforme em magenta). "ELENCO_OK" no log.
"""

import argparse, glob, math, os, sys
import bpy
import numpy as np
from mathutils import Matrix

# nome: ([cores do uniforme em hex, como na textura], tolerancia RGB, faixa de altura (0 = pe, 1 = topo da cabeca)). Sem cor =
# personagem sem mascara (passageiro, que tinge a mala, e o uniforme fixo da funcao no patio). Cores lidas no preview (luz chapada).
UNIFORME = {
    "passageiro_a": ([], 0.0, 0.0, 1.0),
    "passageira_b": ([], 0.0, 0.0, 1.0),
    "passageiro_idoso": ([], 0.0, 0.0, 1.0),
    "crianca": ([], 0.0, 0.0, 1.0),
    "atendente": (["#022754"], 0.12, 0.44, 0.84),
    "seguranca": (["#59595F", "#424146"], 0.08, 0.45, 0.85),
    "vendedor": (["#87A490"], 0.1, 0.25, 0.8),
    "bagageiro": (["#D87838", "#E88838"], 0.14, 0.35, 0.85),
    "mecanico": (["#286878", "#287888"], 0.12, 0.1, 0.85),
    "despachante": (["#D87838", "#E88838", "#C8B858"], 0.14, 0.35, 0.85),
    "piloto": ([], 0.0, 0.0, 1.0),
    "lara": ([], 0.0, 0.0, 1.0),
    "comissaria": ([], 0.0, 0.0, 1.0),
    "comissario": ([], 0.0, 0.0, 1.0),
}


def die(msg):
    print("ERRO " + msg)
    sys.exit(1)


def rgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i : i + 2], 16) / 255.0 for i in (0, 2, 4)])


def base_node(m):
    bsdf = next((n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf:
        for link in bsdf.inputs["Base Color"].links:
            if link.from_node.type == "TEX_IMAGE":
                return link.from_node
    return next(
        (
            n
            for n in m.node_tree.nodes
            if n.type == "TEX_IMAGE" and n.image and "base" in n.image.name.lower()
        ),
        None,
    )


def uniforme(ob, img, cores, tol, z0, z1):
    """Faces do uniforme -> material TA_MASK (copia do material base, mesma textura). Devolve a fracao de faces."""
    me = ob.data
    w, h = img.size
    px = np.empty(w * h * 4, np.float32)
    img.pixels.foreach_get(px)
    px = px.reshape(h, w, 4)
    uv = np.empty(len(me.loops) * 2, np.float32)
    me.uv_layers.active.data.foreach_get("uv", uv)
    uv = uv.reshape(-1, 2)
    start = np.empty(len(me.polygons), np.int32)
    total = np.empty(len(me.polygons), np.int32)
    me.polygons.foreach_get("loop_start", start)
    me.polygons.foreach_get("loop_total", total)
    centro = np.add.reduceat(uv, start, axis=0) / total[:, None]
    xs = np.clip((centro[:, 0] % 1.0) * (w - 1), 0, w - 1).astype(int)
    ys = np.clip((centro[:, 1] % 1.0) * (h - 1), 0, h - 1).astype(int)
    cor = px[ys, xs, :3]
    dist = np.min([np.linalg.norm(cor - rgb(c), axis=1) for c in cores], axis=0)
    co = np.empty(len(me.vertices) * 3, np.float32)
    me.vertices.foreach_get("co", co)
    M = np.array(ob.matrix_world)
    zv = (co.reshape(-1, 3) @ M[:3, :3].T + M[:3, 3])[:, 2]
    vi = np.empty(len(me.loops), np.int32)
    me.loops.foreach_get("vertex_index", vi)
    zf = (np.add.reduceat(zv[vi], start) / total - zv.min()) / max(float(np.ptp(zv)), 1e-6)
    sel = (dist < tol) & (zf >= z0) & (zf <= z1)
    base = me.materials[0]
    mask = base.copy()
    mask.name = "TA_MASK"
    me.materials.append(mask)
    idx = np.zeros(len(me.polygons), np.int32)
    idx[sel] = len(me.materials) - 1
    me.polygons.foreach_set("material_index", idx)
    me.update()
    return float(sel.mean())


def preview(path):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light, sc.display.shading.color_type = "FLAT", "TEXTURE"
    sc.render.film_transparent = True
    sc.render.resolution_x, sc.render.resolution_y = 384, 384
    magenta = bpy.data.images.new("magenta", 4, 4)
    magenta.pixels.foreach_set(np.tile(np.array([1.0, 0.0, 1.0, 1.0], np.float32), 16))
    for m in bpy.data.materials:  # uniforme em magenta chapado: a textura do TA_MASK vira uma imagem magenta (so no preview, depois de salvar)
        if m.name.startswith("TA_MASK"):
            for n in m.node_tree.nodes:
                if n.type == "TEX_IMAGE":
                    n.image = magenta
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.type, cam.data.ortho_scale = "ORTHO", 1.2
    cam.location, cam.rotation_euler = (0.0, -10.0, 0.5), (math.pi / 2, 0.0, 0.0)
    sc.collection.objects.link(cam)
    sc.camera = cam
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)


def main():
    a = argparse.ArgumentParser()
    a.add_argument("--src", required=True)
    a.add_argument("--out", required=True)
    a.add_argument("--only")
    a.add_argument("--tex", type=int, default=1024)
    a.add_argument("--preview")
    a.add_argument("--remask", action="store_true", help="refaz so a mascara no .blend ja convertido (sem importar o FBX)")
    a = a.parse_args(sys.argv[sys.argv.index("--") + 1 :])
    os.makedirs(a.out, exist_ok=True)
    for nome in a.only.split(",") if a.only else list(UNIFORME):
        out = os.path.join(a.out, nome + ".blend")
        if a.remask:
            bpy.ops.wm.open_mainfile(filepath=out)
            for o in [o for o in bpy.context.scene.objects if o.type == "MESH"]:
                o.data.polygons.foreach_set("material_index", np.zeros(len(o.data.polygons), np.int32))
                while len(o.data.materials) > 1:
                    o.data.materials.pop()
        else:
            bpy.ops.wm.read_factory_settings(use_empty=True)
            fbx = sorted(glob.glob(os.path.join(a.src, nome + "_hd", "*.fbx")))
            if not fbx:
                die("sem FBX em " + os.path.join(a.src, nome + "_hd"))
            bpy.ops.import_scene.fbx(filepath=fbx[0])
        meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
        if len(meshes) != 1 or not meshes[0].data.materials:
            die("%s: esperava 1 malha com material, achei %d" % (nome, len(meshes)))
        ob = meshes[0]
        node = base_node(ob.data.materials[0])
        if node is None:
            die(nome + ": sem textura base")
        img = node.image
        for im in list(
            bpy.data.images
        ):  # metal, normal, rm, rugosidade: fora, sem carregar os pixels
            if im != img:
                bpy.data.images.remove(im)
        if not a.remask:
            img.scale(a.tex, a.tex)
            img.pack()
        cores, tol, z0, z1 = UNIFORME[nome]
        frac = uniforme(ob, img, cores, tol, z0, z1) if cores else 0.0
        # altura normalizada a 1 (o render_sprites.py --height poe a medida de projeto): o preview enquadra todos iguais
        bpy.context.view_layer.update()
        zs = [(ob.matrix_world @ v.co).z for v in ob.data.vertices]
        bpy.ops.wm.save_as_mainfile(filepath=out, compress=True)
        print(
            "ELENCO %-18s tris=%d tex=%d uniforme=%.1f%% altura=%.3f -> %s"
            % (
                nome,
                sum(len(p.vertices) - 2 for p in ob.data.polygons),
                a.tex,
                frac * 100,
                max(zs) - min(zs),
                out,
            )
        )
        if a.preview:
            os.makedirs(a.preview, exist_ok=True)
            k = 1.0 / max(max(zs) - min(zs), 1e-6)
            for o in bpy.context.scene.objects:
                if o.parent is None:
                    o.matrix_world = Matrix.Scale(k, 4) @ o.matrix_world
            preview(os.path.join(a.preview, nome + ".png"))
    print("ELENCO_OK")


main()
