using Platformer.Gameplay;
using UnityEngine;

namespace Platformer.Enemies
{
    /// <summary>
    /// 모든 적의 공통 동작: 체력 설정, 피격 번쩍임, 접촉 피해, 넉백·경직, 사망.
    /// 이동·공격은 종류별 컴포넌트(EnemyRunner 등)가 맡는다.
    /// 죽으면 충돌을 끄고 멈춘 뒤 사망 시간(DeathDuration)이 지나면 사라진다. 사망 동작은 종류별 컴포넌트가 보여준다.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour, IDamageModifier
    {
        [SerializeField] EnemySettings settings;
        [Tooltip("피격 때 색으로 번쩍일 그림 (임시 사각형 그래픽용. 실루엣 오버레이를 쓰는 적은 비워 둔다)")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [Tooltip("몸에 닿으면 피해를 주는 레이어")]
        [SerializeField] LayerMask contactTargets;

        Health health;
        Rigidbody2D body;
        Collider2D[] colliders;
        Color baseColor;
        float flashEndTime;
        float stunEndTime;
        float counterStaggerEndTime;

        /// <summary>경직 중인지. 경직 중에는 이동 컴포넌트가 움직이지 않는다 (밀려나는 속도는 물리에 맡긴다)</summary>
        public bool IsStunned => Time.time < stunEndTime;

        /// <summary>카운터에 당해 경직 중인지. 이 동안 가까이서 쏘면 즉사한다 (카운터 처치, GDD 4.5)</summary>
        public bool IsCounterStaggered => !IsDead && Time.time < counterStaggerEndTime;

        /// <summary>피격 번쩍임 중인지 (실루엣 오버레이를 쓰는 적이 참고한다)</summary>
        public bool IsHitFlashing => Time.time < flashEndTime;

        public bool IsDead => health.IsDead;

        /// <summary>보는 방향: 1(오른쪽), -1(왼쪽). 이동 컴포넌트가 정한다 (정면 피해 감소 판정용)</summary>
        public int FacingDirection { get; set; } = -1;

        void Reset()
        {
            contactTargets = LayerMask.GetMask("Player");
        }

        void Awake()
        {
            health = GetComponent<Health>();
            body = GetComponent<Rigidbody2D>();
            colliders = GetComponents<Collider2D>();
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
                spriteRenderer.color = IsHitFlashing ? Color.white : baseColor;
        }

        void FixedUpdate()
        {
            // 경직 중에는 이동 컴포넌트가 속도를 건드리지 않으므로, 밀려나는 가로 속도를 여기서 줄여 멈춘다
            if (!IsStunned || IsDead || body == null) return;

            Vector2 velocity = body.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, settings.KnockbackDeceleration * Time.fixedDeltaTime);
            body.linearVelocity = velocity;
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            if (IsDead || (contactTargets.value & (1 << collision.gameObject.layer)) == 0) return;

            var target = collision.collider.GetComponentInParent<IDamageable>();
            target?.TakeDamage(new DamageInfo(settings.ContactDamage, transform.position));
        }

        /// <summary>속도를 바꿔 밀쳐내고 일정 시간 경직시킨다 (과열 배출 충격파, 카운터 등).</summary>
        public void ApplyKnockback(Vector2 velocity, float stunDuration)
        {
            if (IsDead) return;
            if (body != null)
                body.linearVelocity = velocity * settings.KnockbackScale;
            stunEndTime = Mathf.Max(stunEndTime, Time.time + stunDuration);
        }

        // 보는 쪽에서 날아온 피해를 줄인다 (브루트, GDD 4.6)
        public int ModifyDamage(in DamageInfo damage)
        {
            if (settings.FrontDamageMultiplier >= 1f || damage.IgnoresArmor) return damage.Amount;

            float side = damage.SourcePosition.x - transform.position.x;
            bool fromFront = side * FacingDirection > 0f;
            if (!fromFront) return damage.Amount;
            return Mathf.Max(1, Mathf.RoundToInt(damage.Amount * settings.FrontDamageMultiplier));
        }

        /// <summary>카운터 성공: 밀쳐내고 경직시키며, 경직 동안 카운터 처치를 받을 수 있게 한다.</summary>
        public void Stagger(Vector2 velocity, float duration)
        {
            if (IsDead) return;
            ApplyKnockback(velocity, duration);
            counterStaggerEndTime = Time.time + duration;
        }

        /// <summary>남은 체력과 상관없이 즉시 죽인다 (카운터 처치).</summary>
        public void Kill(Vector2 source)
        {
            health.Kill(source);
        }

        void OnDamaged(DamageInfo damage)
        {
            flashEndTime = Time.time + settings.HitFlashTime;
        }

        void OnDied()
        {
            // 탄과 플레이어가 더 이상 닿지 않도록 충돌을 끄고 그 자리에 멈춘다. 처치 보상(체력 조각)은 N3 카운터 처치에서
            foreach (Collider2D collider in colliders)
                collider.enabled = false;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.bodyType = RigidbodyType2D.Kinematic;
            }
            Destroy(gameObject, settings.DeathDuration);
        }
    }
}
