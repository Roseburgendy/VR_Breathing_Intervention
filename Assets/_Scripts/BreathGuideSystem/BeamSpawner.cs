using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.BreathGuideSystem
{
    public class BeamSpawner : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [SerializeField] private GameObject leftBeamSegmentPrefab;
        [SerializeField] private GameObject rightBeamSegmentPrefab;
        [SerializeField] private Transform spawnPoint;

        [Header("Beam Settings")]
        [SerializeField] private MovementType currentMovementType = MovementType.VerticalUp;
        [SerializeField] private bool spawnForLeftHand = true;
        [SerializeField] private bool spawnForRightHand = true;

        [Header("Ring (world XY)")]
        [SerializeField] private float ringRadius;
        
        private float _currentPhaseDuration = 4f;

        private readonly List<BeamSegment> _activeSegments = new List<BeamSegment>();
        public void SetPhaseDuration(float duration)
        {
            _currentPhaseDuration = Mathf.Max(0.1f, duration);
        }
        public void SetMovementType(MovementType type)
        {
            currentMovementType = type;
        }
        public GameObject GetLeftBeamPrefab() => leftBeamSegmentPrefab;
        public void ClearAllSegments()
        {
            foreach (var seg in _activeSegments)
            {
                if (seg != null) Destroy(seg.gameObject);
            }
            _activeSegments.Clear();
            
        }

        #region Spawn Implementation
        public void SpawnBeamSegments()
        {
            if (spawnPoint == null) return;
            if (spawnForLeftHand)
            {
                if (leftBeamSegmentPrefab == null) return;
                DoSpawning(true, leftBeamSegmentPrefab);
            }

            if (spawnForRightHand)
            {
                if (rightBeamSegmentPrefab == null) return;
                DoSpawning(false, rightBeamSegmentPrefab);
            }
        }
        private void DoSpawning(bool isLeftHand, GameObject segmentPrefab)
        {
            Vector3 ringCenter = spawnPoint.position;
            Vector3 ringRight = Vector3.right;
            Vector3 spawnPosition = PathCalculator.GetCP2OnPathXY(
                ringCenter,
                ringRadius,
                currentMovementType,
                isLeftHand,
                0f, 
                ringRight
            );
            GameObject obj = Instantiate(segmentPrefab, spawnPosition, Quaternion.identity);
            BeamSegment segment = obj.GetComponent<BeamSegment>();
            if (segment != null)
            {
                segment.Init(
                    spawnPoint.forward,
                    currentMovementType,
                    isLeftHand,
                    ringCenter,
                    ringRadius,
                    ringRight,
                    _currentPhaseDuration
                );
                _activeSegments.Add(segment);
            }
        }
        private void OnDestroy()
        {
            ClearAllSegments();
        }
        #endregion
    }
}
