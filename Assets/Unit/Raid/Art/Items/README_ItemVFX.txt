Raid Item VFX - Shader Optimized

Persistent world item visuals:
- ItemAttackVFX
- ItemAttackSpeedVFX
- ItemHealVFX

Design:
- 1 built-in Quad per item
- 1 MeshRenderer per item
- 1 transparent URP pass
- 0 ParticleSystems
- 0 texture samples / 0 external textures
- all animation is GPU-side via _Time
- no Update/Coroutine/runtime allocations

The supplied PixPlays/CartoonVFX packs were inspected as visual references. Their persistent aura/item prefabs are not copied here because the persistent field can be rendered more cheaply with the custom shader. Selected source VFX can still be copied later for short pickup one-shot effects.
