using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 8방향 조준 (GDD 3.3 조준 규칙).
    /// - 기본: W = 위, 달리며 W = 위 대각선. 공중에서는 S로 아래·아래 대각선
    /// - Left Shift(제자리 조준): 땅에서는 멈춰 서서 WASD로 8방향 조준 (땅에서 S만 누르면 앞쪽 아래 대각선)
    /// - 앉기·슬라이드 중에는 정면
    /// 사격 컴포넌트가 이 방향과 총구 위치를 가져다 쓴다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(BoxCollider2D), typeof(PlayerMovement), typeof(PlayerInputReader))]
    public class PlayerAim : MonoBehaviour
    {
        [SerializeField] PlayerCombatSettings settings;
        [Tooltip("임시 조준 표시. 총구 위치로 옮긴다 (아트 적용 후 제거)")]
        [SerializeField] Transform aimMarker;

        BoxCollider2D box;
        PlayerMovement movement;
        PlayerInputReader input;
        PlayerCrouch crouch;
        PlayerSlide slide;

        /// <summary>조준 방향 (8방향 중 하나, 각 성분 -1/0/1)</summary>
        public Vector2Int Direction8 { get; private set; } = Vector2Int.right;

        /// <summary>정규화된 조준 방향</summary>
        public Vector2 Direction => ((Vector2)Direction8).normalized;

        /// <summary>제자리 조준 중인지 (매달리기 등 조작이 잠긴 동안은 false)</summary>
        public bool IsAimLocked => input.AimLockHeld && !movement.ControlLocked;

        /// <summary>빔이 나가는 위치. 앉으면 충돌체가 낮아지므로 어깨 높이도 함께 내려간다</summary>
        public Vector2 MuzzlePosition
        {
            get
            {
                Bounds b = box.bounds;
                var shoulder = new Vector2(b.center.x, b.max.y - settings.ShoulderDepth);
                return shoulder + Direction * settings.MuzzleDistance;
            }
        }

        void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            movement = GetComponent<PlayerMovement>();
            input = GetComponent<PlayerInputReader>();
            crouch = GetComponent<PlayerCrouch>();
            slide = GetComponent<PlayerSlide>();
        }

        void Update()
        {
            Direction8 = ResolveDirection();
        }

        void LateUpdate()
        {
            if (aimMarker != null)
                aimMarker.position = MuzzlePosition;
        }

        Vector2Int ResolveDirection()
        {
            int facing = movement.FacingDirection;
            int vertical = input.Vertical;
            bool grounded = movement.IsGrounded;

            if (IsAimLocked)
            {
                if (vertical == 0) return new Vector2Int(facing, 0);
                // 바로 아래는 공중에서만. 땅에서 S만 누르면 앞쪽 아래 대각선
                if (input.Horizontal == 0 && (vertical == 1 || !grounded)) return new Vector2Int(0, vertical);
                return new Vector2Int(facing, vertical);
            }

            bool low = (crouch != null && crouch.IsCrouching) || (slide != null && slide.IsSliding);
            if (low) return new Vector2Int(facing, 0);

            if (vertical == 1)
                return input.Horizontal == 0 ? Vector2Int.up : new Vector2Int(facing, 1);

            // 땅에서 S는 앉기·슬라이드에 쓰이므로 아래 조준은 공중에서만
            if (vertical == -1 && !grounded)
                return input.Horizontal == 0 ? Vector2Int.down : new Vector2Int(facing, -1);

            return new Vector2Int(facing, 0);
        }

        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || settings == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(MuzzlePosition, MuzzlePosition + Direction * 2f);
        }
    }
}
