"""malhas_ta.py - Blender headless: corpos rigidos do Tripo (HD, 50 000 faces) -> malha leve para o Unity (GDD §10.2, D-18).

Para cada peca da tabela PECAS: importa o FBX HD de arte/tripo/<peca>_hd/, junta as malhas, decima ate o teto de triangulos,
normaliza a maior medida horizontal para o tamanho de projeto (1 unidade = 1 m), poe o pivo no chao (centro da base em z = 0),
reduz a textura base para N px e exporta <peca>.fbx (so a malha, sem material) + <peca>.png. O Mundo monta o material em
tempo de execucao (Legacy Diffuse com a textura). Saida fora do git (licenca do Tripo: o modelo nao se redistribui; GDD §10.2).
Tambem renderiza uma folha de conferencia no angulo da camera do jogo (ortografica, 45 graus de cima e de lado).

Uso:
  blender.exe -b --factory-startup --python-exit-code 1 --python malhas_ta.py -- --src <arte/tripo> --out <pasta>
      [--preview conf.png] [--only aviao_regional,loja]
Saida: <peca>.fbx, <peca>.png, malhas.json (triangulos, medidas, SHA-256), "MALHAS_OK" no log.
"""

import argparse, glob, hashlib, json, math, os, sys
import bpy
from mathutils import Matrix, Vector

# peca: (teto de triangulos, px da textura, maior medida horizontal em m; negativa = altura em m)
# ponytail: teto por peca numa tabela so; o K3 (60 pessoas, 2 avioes, 6 veiculos) cabe com folga num Android de entrada
PECAS = {
    "terminal_vale_verde": (6000, 1024, 9.0),
    "torre_controle": (3000, 512, 2.2),
    "hangar_manutencao": (4000, 1024, 7.0),
    "balcao_checkin": (2000, 512, 0.75),
    "raio_x": (2500, 512, 1.15),
    "loja": (2500, 512, 1.4),
    "portao_embarque": (2000, 512, 0.75),
    # Lote 5 (29/10): nivel 2 das estacoes (o predio muda com o upgrade, GDD §5.13)
    "balcao_checkin_n2": (2000, 512, 0.75),
    "raio_x_n2": (2500, 512, 1.15),
    "loja_n2": (2500, 512, 1.4),
    "portao_embarque_n2": (2000, 512, 0.75),
    # Lote 6: nivel 3
    "loja_n3": (2500, 512, 1.4),
    "balcao_checkin_n3": (2000, 512, 0.75),
    "raio_x_n3": (2500, 512, 1.15),
    "portao_embarque_n3": (2000, 512, 0.75),
    "ponte_embarque": (2500, 512, 3.4),
    "cadeiras_espera": (1500, 512, 1.6),
    "vaso_planta": (800, 256, 0.5),
    "aviao_regional": (5000, 1024, 7.6),
    "aviao_narrow": (5000, 1024, 9.0),
    "rebocador": (2000, 512, 1.5),
    "carrinho_bagagem": (2500, 512, 2.6),
    "caminhao_combustivel": (2500, 512, 2.6),
    "escada_embarque": (2000, 512, 2.3),
    "onibus_patio": (2500, 512, 3.2),
    # Lote 4 (29/10): cenario e o 6o veiculo; medida negativa = altura (peca alta e fina)
    "caminhao_catering": (2500, 512, 2.8),
    "conteiner_bagagem": (1200, 256, 1.3),
    "arvore": (1500, 512, -3.2),
    "poste": (800, 256, -4.0),
    "cone": (300, 128, -0.35),
    "biruta": (1000, 256, -2.6),
    "radar": (1500, 256, -2.4),
    # as 4 salas novas (Fase C2), a sala da tripulacao e o que muda de nivel fora das salas
    "esteira_bagagem": (2500, 512, 1.15),
    "esteira_bagagem_n2": (2500, 512, 1.15),
    "esteira_bagagem_n3": (2500, 512, 1.15),
    "alimentacao": (2500, 512, 1.4),
    "alimentacao_n2": (2500, 512, 1.4),
    "alimentacao_n3": (2500, 512, 1.4),
    "balcao_informacoes": (1500, 512, 0.9),
    "oficina_mecanico": (3000, 512, 3.2),
    "sala_tripulacao": (3000, 512, 3.2),
    "hangar_manutencao_n2": (4000, 1024, 7.0),
    "hangar_manutencao_n3": (4000, 1024, 7.0),
    "torre_controle_n2": (3000, 512, 2.2),
    "torre_controle_n3": (3000, 512, 2.2),
    "terminal_vale_verde_n2": (6000, 1024, 9.0),
    "terminal_vale_verde_n3": (6000, 1024, 9.0),
    # Lote 11 e a seguranca com etapas na tela
    "garagem_patio": (5000, 1024, 9.6),
    "cabine_scanner_corpo": (2500, 512, -1.35),
    "portico_detector": (1500, 512, -1.3),
    "bandeja_seguranca": (600, 256, 0.42),
    "scanner_mao": (400, 128, 0.3),
    # mobilia das salas na regua de pessoas (adulto de 0,5 m e 0,2 m de ombros, docs/COMPETITIVO.md §h.8): HD ja gerada, sem credito
    "banco": (800, 256, 0.8),
    "balanca_bagagem": (600, 256, 0.24),
    "bebedouro": (600, 256, -0.42),
    "lixeira_terminal": (500, 256, -0.3),
    "maquina_venda": (1200, 512, -0.68),
    "mesas_alimentacao": (1800, 512, 0.9),
    "prateleiras_loja": (2000, 512, 0.9),
    "vitrine_cafe": (1500, 512, 0.8),
    "vitrine_loja": (1500, 512, 0.8),
    "caixa_registradora": (500, 256, 0.16),
    "esteira_rolante": (1500, 512, 1.4),
    "esteira_carga": (1500, 512, 1.4),
    "totem_autoatendimento": (800, 256, -0.55),
    "placa_direcao": (600, 256, -0.6),
    "sofa_lounge": (1200, 512, 0.8),
    "mesa_lounge": (800, 256, 0.35),
    "bar_lounge": (2000, 512, 1.2),
    "caixa_ferramentas": (600, 256, 0.25),
    "carrinho_cafe": (1200, 512, 0.5),
    "carrinho_limpeza": (1000, 256, 0.3),
    "cavalete_manutencao": (800, 256, 0.5),
    "escada_manutencao": (800, 256, -0.6),
    "gerador_solo": (1200, 256, 0.7),
    "caixotes_carga": (800, 256, 0.5),
    "paletes_carga": (800, 256, 0.6),
    "arbusto": (800, 256, 0.4),
    "canteiro_flores": (800, 256, 0.6),
    "arco_entrada": (1500, 512, 1.0),
    # a sala de espera do portao
    "catraca_embarque": (1200, 512, 0.7),
    "cordao_fila": (800, 256, 1.0),
    # a mala solta (balanca, esteira, bandeja, carrinho)
    "mala_rodinha": (500, 256, -0.42),
    "balanca_bagagem": (1000, 256, 0.6),
}


