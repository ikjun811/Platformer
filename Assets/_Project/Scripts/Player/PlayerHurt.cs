using Platformer.Gameplay;
using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 플레이어 피격 처리 (GDD 4.3).
    /// - 일반 피해: 피해 반대 방향으로 넉백, 잠깐 조작 불가, 무적 시간 동안 깜빡임
    /// - 환경 피해(가시 등): 넉백 대신 마지막 안전한 발판으로 되돌림
    /// - 에너지 0: 임시로 시작 위치에서 에너지를 채워 재시작 (세이브 스테이션 재시작은 M3)
    /// </summary>
    [RequireComponent(typeof(Health), typeof(PlayerMovement), typeof(Rigidbody2D))]
    public class PlayerHurt : MonoBehaviour
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
        PlayerSlide slide;
        PlayerLedgeGrab ledge;

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
            slide = GetComponent<PlayerSlide>();
            ledge = GetComponent<PlayerLedgeGrab>();

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
            if (ledge != null)
                ledge.CancelHang();

            if (damage.IsHazard)
            {
                Teleport(lastSafePosition);
                movement.Stun(settings.StunTime);
                return;
            }

            // 슬라이드 중에는 밀려나지 않는다 (낮은 통로에서 튕겨 나가 끼지 않도록)
            if (slide != null && slide.IsSliding) return;

            int direction = body.position.x >= damage.SourcePosition.x ? 1 : -1;
            Vector2 knockback = settings.KnockbackVelocity;
            movement.ApplyKnockback(new Vector2(direction * knockback.x, knockback.y), settings.StunTime);
        }

        void OnDied()
        {
            Debug.Log("[PlayerHurt] 에너지 0 → 시작 위치에서 재시작 (임시, 세이브 스테이션 재시작은 M3)");
            Teleport(spawnPosition);
            lastSafePosition = spawnPosition;
            health.RestoreFull();
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
