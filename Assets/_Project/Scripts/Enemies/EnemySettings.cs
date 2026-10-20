using UnityEngine;

namespace Platformer.Enemies
{
    /// <summary>
    /// 모든 적이 공통으로 쓰는 수치. 적 종류별 설정은 이 클래스를 상속해 이동·공격 수치를 더한다.
    /// </summary>
    public abstract class EnemySettings : ScriptableObject
    {
        [Header("공통")]
        [Tooltip("최대 체력 (빔 10, 차지 빔 30 기준으로 조절)")]
        [SerializeField] int maxHealth = 30;
        [Tooltip("플레이어가 몸에 닿았을 때 주는 피해")]
        [SerializeField] int contactDamage = 10;
        [Tooltip("피해를 받았을 때 하얗게 번쩍이는 시간 (초)")]
        [SerializeField] float hitFlashTime = 0.08f;

        public int MaxHealth => maxHealth;
        public int ContactDamage => contactDamage;
        public float HitFlashTime => hitFlashTime;
    }
}
