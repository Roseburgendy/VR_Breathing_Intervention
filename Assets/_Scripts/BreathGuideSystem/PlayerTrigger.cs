using UnityEngine;

namespace _Scripts.BreathGuideSystem
{
    public class PlayerTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            BeamSegment segment = other.GetComponentInParent<BeamSegment>();
            if (segment == null) return;
            
        }
    }
}