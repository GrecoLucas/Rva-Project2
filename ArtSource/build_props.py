"""
Arcane Restoration - procedural low-poly props.

Builds every custom prop with bmesh, colours faces through a shared palette
texture (one material for everything = good batching on the Quest), exports one
FBX per prop into the Unity project and saves the .blend next to this script.

Run (Blender 4.2+):
    blender --background --factory-startup --python ArtSource/build_props.py

Conventions: Blender Z-up, 1 unit = 1 m, the "front" of a prop faces +Y.
"""
import math
import os
import random

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(HERE)
MODELS_DIR = os.path.join(PROJECT, "Assets", "_Project", "Art", "Models")
TEXTURES_DIR = os.path.join(PROJECT, "Assets", "_Project", "Art", "Textures")
BLEND_PATH = os.path.join(HERE, "ArcaneProps.blend")

random.seed(7)

# --------------------------------------------------------------------------- palette
# sRGB colours. Order matters: index -> cell in the palette texture.
PALETTE = [
    ("white", (0.95, 0.95, 0.93)),
    ("light_grey", (0.78, 0.78, 0.78)),
    ("grey", (0.55, 0.56, 0.58)),
    ("dark_grey", (0.30, 0.31, 0.33)),
    ("black", (0.09, 0.09, 0.10)),
    ("stone", (0.84, 0.80, 0.72)),
    ("stone_dark", (0.64, 0.60, 0.54)),
    ("metal", (0.62, 0.65, 0.70)),
    ("wood", (0.66, 0.45, 0.26)),
    ("wood_dark", (0.44, 0.29, 0.17)),
    ("cardboard", (0.74, 0.56, 0.35)),
    ("cream", (0.96, 0.91, 0.76)),
    ("eco_yellow", (0.99, 0.80, 0.10)),
    ("eco_yellow_dark", (0.85, 0.64, 0.05)),
    ("eco_blue", (0.13, 0.42, 0.86)),
    ("eco_blue_dark", (0.08, 0.30, 0.66)),
    ("eco_green", (0.16, 0.62, 0.27)),
    ("eco_green_dark", (0.10, 0.46, 0.19)),
    ("eco_brown", (0.50, 0.32, 0.17)),
    ("eco_brown_dark", (0.36, 0.22, 0.11)),
    ("eco_grey", (0.42, 0.44, 0.47)),
    ("eco_grey_dark", (0.28, 0.30, 0.33)),
    ("red", (0.86, 0.16, 0.16)),
    ("orange", (0.96, 0.56, 0.16)),
    ("banana", (0.99, 0.86, 0.26)),
    ("banana_tip", (0.36, 0.25, 0.10)),
    ("leaf_green", (0.32, 0.66, 0.26)),
    ("glass_green", (0.27, 0.66, 0.37)),
    ("glass_clear", (0.76, 0.89, 0.92)),
    ("plastic_blue", (0.56, 0.80, 0.96)),
    ("water_murky", (0.33, 0.36, 0.24)),
    ("water_clean", (0.24, 0.60, 0.78)),
    ("awning_red", (0.82, 0.22, 0.20)),
    ("sign_blue", (0.10, 0.32, 0.72)),
    ("play_red", (0.90, 0.26, 0.22)),
    ("play_yellow", (0.99, 0.78, 0.18)),
    ("play_blue", (0.20, 0.52, 0.90)),
    ("crystal", (0.62, 0.86, 1.00)),
]
INDEX = {name: i for i, (name, _) in enumerate(PALETTE)}
CELLS = 8
CELL_PX = 4
TEX_SIZE = CELLS * CELL_PX


def palette_uv(name):
    i = INDEX[name]
    cx, cy = i % CELLS, i // CELLS
    return ((cx + 0.5) / CELLS, (cy + 0.5) / CELLS)


def build_palette_image():
    img = bpy.data.images.new("ArcanePalette", TEX_SIZE, TEX_SIZE, alpha=False)
    px = [0.0] * (TEX_SIZE * TEX_SIZE * 4)
    for i, (_, rgb) in enumerate(PALETTE):
        cx, cy = i % CELLS, i // CELLS
        for y in range(cy * CELL_PX, (cy + 1) * CELL_PX):
            for x in range(cx * CELL_PX, (cx + 1) * CELL_PX):
                o = (y * TEX_SIZE + x) * 4
                px[o:o + 4] = [rgb[0], rgb[1], rgb[2], 1.0]
    img.pixels = px
    os.makedirs(TEXTURES_DIR, exist_ok=True)
    img.filepath_raw = os.path.join(TEXTURES_DIR, "ArcanePalette.png")
    img.file_format = "PNG"
    img.save()
    return img


