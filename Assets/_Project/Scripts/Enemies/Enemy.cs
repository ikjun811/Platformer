using Platformer.Gameplay;
using UnityEngine;

namespace Platformer.Enemies
{
    /// <summary>
    /// 모든 적의 공통 동작: 체력 설정, 피격 번쩍임, 접촉 피해, 사망.
    /// 이동은 종류별 컴포넌트(EnemyPatrol 등)가 맡는다.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        [SerializeField] EnemySettings settings;
        [Tooltip("피격 때 번쩍일 그림")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [Tooltip("몸에 닿으면 피해를 주는 레이어")]
        [SerializeField] LayerMask contactTargets;

        Health health;
        Rigidbody2D body;
        Color baseColor;
        float flashEndTime;
        float stunEndTime;

        /// <summary>경직 중인지. 경직 중에는 이동 컴포넌트가 움직이지 않는다 (밀려나는 속도는 물리에 맡긴다)</summary>
        public bool IsStunned => Time.time < stunEndTime;

        void Reset()
        {
            contactTargets = LayerMask.GetMask("Player");
        }

        void Awake()
        {
            health = GetComponent<Health>();
            body = GetComponent<Rigidbody2D>();
            health.SetMax(settings.MaxHealth, true);
            if (spriteRenderer != null)
                baseColor = spriteRenderer.color;
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void Update()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Time.time < flashEndTime ? Color.white : baseColor;
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            if ((contactTargets.value & (1 << collision.gameObject.layer)) == 0) return;

            var target = collision.collider.GetComponentInParent<IDamageable>();
            target?.TakeDamage(new DamageInfo(settings.ContactDamage, transform.position));
        }

        /// <summary>속도를 바꿔 밀쳐내고 일정 시간 경직시킨다 (과열 배출 충격파, 카운터 등).</summary>
        public void ApplyKnockback(Vector2 velocity, float stunDuration)
        {
            if (body != null)
                body.linearVelocity = velocity;
            stunEndTime = Mathf.Max(stunEndTime, Time.time + stunDuration);
        }

        void OnDamaged(DamageInfo damage)
        {
            flashEndTime = Time.time + settings.HitFlashTime;
        }

        void OnDied()
        {
            // 처치 연출과 아이템 드롭은 추후 (GDD 4.1 회복 아이템)
            Destroy(gameObject);
        }
    }
}
