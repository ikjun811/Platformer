using UnityEngine;

namespace Platformer.Enemies
{
    /// <summary>지상 순찰형 적 수치.</summary>
    [CreateAssetMenu(fileName = "EnemyPatrolSettings", menuName = "Platformer/Enemy Patrol Settings")]
    public class EnemyPatrolSettings : EnemySettings
    {
        [Header("순찰")]
        [Tooltip("이동 속도 (타일/초)")]
        [SerializeField] float moveSpeed = 2f;

        public float MoveSpeed => moveSpeed;
    }
}
