using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.EffectModules
{
    public class SeasonShaderController : MonoBehaviour
    {
        [Header("Scene Renderers (optional but recommended)")]
        [SerializeField] private Transform targetsRoot;
        
        [Header("Runtime Safety")]
        [Tooltip("Clone materials at runtime so we never modify material assets.")]
        [SerializeField] private bool runtimeCloneMaterials = true;

        [Header("Debug")]
        [SerializeField] private bool showDebug;

        [SerializeField] private TerrainTreeMaterialDriver prototypeCloner;
        
        // Property IDs
        private static readonly int GroundFadeHeight = Shader.PropertyToID("_GroundFadeHeight");
        private static readonly int GroundFadeContrast = Shader.PropertyToID("_GroundFadeContrast");
        private static readonly int BaseTexColorTint = Shader.PropertyToID("_BaseTexColorTint");
        private static readonly int BlendTextureHeight = Shader.PropertyToID("_BlendTextureHeight");
        private static readonly int BlendTextureContrast = Shader.PropertyToID("_BlendTextureContrast");
        private static readonly int BlendTextureOpacity = Shader.PropertyToID("_BlendTextureOpacity");
        private static readonly int SnowLevel = Shader.PropertyToID("_SnowLevel");

        // Your spring target tint
        private static readonly Color SpringGreen = new Color(0.17f, 0.6f, 0f, 1f);

        private readonly List<Renderer> _sceneRenderers = new();
        private readonly List<Renderer> _prototypeRenderers = new();

        // Map original material asset -> runtime clone
        private readonly Dictionary<Material, Material> _matCloneMap = new();
        private bool _prepared;

        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            _mpb ??= new MaterialPropertyBlock();

        }

        private void Update()
        {
            if (!Application.isPlaying) return;

        }

        [ContextMenu("Collect Renderers")]
        public void CollectRenderers()
        {
            _sceneRenderers.Clear();
            _prototypeRenderers.Clear();

            if (targetsRoot != null)
                _sceneRenderers.AddRange(targetsRoot.GetComponentsInChildren<Renderer>(true));

            if (prototypeCloner != null)
            {
                var runtimePrefabs = prototypeCloner.GetRuntimePrototypeInstances();
                for (int i = 0; i < runtimePrefabs.Count; i++)
                {
                    var go = runtimePrefabs[i];
                    if (go == null) continue;
                    _prototypeRenderers.AddRange(go.GetComponentsInChildren<Renderer>(true));
                }
            }
            
            if (showDebug)
            {
                Debug.Log($"[SeasonShaderController] sceneRenderers={_sceneRenderers.Count}, prototypeRenderers={_prototypeRenderers.Count}, runtimePrefabs={(prototypeCloner!=null ? prototypeCloner.GetRuntimePrototypeInstances().Count : -1)}");
            }


        }

        private void PrepareRuntimeMaterialsIfNeeded()
        {
            if (_prepared) return;
            if (!Application.isPlaying) return;

            if (!runtimeCloneMaterials)
            {
                _prepared = true;
                return;
            }

            // 1) Clone materials used by scene renderers + prototype renderers
            CloneAndReplaceMaterials(_sceneRenderers);
            CloneAndReplaceMaterials(_prototypeRenderers);

            _prepared = true;

            if (showDebug) Debug.Log($"[SeasonShaderController] Runtime material clones prepared: {_matCloneMap.Count}");
        }

        private void CloneAndReplaceMaterials(List<Renderer> renderers)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (r == null) continue;

                var mats = r.sharedMaterials;
                if (mats == null || mats.Length == 0) continue;

                bool changed = false;

                for (int m = 0; m < mats.Length; m++)
                {
                    var src = mats[m];
                    if (src == null) continue;

                    // Only bother cloning materials that have any of the properties we want to drive
                    if (!MaterialIsRelevant(src)) continue;

                    if (!_matCloneMap.TryGetValue(src, out var clone) || clone == null)
                    {
                        clone = Instantiate(src);
                        clone.name = src.name + "_RUNTIME_CLONE";
                        _matCloneMap[src] = clone;
                    }

                    mats[m] = clone;
                    changed = true;
                }

                if (changed)
                    r.sharedMaterials = mats;
            }
        }

        private static bool MaterialIsRelevant(Material m)
        {
            if (m == null) return false;

            return m.HasProperty(GroundFadeHeight)
                   || m.HasProperty(GroundFadeContrast)
                   || m.HasProperty(BaseTexColorTint)
                   || m.HasProperty(BlendTextureHeight)
                   || m.HasProperty(BlendTextureContrast)
                   || m.HasProperty(BlendTextureOpacity)
                   || m.HasProperty(SnowLevel);
        }

        /// <summary>
        /// 0 = winter, 1 = spring
        /// </summary>
        public void SetSeasonProgress(float t)
        {
            t = Mathf.Clamp01(t);

            // 如果还没准备好，尝试准备
            if (!_prepared)
            {
                TryPrepare();
            }

            if (!_prepared)
                return; // 还没 ready，就先不改（下一帧再试）

            ApplyToRenderersWithMPB(_sceneRenderers, t);
            ApplyToClonedSharedMaterials(t);
        }
        
        private void TryPrepare()
        {
            if (prototypeCloner == null)
                prototypeCloner = FindObjectOfType<TerrainTreeMaterialDriver>();

            if (prototypeCloner == null)
                return;

            // runtime tree 还没生成，不要准备
            if (prototypeCloner.GetRuntimePrototypeInstances().Count == 0)
                return;

            CollectRenderers();
            PrepareRuntimeMaterialsIfNeeded();

            if (showDebug)
                Debug.Log("[SeasonShaderController] Prepared successfully.");

            _prepared = true;
        }


        private void ApplyToRenderersWithMPB(List<Renderer> renderers, float t)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (r == null) continue;

                // We set only properties that exist on at least one of its materials
                bool anyRelevant = false;
                var mats = r.sharedMaterials;
                if (mats != null)
                {
                    for (int m = 0; m < mats.Length; m++)
                    {
                        if (MaterialIsRelevant(mats[m])) { anyRelevant = true; break; }
                    }
                }
                if (!anyRelevant) continue;

                r.GetPropertyBlock(_mpb);

                // We cannot know which exact sub-material should get which values via MPB,
                // but in practice your foliage shader properties are consistent across these materials.
                // Apply the "global spring direction" to all relevant renderers:
                // (If you need per-material-specific logic per renderer, tell me and we'll add per-slot logic.)
                // --- Targets from your list ---
                // Grass_1: _GroundFadeHeight -> -5, _GroundFadeContrast -> 0.46
                // Grass_2 / Leaves_1: _BaseTexColorTint -> SpringGreen
                // Leaves_2: _BlendTextureHeight -> -5, _BlendTextureContrast -> 0
                // Branch_1/2: _BlendTextureOpacity -> 0
                // Stone_1: _SnowLevel -> 0

                // For MPB we do not branch by material name. We push values only if property exists.
                if (HasAnyMaterialProp(mats, GroundFadeHeight)) _mpb.SetFloat(GroundFadeHeight, Mathf.Lerp(GetBaselineFloat(mats, GroundFadeHeight), -5f, t));
                if (HasAnyMaterialProp(mats, GroundFadeContrast)) _mpb.SetFloat(GroundFadeContrast, Mathf.Lerp(GetBaselineFloat(mats, GroundFadeContrast), 0.46f, t));
                if (HasAnyMaterialProp(mats, BaseTexColorTint)) _mpb.SetColor(BaseTexColorTint, Color.Lerp(GetBaselineColor(mats, BaseTexColorTint), SpringGreen, t));
                if (HasAnyMaterialProp(mats, BlendTextureHeight)) _mpb.SetFloat(BlendTextureHeight, Mathf.Lerp(GetBaselineFloat(mats, BlendTextureHeight), -5f, t));
                if (HasAnyMaterialProp(mats, BlendTextureContrast)) _mpb.SetFloat(BlendTextureContrast, Mathf.Lerp(GetBaselineFloat(mats, BlendTextureContrast), 0f, t));
                if (HasAnyMaterialProp(mats, BlendTextureOpacity)) _mpb.SetFloat(BlendTextureOpacity, Mathf.Lerp(GetBaselineFloat(mats, BlendTextureOpacity), 0f, t));
                if (HasAnyMaterialProp(mats, SnowLevel)) _mpb.SetFloat(SnowLevel, Mathf.Lerp(GetBaselineFloat(mats, SnowLevel), 0f, t));

                r.SetPropertyBlock(_mpb);
            }
        }

        private void ApplyToClonedSharedMaterials(float t)
        {
            // Drive the runtime clones directly.
            foreach (var kv in _matCloneMap)
            {
                var mat = kv.Value;
                if (mat == null) continue;

                // Decide target based on material name (exactly your mapping)
                string n = mat.name;

                if (n.Contains("Grass_1"))
                {
                    if (mat.HasProperty(GroundFadeHeight))
                        mat.SetFloat(GroundFadeHeight, Mathf.Lerp(mat.GetFloat(GroundFadeHeight), -5f, t));
                    if (mat.HasProperty(GroundFadeContrast))
                        mat.SetFloat(GroundFadeContrast, Mathf.Lerp(mat.GetFloat(GroundFadeContrast), 0.46f, t));
                }
                else if (n.Contains("Grass_2"))
                {
                    if (mat.HasProperty(BaseTexColorTint))
                        mat.SetColor(BaseTexColorTint, Color.Lerp(mat.GetColor(BaseTexColorTint), SpringGreen, t));
                }
                else if (n.Contains("Leaves_1"))
                {
                    if (mat.HasProperty(BaseTexColorTint))
                        mat.SetColor(BaseTexColorTint, Color.Lerp(mat.GetColor(BaseTexColorTint), SpringGreen, t));
                }
                else if (n.Contains("Leaves_2"))
                {
                    if (mat.HasProperty(BlendTextureHeight))
                        mat.SetFloat(BlendTextureHeight, Mathf.Lerp(mat.GetFloat(BlendTextureHeight), -5f, t));
                    if (mat.HasProperty(BlendTextureContrast))
                        mat.SetFloat(BlendTextureContrast, Mathf.Lerp(mat.GetFloat(BlendTextureContrast), 0f, t));
                }
                else if (n.Contains("Branch_1") || n.Contains("Branch_2"))
                {
                    if (mat.HasProperty(BlendTextureOpacity))
                        mat.SetFloat(BlendTextureOpacity, Mathf.Lerp(mat.GetFloat(BlendTextureOpacity), 0f, t));
                }
                else if (n.Contains("Stone_1"))
                {
                    if (mat.HasProperty(SnowLevel))
                        mat.SetFloat(SnowLevel, Mathf.Lerp(mat.GetFloat(SnowLevel), 0f, t));
                }
            }
        }

        // Helpers: baseline fetch (material's current value is treated as baseline)
        private static bool HasAnyMaterialProp(Material[] mats, int propId)
        {
            if (mats == null) return false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m != null && m.HasProperty(propId)) return true;
            }
            return false;
        }

        private static float GetBaselineFloat(Material[] mats, int propId)
        {
            if (mats == null) return 0f;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m != null && m.HasProperty(propId)) return m.GetFloat(propId);
            }
            return 0f;
        }

        private static Color GetBaselineColor(Material[] mats, int propId)
        {
            if (mats == null) return Color.white;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m != null && m.HasProperty(propId)) return m.GetColor(propId);
            }
            return Color.white;
        }
    }
}
