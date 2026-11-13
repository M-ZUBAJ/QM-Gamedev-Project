LOW POLY TOOL PACK — 25 STYLIZED TOOLS
Abduo Bricks
Support: realnotfake700@gmail.com

CONTENTS
--------
25 prefabs: 5 tools (Sword, Axe, Pickaxe, Hoe, Shovel), each in 5 material
tiers (Wood, Stone, Iron, Gold, Diamond).

  Prefabs/  Sword_Wood ... Shovel_Diamond   (25 ready-to-drop prefabs)
  Models/   sword_wood.fbx ... shovel_diamond.fbx  (source meshes)
  Materials/ 6 URP Lit materials (M_Wood, M_Stone, M_Iron, M_Gold,
             M_Diamond, M_Handle_Wood)
  Scenes/   LowPolyToolPack_Demo.unity  (all 25 tools laid out)

STRUCTURE
---------
Every tool is a two-object hierarchy:
  Handle   (root; pivot at the very bottom of the grip)
  ToolHead (child)
Swap the material on ToolHead — or the whole ToolHead mesh — to change tiers
at runtime. The pivot at the grip base makes hand/socket attachment trivial:
parent the prefab to your hand bone and zero its local position.

SPECS
-----
- 40–64 triangles per complete tool (mobile-safe)
- UV-unwrapped, no textures needed (value-based materials keep builds tiny)
- FBX exported Y-up at scale 1 — imports with no rotation/scale fixes
- No rigs, no animations, no runtime scripts

RENDER PIPELINES
----------------
Materials use URP Lit (project default). For Built-in RP recreate them with
the Standard shader using the same colors; for HDRP use Unity's material
converter (Window > Rendering > HDRP Wizard > Convert materials).

QUESTIONS / SUPPORT
-------------------
realnotfake700@gmail.com — we usually reply within 1–2 business days.
