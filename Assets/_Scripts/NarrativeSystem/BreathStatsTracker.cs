using UnityEngine;

namespace _Scripts.NarrativeSystem
{
    public class BreathStatsTracker : MonoBehaviour
    {
        public static BreathStatsTracker Instance { get; private set; }

        [Header("Phase 1")]
        public int Phase1SystemCycles { get; private set; }
        public int Phase1PlayerCyclesHit { get; private set; }
        public int Phase1PlayerCyclesParticipated { get; private set; }

        [Header("Phase 2")]
        public int Phase2SystemCycles { get; private set; }
        public int Phase2PlayerCyclesParticipated { get; private set; }
        public int Phase2PlayerCyclesCompleted { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ResetAll()
        {
            Phase1SystemCycles = 0;
            Phase1PlayerCyclesHit = 0;
            Phase1PlayerCyclesParticipated = 0;

            Phase2SystemCycles = 0;
            Phase2PlayerCyclesParticipated = 0;
            Phase2PlayerCyclesCompleted = 0;
        }

        // ───────── Phase 1 ─────────
        public void AddPhase1SystemCycle()
        {
            Phase1SystemCycles++;
        }

        public void RecordPhase1PlayerCycle(bool participated, bool hit)
        {
            if (participated) Phase1PlayerCyclesParticipated++;
            if (hit) Phase1PlayerCyclesHit++;
        }

        public float GetPhase1HitRate()
        {
            if (Phase1SystemCycles <= 0) return 0f;
            return (float)Phase1PlayerCyclesHit / Phase1SystemCycles;
        }

        // ───────── Phase 2（你已有） ─────────
        public void AddPhase2SystemCycle() => Phase2SystemCycles++;
        public void RecordPhase2PlayerCycle(bool participated, bool completed)
        {
            if (participated) Phase2PlayerCyclesParticipated++;
            if (completed) Phase2PlayerCyclesCompleted++;
        }
    }
}
