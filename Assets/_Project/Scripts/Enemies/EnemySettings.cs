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
        [Tooltip("죽은 뒤 사라지기까지 걸리는 시간 (초). 이 동안 사망 동작을 보여준다")]
        [SerializeField] float deathDuration = 0.5f;
        [Tooltip("경직 중 밀려나는 가로 속도가 초당 줄어드는 양 (타일/초²). 몸에 마찰이 없어서 이 값으로 멈춘다")]
        [SerializeField] float knockbackDeceleration = 2f;
        [Tooltip("밀려나는 속도 배율 (1 = 그대로, 0.5 = 절반). 무거운 적은 작게")]
        [SerializeField] float knockbackScale = 1f;
        [Tooltip("보는 방향(정면)에서 맞은 피해 배율 (1 = 감소 없음). 카운터 처치는 영향을 받지 않는다")]
        [SerializeField, Range(0f, 1f)] float frontDamageMultiplier = 1f;

        public int MaxHealth => maxHealth;
        public int ContactDamage => contactDamage;
        public float HitFlashTime => hitFlashTime;
        public float DeathDuration => deathDuration;
        public float KnockbackDeceleration => knockbackDeceleration;
        public float KnockbackScale => knockbackScale;
        public float FrontDamageMultiplier => frontDamageMultiplier;
    }
}