def build_palette_material(img):
    mat = bpy.data.materials.new("ArcanePalette")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.interpolation = "Closest"
    mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.85
    return mat


# --------------------------------------------------------------------------- mesh builder
class MeshBuilder:
    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.colours = []

    def _assign(self, verts, colour):
        if colour not in INDEX:
            raise KeyError(colour)
        if colour not in self.colours:
            self.colours.append(colour)
        idx = self.colours.index(colour)
        faces = {f for v in verts for f in v.link_faces}
        for f in faces:
            f.material_index = idx
        return verts

    @staticmethod
    def _matrix(center, rot=(0, 0, 0), scale=(1, 1, 1)):
        return (Matrix.Translation(Vector(center))
                @ Euler(rot).to_matrix().to_4x4()
                @ Matrix.Diagonal((scale[0], scale[1], scale[2], 1.0)))

    def box(self, center, size, colour, rot=(0, 0, 0)):
        r = bmesh.ops.create_cube(self.bm, size=1.0, matrix=self._matrix(center, rot, size))
        return self._assign(r["verts"], colour)

    def cyl(self, center, r1, r2, depth, colour, seg=12, rot=(0, 0, 0)):
        r = bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=seg,
                                  radius1=r1, radius2=r2, depth=depth,
                                  matrix=self._matrix(center, rot))
        return self._assign(r["verts"], colour)

    def sphere(self, center, radius, colour, subdiv=1, scale=(1, 1, 1), jitter=0.0):
        r = bmesh.ops.create_icosphere(self.bm, subdivisions=subdiv, radius=radius,
                                       matrix=self._matrix(center, (0, 0, 0), scale))
        if jitter:
            for v in r["verts"]:
                v.co += Vector((random.uniform(-jitter, jitter),
                                random.uniform(-jitter, jitter),
                                random.uniform(-jitter, jitter)))
        return self._assign(r["verts"], colour)

    def tube(self, z, r_in, r_out, height, colour, seg=24):
        """Ring with walls and top/bottom (a basin wall)."""
        rings = []
        for (rad, zz) in ((r_out, z), (r_out, z + height), (r_in, z + height), (r_in, z)):
            ring = []
            for i in range(seg):
                a = 2 * math.pi * i / seg
                ring.append(self.bm.verts.new((math.cos(a) * rad, math.sin(a) * rad, zz)))
            rings.append(ring)
        new_verts = [v for ring in rings for v in ring]
        for k in range(4):
            ra, rb = rings[k], rings[(k + 1) % 4]
            for i in range(seg):
                j = (i + 1) % seg
                self.bm.faces.new((ra[i], ra[j], rb[j], rb[i]))
        return self._assign(new_verts, colour)

    def build(self, palette_mat, origin="base"):
        bm = self.bm
        bm.normal_update()
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        uv = bm.loops.layers.uv.new("UVMap")
        for f in bm.faces:
            u, v = palette_uv(self.colours[f.material_index])
            for loop in f.loops:
                loop[uv].uv = (u, v)
            f.material_index = 0
            f.smooth = False
        # origin: base (bottom centre) or centre of the bounding box
        xs = [v.co.x for v in bm.verts]
        ys = [v.co.y for v in bm.verts]
        zs = [v.co.z for v in bm.verts]
        cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
        cz = min(zs) if origin == "base" else (min(zs) + max(zs)) / 2
        if origin != "keep":
            bmesh.ops.translate(bm, verts=bm.verts, vec=(-cx, -cy, -cz))
        mesh = bpy.data.meshes.new(self.name)
        bm.to_mesh(mesh)
        bm.free()
        mesh.materials.append(palette_mat)
        obj = bpy.data.objects.new(self.name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        return obj


# --------------------------------------------------------------------------- props
def ecoponto(colour):
    """Portuguese ecoponto container. Intake hole on the front (+Y)."""
    b = MeshBuilder(f"Ecoponto_{colour.split('_')[1].capitalize()}")
    dark = colour + "_dark"
    b.box((0, 0, 0.05), (1.32, 1.32, 0.10), "dark_grey")
    b.cyl((0, 0, 0.85), 0.86, 0.79, 1.50, colour, seg=4, rot=(0, 0, math.radians(45)))
    b.cyl((0, 0, 1.70), 0.80, 0.58, 0.20, dark, seg=4, rot=(0, 0, math.radians(45)))
    # intake hole with a dark rim, upper front
    b.cyl((0, 0.565, 1.22), 0.22, 0.22, 0.03, "dark_grey", seg=12, rot=(math.radians(90), 0, 0))
    b.cyl((0, 0.585, 1.22), 0.17, 0.17, 0.03, "black", seg=12, rot=(math.radians(90), 0, 0))
    # white label plate with a coloured stripe
    b.box((0, 0.585, 0.72), (0.56, 0.02, 0.30), "white")
    b.box((0, 0.597, 0.80), (0.46, 0.01, 0.05), dark)
    return b


def trash_items():
    items = []

    b = MeshBuilder("Trash_PlasticBottle")
    b.cyl((0, 0, 0.11), 0.05, 0.05, 0.22, "plastic_blue", seg=8)
    b.cyl((0, 0, 0.12), 0.052, 0.052, 0.08, "white", seg=8)
    b.cyl((0, 0, 0.25), 0.05, 0.022, 0.06, "plastic_blue", seg=8)
    b.cyl((0, 0, 0.295), 0.025, 0.025, 0.03, "eco_blue", seg=8)
    items.append(b)

    b = MeshBuilder("Trash_Can")
    b.cyl((0, 0, 0.085), 0.045, 0.045, 0.17, "red", seg=10)
    b.cyl((0, 0, 0.09), 0.046, 0.046, 0.05, "white", seg=10)
    b.cyl((0, 0, 0.172), 0.042, 0.042, 0.012, "metal", seg=10)
    b.cyl((0, 0, -0.002), 0.042, 0.042, 0.012, "metal", seg=10)
    items.append(b)

    b = MeshBuilder("Trash_PlasticBag")
    b.sphere((0, 0, 0), 0.16, "white", subdiv=1, scale=(1, 0.8, 0.55), jitter=0.035)
    b.box((0.05, 0, 0.09), (0.03, 0.10, 0.06), "white", rot=(0, 0.4, 0))
    items.append(b)

    b = MeshBuilder("Trash_Newspaper")
    b.box((0, 0, 0), (0.36, 0.26, 0.025), "light_grey")
    b.box((0.01, 0.01, 0.02), (0.34, 0.25, 0.015), "white", rot=(0, 0, math.radians(7)))
    for k, y in enumerate((0.07, 0.02, -0.03, -0.08)):
        b.box((-0.02, y, 0.0285), (0.26 if k else 0.30, 0.02, 0.002), "dark_grey", rot=(0, 0, math.radians(7)))
    items.append(b)

    b = MeshBuilder("Trash_CardboardBox")
    b.box((0, 0, 0), (0.46, 0.36, 0.30), "cardboard")
    b.box((0, 0, 0.152), (0.47, 0.08, 0.006), "cream")
    b.box((0, 0.26, 0.20), (0.44, 0.17, 0.012), "cardboard", rot=(math.radians(-35), 0, 0))
    items.append(b)

    b = MeshBuilder("Trash_GlassBottle")
    b.cyl((0, 0, 0.12), 0.045, 0.045, 0.24, "glass_green", seg=8)
    b.cyl((0, 0, 0.10), 0.047, 0.047, 0.08, "cream", seg=8)
    b.cyl((0, 0, 0.28), 0.045, 0.016, 0.08, "glass_green", seg=8)
    b.cyl((0, 0, 0.355), 0.016, 0.016, 0.07, "glass_green", seg=8)
    items.append(b)

    b = MeshBuilder("Trash_GlassJar")
    b.cyl((0, 0, 0.065), 0.06, 0.06, 0.13, "glass_clear", seg=10)
    b.cyl((0, 0, 0.07), 0.062, 0.062, 0.06, "cream", seg=10)
    b.cyl((0, 0, 0.14), 0.063, 0.063, 0.025, "red", seg=10)
    items.append(b)

    b = MeshBuilder("Trash_BananaPeel")
    b.sphere((0, 0, 0.03), 0.035, "banana", subdiv=1)
    for k in range(3):
        a = k * 2 * math.pi / 3
        c = (math.cos(a) * 0.085, math.sin(a) * 0.085, 0.012)
        b.box(c, (0.17, 0.045, 0.014), "banana", rot=(0, math.radians(18), a))
        tip = (math.cos(a) * 0.17, math.sin(a) * 0.17, -0.012)
        b.box(tip, (0.02, 0.04, 0.012), "banana_tip", rot=(0, math.radians(18), a))
    b.box((0, 0, 0.075), (0.02, 0.02, 0.05), "banana_tip")
    items.append(b)

    b = MeshBuilder("Trash_AppleCore")
    b.cyl((0, 0, 0.02), 0.045, 0.022, 0.04, "red", seg=8)
    b.cyl((0, 0, 0.065), 0.022, 0.022, 0.05, "cream", seg=8)
    b.cyl((0, 0, 0.11), 0.022, 0.045, 0.04, "red", seg=8)
    b.box((0, 0, 0.145), (0.008, 0.008, 0.035), "wood_dark")
    items.append(b)

    b = MeshBuilder("Trash_CigaretteButt")
    b.cyl((0, 0, 0), 0.013, 0.013, 0.06, "orange", seg=6, rot=(0, math.radians(90), 0))
    b.cyl((0.045, 0, 0), 0.013, 0.013, 0.03, "white", seg=6, rot=(0, math.radians(90), 0))
    b.cyl((0.0615, 0, 0), 0.012, 0.012, 0.004, "dark_grey", seg=6, rot=(0, math.radians(90), 0))
    items.append(b)

    b = MeshBuilder("Trash_Napkin")
    b.sphere((0, 0, 0), 0.08, "white", subdiv=1, scale=(1, 0.9, 0.5), jitter=0.02)
    items.append(b)

    b = MeshBuilder("Trash_GarbageBag")
    b.sphere((0, 0, 0), 0.30, "black", subdiv=2, scale=(1, 0.95, 1.15), jitter=0.035)
    b.cyl((0, 0, 0.36), 0.03, 0.09, 0.12, "black", seg=6)
    items.append(b)

    return items


def fountain():
    stone = MeshBuilder("Fountain")
    stone.tube(0.0, 2.0, 2.3, 0.55, "stone", seg=24)
    stone.tube(0.55, 1.95, 2.42, 0.08, "stone_dark", seg=24)
    stone.cyl((0, 0, 0.05), 2.0, 2.0, 0.10, "stone_dark", seg=24)
    stone.cyl((0, 0, 0.70), 0.30, 0.26, 1.40, "stone", seg=12)
    stone.cyl((0, 0, 1.45), 0.25, 0.90, 0.30, "stone", seg=16)
    stone.tube(1.55, 0.80, 0.95, 0.10, "stone_dark", seg=16)
    stone.cyl((0, 0, 1.80), 0.10, 0.08, 0.40, "stone_dark", seg=8)
    stone.sphere((0, 0, 2.05), 0.14, "stone", subdiv=1)
    water = MeshBuilder("Fountain_Water")
    water.cyl((0, 0, 0.42), 1.99, 1.99, 0.02, "water_murky", seg=24)
    water.cyl((0, 0, 1.58), 0.80, 0.80, 0.02, "water_murky", seg=16)
    return stone, water


def canal_segment():
    """8 x 8 m tile: walkways on both sides, railings, a 5 m wide channel."""
    c = MeshBuilder("Canal_Segment")
    for s in (-1, 1):
        c.box((0, s * 3.4, -0.02), (8.0, 1.2, 0.36), "stone")
        c.box((0, s * 2.65, 0.10), (8.0, 0.30, 0.12), "stone_dark")
        c.box((0, s * 2.65, -0.62), (8.0, 0.30, 1.32), "stone_dark")
        for x in (-3.0, -1.0, 1.0, 3.0):
            c.cyl((x, s * 2.95, 0.66), 0.03, 0.03, 1.0, "metal", seg=6)
        c.box((0, s * 2.95, 1.16), (8.0, 0.05, 0.05), "metal")
        c.box((0, s * 2.95, 0.66), (8.0, 0.03, 0.03), "metal")
    c.box((0, 0, -1.30), (8.0, 5.0, 0.20), "dark_grey")
    water = MeshBuilder("Canal_Water")
    water.box((0, 0, -0.55), (8.0, 5.0, 0.02), "water_murky")
    return c, water


def market_stall():
    b = MeshBuilder("Market_Stall")
    b.box((0, 0.5, 0.475), (3.0, 0.8, 0.95), "wood")
    b.box((0, 0.5, 0.98), (3.1, 0.9, 0.06), "wood_dark")
    for x in (-1.45, 1.45):
        for y in (-0.85, 0.85):
            b.cyl((x, y, 1.25), 0.05, 0.05, 2.5, "wood_dark", seg=6)
    for k in range(6):
        x = -1.25 + k * 0.5
        b.box((x, 0.1, 2.55), (0.5, 2.15, 0.05), "awning_red" if k % 2 == 0 else "white",
              rot=(math.radians(-12), 0, 0))
    b.box((0, 1.12, 2.86), (1.6, 0.05, 0.34), "cream")
    produce = ("red", "orange", "leaf_green")
    for k, x in enumerate((-1.0, 0.0, 1.0)):
        b.box((x, 0.6, 1.11), (0.55, 0.42, 0.2), "wood")
        for n in range(5):
            b.sphere((x + random.uniform(-0.18, 0.18), 0.6 + random.uniform(-0.12, 0.12), 1.24),
                     0.07, produce[k], subdiv=1)
    return b


def bus_shelter():
    b = MeshBuilder("Bus_Shelter")
    b.box((0, -0.75, 1.15), (4.0, 0.05, 2.0), "glass_clear")
    b.box((2.0, 0, 1.15), (0.05, 1.45, 2.0), "glass_clear")
    for x in (-2.0, 2.0):
        for y in (-0.75, 0.75):
            b.box((x, y, 1.25), (0.08, 0.08, 2.5), "dark_grey")
    b.box((0, 0, 2.56), (4.4, 1.9, 0.12), "dark_grey")
    b.box((0, -0.45, 0.48), (2.4, 0.42, 0.06), "wood")
    for x in (-1.0, 1.0):
        b.box((x, -0.45, 0.23), (0.06, 0.36, 0.46), "metal")
    b.cyl((-2.6, 0.6, 1.4), 0.04, 0.04, 2.8, "metal", seg=8)
    b.cyl((-2.6, 0.62, 2.7), 0.26, 0.26, 0.04, "sign_blue", seg=12, rot=(math.radians(90), 0, 0))
    b.box((-2.6, 0.645, 2.7), (0.30, 0.01, 0.07), "white")
    return b


def bench():
    b = MeshBuilder("Bench")
    for y in (-0.14, 0.0, 0.14):
        b.box((0, y, 0.45), (1.8, 0.12, 0.04), "wood")
    for z in (0.66, 0.82):
        b.box((0, -0.24, z), (1.8, 0.10, 0.04), "wood", rot=(math.radians(-12), 0, 0))
    for x in (-0.75, 0.75):
        b.box((x, 0, 0.22), (0.06, 0.45, 0.44), "dark_grey")
        b.box((x, -0.22, 0.62), (0.06, 0.05, 0.45), "dark_grey", rot=(math.radians(-12), 0, 0))
    return b


def slide():
    b = MeshBuilder("Playground_Slide")
    b.box((0, -0.6, 1.5), (0.9, 0.9, 0.08), "play_yellow")
    for x in (-0.42, 0.42):
        for y in (-1.02, -0.18):
            b.cyl((x, y, 0.75), 0.04, 0.04, 1.5, "metal", seg=6)
        b.box((x, -1.05, 1.9), (0.05, 0.05, 0.8), "metal")
    b.box((0, 0.6, 0.78), (0.6, 2.2, 0.05), "play_red", rot=(math.radians(-38), 0, 0))
    for x in (-0.3, 0.3):
        b.box((x, 0.6, 0.86), (0.04, 2.2, 0.16), "play_red", rot=(math.radians(-38), 0, 0))
    for x in (-0.35, 0.35):
        b.box((x, -1.25, 0.75), (0.05, 0.05, 1.55), "play_blue", rot=(math.radians(15), 0, 0))
    for z in (0.35, 0.7, 1.05, 1.4):
        b.box((0, -1.25 + (0.75 - z) * math.tan(math.radians(15)) * -1, z), (0.7, 0.05, 0.04), "play_blue")
    return b


def swing():
    b = MeshBuilder("Playground_Swing")
    for x in (-1.5, 1.5):
        for s in (-1, 1):
            b.cyl((x, s * 0.45, 1.2), 0.05, 0.05, 2.5, "play_blue", seg=6,
                  rot=(s * math.radians(-20), 0, 0))
    b.cyl((0, 0, 2.38), 0.06, 0.06, 3.2, "play_blue", seg=8, rot=(0, math.radians(90), 0))
    for x in (-0.7, 0.7):
        for dx in (-0.2, 0.2):
            b.box((x + dx, 0, 1.45), (0.02, 0.02, 1.85), "metal")
        b.box((x, 0, 0.52), (0.5, 0.22, 0.04), "play_red")
    return b


def trash_pile():
    b = MeshBuilder("Trash_Pile")
    for (x, y, z, r) in ((0, 0, 0.3, 0.32), (0.45, 0.2, 0.25, 0.28), (-0.4, 0.25, 0.25, 0.27),
                          (0.1, -0.4, 0.25, 0.26), (0.05, 0.1, 0.62, 0.24)):
        b.sphere((x, y, z), r, "black", subdiv=2, scale=(1, 0.95, 1.1), jitter=0.03)
    b.box((-0.55, -0.35, 0.18), (0.46, 0.36, 0.30), "cardboard", rot=(0, 0, 0.5))
    b.box((0.6, -0.3, 0.12), (0.4, 0.3, 0.22), "cardboard", rot=(0.2, 0, -0.3))
    return b


def wand():
    """Wand pointing along -Y (Unity +Z). Origin at the butt of the handle.
    The crystal tip is a separate object so it can get its own glowing material."""
    along = (math.radians(90), 0, 0)  # cylinder axis Z -> Y
    b = MeshBuilder("Wand")
    b.cyl((0, -0.005, 0), 0.016, 0.016, 0.012, "metal", seg=8, rot=along)
    b.cyl((0, -0.10, 0), 0.013, 0.015, 0.18, "wood_dark", seg=8, rot=along)
    b.cyl((0, -0.195, 0), 0.016, 0.016, 0.014, "metal", seg=8, rot=along)
    b.cyl((0, -0.255, 0), 0.006, 0.010, 0.11, "wood", seg=8, rot=along)
    b.cyl((0, -0.31, 0), 0.011, 0.011, 0.008, "metal", seg=8, rot=along)
    tip = MeshBuilder("Wand_Tip")
    tip.sphere((0, -0.33, 0), 0.016, "crystal", subdiv=1, scale=(1, 1.5, 1))
    return b, tip


def finale_plinth():
    b = MeshBuilder("Finale_Plinth")
    b.cyl((0, 0, 0.10), 1.8, 1.8, 0.20, "stone_dark", seg=8)
    b.cyl((0, 0, 0.30), 1.4, 1.4, 0.20, "stone", seg=8)
    b.cyl((0, 0, 1.00), 0.60, 0.45, 1.20, "stone", seg=8)
    b.cyl((0, 0, 1.64), 0.55, 0.55, 0.08, "stone_dark", seg=8)
    b.sphere((0, 0, 2.05), 0.42, "crystal", subdiv=1, scale=(1, 1, 1.35))
    return b


# --------------------------------------------------------------------------- export
def export_fbx(objects, filename):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(MODELS_DIR, filename),
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="STRIP",
    )


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    os.makedirs(MODELS_DIR, exist_ok=True)
    img = build_palette_image()
    mat = build_palette_material(img)

    exported = []  # (objects, folder/filename)
    for colour in ("eco_yellow", "eco_blue", "eco_green", "eco_brown", "eco_grey"):
        obj = ecoponto(colour).build(mat, "base")
        exported.append(([obj], f"Ecoponto/{obj.name}.fbx"))
    for b in trash_items():
        obj = b.build(mat, "center")
        exported.append(([obj], f"Waste/{obj.name}.fbx"))

    f_stone, f_water = fountain()
    exported.append(([f_stone.build(mat, "keep"), f_water.build(mat, "keep")], "Environment/Fountain.fbx"))
    c_stone, c_water = canal_segment()
    exported.append(([c_stone.build(mat, "keep"), c_water.build(mat, "keep")], "Environment/Canal_Segment.fbx"))
    w_body, w_tip = wand()
    exported.append(([w_body.build(mat, "keep"), w_tip.build(mat, "keep")], "Magic/Wand.fbx"))
    for fn in (market_stall, bus_shelter, bench, slide, swing, trash_pile, finale_plinth):
        obj = fn().build(mat, "base")
        exported.append(([obj], f"Environment/{obj.name}.fbx"))

    for objs, rel in exported:
        os.makedirs(os.path.dirname(os.path.join(MODELS_DIR, rel)), exist_ok=True)
        export_fbx(objs, rel)
        print("EXPORTED", rel, sum(len(o.data.polygons) for o in objs), "faces")

    # Lay the props out in a grid for the saved .blend (after export, so FBX pivots stay at 0)
    x = 0.0
    for objs, _ in exported:
        for o in objs:
            o.location.x = x
        x += 3.0
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    print("SAVED", BLEND_PATH)


main()
