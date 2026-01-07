using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

namespace _Scripts.EffectModules
{
    [RequireComponent(typeof(SplineAnimate))]
    public class ButterflySpawner : MonoBehaviour
    {
        [Header("Anchor Reveal Timing")]
        [Tooltip("在这些时间点（秒）触发对应的 Anchor")]
        [SerializeField] private List<float> anchorRevealTimes = new List<float> 
        { 
        };
        
        [Header("Debug")]
        [SerializeField] private bool showDebug = true;
        
        private SplineAnimate _splineAnimate;
        private bool _isAnimating = false;
        private List<bool> _anchorRevealed = new List<bool>();

        void Awake()
        {
            _splineAnimate = GetComponent<SplineAnimate>();
        }

        /// <summary>
        /// 启动动画（从 Phase2Controller 调用）
        /// </summary>
        public void SpawnButterflies()
        {
            if (_isAnimating) return;
            
            // 重置触发状态
            _anchorRevealed.Clear();
            for (int i = 0; i < anchorRevealTimes.Count; i++)
            {
                _anchorRevealed.Add(false);
            }
            
            // 启动动画
            _splineAnimate.Restart(true);
            _isAnimating = true;
        }

        void Update()
        {
            if (!_isAnimating) return;

            // 获取当前时间
            float currentTime = _splineAnimate.ElapsedTime;

            // 检查触发点
            for (int i = 0; i < anchorRevealTimes.Count; i++)
            {
                if (_anchorRevealed[i]) continue;

                if (currentTime >= anchorRevealTimes[i])
                {
                    TeleportPathController.Instance?.RevealAnchor(i);
                    _anchorRevealed[i] = true;
                    
                    if (showDebug)
                        Debug.Log($"[ButterflySpawner] {currentTime:F1}s → Anchor {i}");
                }
            }

            // 检查是否结束
            if (!_splineAnimate.IsPlaying)
            {
                _isAnimating = false;
            }
        }
    }
}