using Platformer.Gameplay;
using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 플레이어 피격 처리 (GDD 4.3).
    /// - 일반 피해: 피해 반대 방향으로 넉백, 잠깐 조작 불가, 무적 시간 동안 깜빡임
    /// - 환경 피해(가시 등): 넉백 대신 마지막 안전한 발판으로 되돌림
    /// - 에너지 0: 부활 위치에서 체력·열을 채워 다시 시작한다 (GDD 7). 부활 위치는 처음엔 시작 위치이고,
    ///   소탕 방이 시작되면 그 방 입구로 바뀐다 (Room이 SetRespawnPoint 호출)
    /// </summary>
    [RequireComponent(typeof(Health), typeof(PlayerMovement), typeof(Rigidbody2D))]
    public class PlayerHurt : MonoBehaviour, IRespawnable
    {
        [SerializeField] PlayerHurtSettings settings;
        [Tooltip("무적 중 깜빡일 그림")]
        [SerializeField] SpriteRenderer blinkTarget;
        [Tooltip("환경 피해 레이어. 이 근처에 있지 않을 때만 안전한 발판으로 기록한다")]
        [SerializeField] LayerMask hazardLayers;
        [Tooltip("안전한 발판으로 기록하려면 환경 피해에서 이만큼 떨어져 있어야 한다 (타일)")]
        [SerializeField] float hazardMargin = 0.75f;

        Health health;
        PlayerMovement movement;
        Rigidbody2D body;
        BoxCollider2D box;
        PlayerHeat heat;

        Vector2 spawnPosition;
        Vector2 lastSafePosition;

        void Reset()
        {
            hazardLayers = LayerMask.GetMask("EnemyProjectile");
        }

        void Awake()
        {
            health = GetComponent<Health>();
            movement = GetComponent<PlayerMovement>();
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            heat = GetComponent<PlayerHeat>();

            health.SetMax(settings.MaxEnergy, true);
            spawnPosition = lastSafePosition = body.position;
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

        void FixedUpdate()
        {
            if (movement.IsGrounded && !movement.IsStunned && !NearHazard())
                lastSafePosition = body.position;
        }

        void LateUpdate()
        {
            if (blinkTarget == null) return;
            blinkTarget.enabled = !health.IsInvulnerable || Mathf.Repeat(Time.time * settings.BlinkRate, 1f) < 0.5f;
        }

        bool NearHazard()
        {
            Bounds b = box.bounds;
            Vector2 size = (Vector2)b.size + new Vector2(hazardMargin * 2f, hazardMargin);
            return Physics2D.OverlapBox(b.center, size, 0f, hazardLayers);
        }

        void OnDamaged(DamageInfo damage)
        {
            Debug.Log($"[PlayerHurt] 피해 {damage.Amount} → 에너지 {health.Current}/{health.Max}");

            health.SetInvulnerable(settings.InvulnerableTime);

            if (damage.IsHazard)
            {
                Teleport(lastSafePosition);
                movement.Stun(settings.StunTime);
                return;
            }

            int direction = body.position.x >= damage.SourcePosition.x ? 1 : -1;
            Vector2 knockback = settings.KnockbackVelocity * damage.KnockbackScale;
            movement.ApplyKnockback(new Vector2(direction * knockback.x, knockback.y), settings.StunTime);
        }

        void OnDied()
        {
            Debug.Log("[PlayerHurt] 에너지 0 → 부활 위치에서 다시 시작");
            Teleport(spawnPosition);
            lastSafePosition = spawnPosition;
            health.RestoreFull();
            if (heat != null)
                heat.ResetHeat();
        }

        /// <summary>부활 위치를 정한다 (발 위치). 몸 위치(충돌체 바닥이 발에 오는 위치)로 바꿔 저장한다.</summary>
        public void SetRespawnPoint(Vector2 feet)
        {
            spawnPosition = feet + new Vector2(0f, box.size.y * 0.5f - box.offset.y);
        }

        void Teleport(Vector2 position)
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
        }

        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lastSafePosition, 0.3f);
        }
    }
}
