using UnityEngine;
using _Scripts.NarrativeSystem;

public class Phase3AreaTrigger : MonoBehaviour
{
    [SerializeField] private bool triggerOnce = true;
    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        // 可选：加 tag 过滤，例如 XR Origin
        // if (!other.CompareTag("Player")) return;

        PhaseManager.Instance?.RequestStartPhase3();

        if (triggerOnce)
        {
            _triggered = true;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }
}