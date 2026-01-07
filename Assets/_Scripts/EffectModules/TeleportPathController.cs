using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.EffectModules
{
    /// <summary>
    /// 传送路径控制器 - 管理 Teleport Anchors 的淡入显示
    /// </summary>
    public class TeleportPathController : MonoBehaviour
    {
        public static TeleportPathController Instance { get; private set; }

        [Header("Path Anchors")]
        [SerializeField] private List<GameObject> teleportAnchors = new List<GameObject>();
        
        [Header("Reveal Settings")]
        [SerializeField] private float revealDuration = 1f;
        [SerializeField] private AnimationCurve revealCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private bool useScaleAnimation = true;
        [SerializeField] private Vector3 startScale = Vector3.zero;
        [SerializeField] private Vector3 endScale = Vector3.one;
        
        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        private List<bool> _anchorRevealed = new List<bool>();

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void Start()
        {
            InitializeAnchors();
        }
        
        void InitializeAnchors()
        {
            _anchorRevealed.Clear();

            foreach (var anchor in teleportAnchors)
            {
                if (anchor == null) continue;

                _anchorRevealed.Add(false);

                // 初始状态：隐藏
                if (useScaleAnimation)
                    anchor.transform.localScale = startScale;

                anchor.SetActive(false);
            }

            if (showDebug)
                Debug.Log($"[TeleportPath] Initialized {teleportAnchors.Count} anchors");
        }

        /// <summary>
        /// 显示指定索引的 Anchor
        /// </summary>
        public void RevealAnchor(int index)
        {
            if (index < 0 || index >= teleportAnchors.Count)
            {
                Debug.LogWarning($"[TeleportPath] Invalid index: {index}");
                return;
            }

            if (_anchorRevealed[index]) return;

            _anchorRevealed[index] = true;
            StartCoroutine(RevealAnchorCoroutine(index));
        }

        IEnumerator RevealAnchorCoroutine(int index)
        {
            GameObject anchor = teleportAnchors[index];
            if (anchor == null) yield break;

            anchor.SetActive(true);

            if (showDebug)
                Debug.Log($"[TeleportPath] Revealing {anchor.name}");

            float elapsed = 0f;

            while (elapsed < revealDuration)
            {
                elapsed += Time.deltaTime;
                float t = revealCurve.Evaluate(elapsed / revealDuration);

                if (useScaleAnimation)
                    anchor.transform.localScale = Vector3.Lerp(startScale, endScale, t);

                yield return null;
            }

            // 确保最终状态
            if (useScaleAnimation)
                anchor.transform.localScale = endScale;
        }
        #region Debug

        [ContextMenu("Reset All")]
        public void ResetAllAnchors()
        {
            InitializeAnchors();
        }

        [ContextMenu("Reveal All")]
        public void RevealAllAnchors()
        {
            for (int i = 0; i < teleportAnchors.Count; i++)
                RevealAnchor(i);
        }

        void OnDrawGizmos()
        {
            if (teleportAnchors == null || teleportAnchors.Count == 0) return;

            // 路径连线
            Gizmos.color = Color.cyan;
            for (int i = 0; i < teleportAnchors.Count - 1; i++)
            {
                if (teleportAnchors[i] != null && teleportAnchors[i + 1] != null)
                {
                    Gizmos.DrawLine(
                        teleportAnchors[i].transform.position,
                        teleportAnchors[i + 1].transform.position
                    );
                }
            }

            // Anchor 位置
            for (int i = 0; i < teleportAnchors.Count; i++)
            {
                if (teleportAnchors[i] != null)
                {
                    Gizmos.color = Application.isPlaying && i < _anchorRevealed.Count && _anchorRevealed[i] 
                        ? Color.green 
                        : Color.yellow;
                    
                    Gizmos.DrawWireSphere(teleportAnchors[i].transform.position, 0.3f);
                }
            }
        }

        #endregion
    }
}