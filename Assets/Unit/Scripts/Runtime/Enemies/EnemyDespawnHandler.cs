using UnityEngine;

namespace EndlessGuard.Unit.Runtime
{
    public abstract class EnemyDespawnHandler : MonoBehaviour
    {
        public abstract void Despawn(float delay);
    }
}
