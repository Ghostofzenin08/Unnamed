using UnityEngine;

namespace Outpost404.Combat
{
    /// <summary>
    /// Unified damage interface implemented by destructible entities,
    /// including Player, Enemies, and Harvestable Resource Nodes.
    /// </summary>
    public interface IDamageable
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsDead { get; }

        void TakeDamage(float amount, Vector3 hitPoint = default, Vector3 hitNormal = default);
    }
}
