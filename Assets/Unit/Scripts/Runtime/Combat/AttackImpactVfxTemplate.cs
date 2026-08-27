using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    [DisallowMultipleComponent]
    public sealed class AttackImpactVfxTemplate : MonoBehaviour
    {
        [SerializeField] private string impactId = "normal_hit";

        public string ImpactId => string.IsNullOrWhiteSpace(impactId) ? gameObject.name : impactId;
    }
}
