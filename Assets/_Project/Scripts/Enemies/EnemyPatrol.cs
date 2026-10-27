using UnityEngine;

namespace Platformer.Enemies
{
    /// <summary>
    /// 지상 순찰형 적 (GDD 4.5). 일정한 속도로 걷다가 앞이 벽이거나 낭떠러지면 돌아선다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Enemy))]
    public class EnemyPatrol : MonoBehaviour
    {
        // 벽·바닥 감지 거리 (타일)
        const float WallCheckDistance = 0.1f;
        const float LedgeCheckDistance = 0.3f;

        [SerializeField] EnemyPatrolSettings settings;
        [Tooltip("벽과 바닥으로 볼 레이어")]
        [SerializeField] LayerMask groundLayers;
        [Tooltip("처음 걷는 방향: 1(오른쪽), -1(왼쪽)")]
        [SerializeField] int startDirection = -1;
        [Tooltip("진행 방향에 맞춰 좌우 반전할 그림")]
        [SerializeField] SpriteRenderer spriteRenderer;

        Rigidbody2D body;
        BoxCollider2D box;
        Enemy enemy;
        int direction;

        void Reset()
        {
            groundLayers = LayerMask.GetMask("Ground", "OneWay");
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            enemy = GetComponent<Enemy>();
            body.freezeRotation = true;
            direction = startDirection >= 0 ? 1 : -1;
        }

        void FixedUpdate()
        {
            // 경직 중에는 밀려나는 속도를 덮어쓰지 않는다
            if (enemy.IsStunned) return;

            bool grounded = CheckGround();
            if (grounded && ShouldTurn())
                direction = -direction;

            body.linearVelocity = new Vector2(grounded ? direction * settings.MoveSpeed : 0f, body.linearVelocity.y);

            if (spriteRenderer != null)
                spriteRenderer.flipX = direction < 0;
        }

        bool CheckGround()
        {
            Bounds b = box.bounds;
            return Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.15f, groundLayers);
        }

        bool ShouldTurn()
        {
            Bounds b = box.bounds;
            float frontX = direction > 0 ? b.max.x : b.min.x;

            // 앞에 벽이 있는가
            var wallOrigin = new Vector2(frontX, b.center.y);
            if (Physics2D.Raycast(wallOrigin, Vector2.right * direction, WallCheckDistance, groundLayers))
                return true;

            // 앞쪽 발밑이 비어 있는가 (낭떠러지)
            var ledgeOrigin = new Vector2(frontX + direction * WallCheckDistance, b.min.y + 0.05f);
            return !Physics2D.Raycast(ledgeOrigin, Vector2.down, LedgeCheckDistance, groundLayers);
        }

        void OnDrawGizmosSelected()
        {
            var collider = GetComponent<BoxCollider2D>();
            if (collider == null) return;

            Bounds b = collider.bounds;
            int dir = Application.isPlaying ? direction : (startDirection >= 0 ? 1 : -1);
            float frontX = dir > 0 ? b.max.x : b.min.x;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(frontX, b.center.y), new Vector3(frontX + dir * WallCheckDistance, b.center.y));
            Gizmos.DrawLine(new Vector3(frontX + dir * WallCheckDistance, b.min.y + 0.05f),
                new Vector3(frontX + dir * WallCheckDistance, b.min.y + 0.05f - LedgeCheckDistance));
        }
    }
}
