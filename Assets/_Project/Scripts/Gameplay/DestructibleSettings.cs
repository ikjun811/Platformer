using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 부서지는 환경 요소 수치 (GDD 4.7): X1 가스탱크, X3 파괴 상자. 단위는 유닛(= 타일)과 초.
    /// </summary>
    [CreateAssetMenu(fileName = "DestructibleSettings", menuName = "Platformer/Destructible Settings")]
    public class DestructibleSettings : ScriptableObject
    {
        [Tooltip("체력")]
        [SerializeField] int maxHealth = 30;

        [Header("폭발 (가스탱크)")]
        [Tooltip("부서질 때 폭발하는지")]
        [SerializeField] bool explodes;
        [Tooltip("폭발 반지름 (타일)")]
        [SerializeField] float explosionRadius = 1.5f;
        [Tooltip("폭발 피해 (적·환경 요소)")]
        [SerializeField] int explosionDamage = 40;
        [Tooltip("폭발 피해 (플레이어)")]
        [SerializeField] int playerExplosionDamage = 20;
        [Tooltip("폭발에 맞은 대상의 넉백 배율")]
        [SerializeField] float explosionKnockbackScale = 1.5f;
        [Tooltip("폭발 이펙트 크기 배율")]
        [SerializeField] float effectScale = 2f;

        public int MaxHealth => maxHealth;
        public bool Explodes => explodes;
        public float ExplosionRadius => explosionRadius;
        public int ExplosionDamage => explosionDamage;
        public int PlayerExplosionDamage => playerExplosionDamage;
        public float ExplosionKnockbackScale => explosionKnockbackScale;
        public float EffectScale => effectScale;
    }
}