# a View pinta na cor do voo (_Color x textura): a textura guarda so o sombreado
TINGIDAS = {"mala_rodinha"}


def cinza(img):
    px = list(img.pixels)
    lum = [0.3 * px[i] + 0.59 * px[i + 1] + 0.11 * px[i + 2] for i in range(0, len(px), 4)]
    k = 0.85 / (sum(lum) / len(lum))
    for n, v in enumerate(lum):
        px[n * 4] = px[n * 4 + 1] = px[n * 4 + 2] = min(1.0, v * k)
    img.pixels = px


def die(msg):
    print("ERRO " + msg)
    sys.exit(1)


def sha(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for b in iter(lambda: f.read(1 << 20), b""):
            h.update(b)
    return h.hexdigest()


def tris(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def base_image(ob):
    for slot in ob.material_slots:
        m = slot.material
        if not (m and m.use_nodes):
            continue
        bsdf = next((n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
        if bsdf:
            for link in bsdf.inputs["Base Color"].links:
                if link.from_node.type == "TEX_IMAGE" and link.from_node.image:
                    return link.from_node.image
        img = next(
            (
                n.image
                for n in m.node_tree.nodes
                if n.type == "TEX_IMAGE" and n.image and "base" in n.image.name.lower()
            ),
            None,
        )
        if img:
            return img
    return None


def peca(nome, src, out):
    teto, px, largura = PECAS[nome]
    fbx = sorted(glob.glob(os.path.join(src, nome + "_hd", "*.fbx")))
    if not fbx:
        die("sem FBX em " + os.path.join(src, nome + "_hd"))
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=fbx[0])
    novos = [o for o in bpy.data.objects if o not in before]
    malhas = [o for o in novos if o.type == "MESH"]
    if not malhas:
        die(nome + ": FBX sem malha")
    for o in novos:  # tira os pais (vazios do FBX) mantendo a pose do mundo
        mw = o.matrix_world.copy()
        o.parent = None
        o.matrix_world = mw
    bpy.ops.object.select_all(action="DESELECT")
    for o in malhas:
        o.select_set(True)
    bpy.context.view_layer.objects.active = malhas[0]
    if len(malhas) > 1:
        bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for o in novos:
        if o != ob and o.name in bpy.data.objects:
            bpy.data.objects.remove(o, do_unlink=True)
    t0 = tris(ob)
    if t0 > teto:
        mod = ob.modifiers.new("dec", "DECIMATE")
        mod.ratio = teto / float(t0)
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    # medida de projeto e pivo no chao: centro da base em (0, 0, 0)
    vs = [v.co for v in ob.data.vertices]
    lo = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
    hi = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
    k = (
        largura / max(hi.x - lo.x, hi.y - lo.y)
        if largura > 0
        else -largura / (hi.z - lo.z)
    )
    c = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
    ob.data.transform(Matrix.Scale(k, 4) @ Matrix.Translation(-c))
    ob.data.update()
    dims = [round((hi[i] - lo[i]) * k, 3) for i in range(3)]
    img = base_image(ob)
    png = os.path.join(out, nome + ".png")
    if img is None:
        die(nome + ": sem textura base")
    img.scale(px, px)
    if nome in TINGIDAS:
        cinza(img)
    img.filepath_raw, img.file_format = png, "PNG"
    img.save()
    ob.name = nome
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    path = os.path.join(out, nome + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"MESH"},
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="STRIP",
    )
    t1 = tris(ob)
    print("PECA %-22s %6d -> %5d tri  %s m  tex %d px" % (nome, t0, t1, dims, px))
    return ob, {
        "tris_hd": t0,
        "tris": t1,
        "dims_m": dims,
        "tex_px": px,
        "fonte_fbx_sha256": sha(fbx[0]),
        "fbx_sha256": sha(path),
        "png_sha256": sha(png),
    }


def preview(obs, path):
    """Folha de conferencia: as pecas numa grade, camera do jogo (ortografica, 45 graus de cima, azimute 45), Workbench com textura."""
    sc = bpy.context.scene
    n = len(obs)
    cols = math.ceil(math.sqrt(n))
    passo = 10.0
    for i, ob in enumerate(obs):
        ob.location = Vector(((i % cols) * passo, -(i // cols) * passo, 0.0))
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light, sc.display.shading.color_type = "STUDIO", "TEXTURE"
    sc.display.shading.show_cavity = True
    sc.render.resolution_x, sc.render.resolution_y = 1600, 1600
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.type, cam.data.ortho_scale = "ORTHO", cols * passo * 1.5
    centro = Vector(((cols - 1) * passo / 2, -((n - 1) // cols) * passo / 2, 0.0))
    # azimute 45 (de frente para +x +y do Unity = -y do Blender, como a camera do jogo) e 45 graus de cima
    d = Vector(
        (-math.sin(math.radians(45)), -math.cos(math.radians(45)), 1.0)
    ).normalized()
    cam.location = centro + d * 80.0
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    sc.collection.objects.link(cam)
    sc.camera = cam
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)


def main():
    a = argparse.ArgumentParser()
    a.add_argument("--src", required=True)
    a.add_argument("--out", required=True)
    a.add_argument("--preview")
    a.add_argument("--only")
    a = a.parse_args(sys.argv[sys.argv.index("--") + 1 :])
    os.makedirs(a.out, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    nomes = a.only.split(",") if a.only else list(PECAS)
    registro, obs = {}, []
    jpath = os.path.join(a.out, "malhas.json")
    if os.path.exists(jpath):
        with open(jpath) as f:
            registro = json.load(f)
    for nome in nomes:
        ob, info = peca(nome, a.src, a.out)
        registro[nome] = info
        obs.append(ob)
        bpy.ops.outliner.orphans_purge(
            do_recursive=True
        )  # a HD (4K, 50 000 faces) sai da memoria antes da proxima
    with open(jpath, "w") as f:
        json.dump(registro, f, indent=1, sort_keys=True)
    if a.preview:
        preview(obs, os.path.abspath(a.preview))
    print("MALHAS_OK %d pecas em %s" % (len(nomes), a.out))


main()
