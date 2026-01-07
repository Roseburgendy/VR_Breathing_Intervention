using UnityEngine;

namespace _Scripts.EffectModules
{
    /// <summary>
    /// Reusable breath-driven crystal visuals
    /// - emission
    /// - scale
    /// - optional particles / audio
    /// Can be used by ANY activated crystal.
    /// </summary>
    public class CrystalBreathResponder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform crystal;
        [SerializeField] private Renderer crystalRenderer;

        [Header("Breath (Emission HDR + Scale)")] 
        [ColorUsage(true, true)] 
        [SerializeField] private Color baseEmissionColor = new Color(0.35f, 0.55f, 1.0f, 1f); 
        [ColorUsage(true, true)] 
        [SerializeField] private Color inhaleEmissionColor = new Color(1.0f, 0.85f, 0.3f, 1f);
        [Header("Scale")]
        [SerializeField] private float inhaleScaleMultiplier = 1.2f;

        [Header("Optional Effects")]
        [SerializeField] private ParticleSystem inhaleParticles;
        [SerializeField] private ParticleSystem exhaleParticles;
        

        private MaterialPropertyBlock _mpb;
        private Vector3 _baseScale;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();

            if (!crystal && crystalRenderer != null)
                crystal = crystalRenderer.transform;

            if (crystal != null)
                _baseScale = crystal.localScale;

            // 获取材质实例
            if (crystalRenderer != null)
            {
                _crystalMaterial = crystalRenderer.material; // 创建材质实例
                EnableEmission(); // 确保启用 emission
            }

            ApplyEmission(baseEmissionColor);
        }
        // ---------- API for BreathRhythm ----------

        public void OnInhaleStart()
        {
            if (inhaleParticles) inhaleParticles.Play(true);

            if (AudioManager.instance )
                AudioManager.instance.Play("crystalInhale");
        }

        public void UpdateInhale(float t)
        {
            t = Mathf.Clamp01(t);
            ApplyEmission(Color.Lerp(baseEmissionColor, inhaleEmissionColor, t));

            if (crystal != null)
                crystal.localScale = Vector3.Lerp(_baseScale, _baseScale * inhaleScaleMultiplier, t);
        }

        public void OnExhaleStart()
        {
            if (exhaleParticles) exhaleParticles.Play(true);

            if (AudioManager.instance )
                AudioManager.instance.Play("crystalExhale");
        }

        public void UpdateExhale(float t)
        {
            t = Mathf.Clamp01(t);
            ApplyEmission(Color.Lerp(inhaleEmissionColor, baseEmissionColor, t));

            if (crystal != null)
                crystal.localScale = Vector3.Lerp(_baseScale * inhaleScaleMultiplier, _baseScale, t);
        }

        private Material _crystalMaterial; // 添加引用



        private void EnableEmission()
        {
            _crystalMaterial.EnableKeyword("_EMISSION");
            _crystalMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        private void ApplyEmission(Color c)
        {
            if (crystalRenderer == null) return;

            // 方法1: 使用 MaterialPropertyBlock (推荐,性能更好)
            crystalRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_EmissionColor", c);
            crystalRenderer.SetPropertyBlock(_mpb);

            // 方法2: 如果上面不行,用这个 (会创建材质实例)
            // _crystalMaterial.SetColor("_EmissionColor", c);
            // DynamicGI.SetEmissive(crystalRenderer, c);
        }
    }
}
