using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 토글 방식 앉기 (시리즈 방식).
    /// 땅에서 S를 누르면 앉은 채 유지되고, W·좌우·점프를 누르거나 공중에 뜨면 일어선다.
    /// 앉으면 충돌체 높이가 줄어들고 발 위치는 그대로 유지된다.
    /// 빠르게 달리는 중에 S를 누르면 앉기 대신 슬라이드(PlayerSlide)가 시작된다.
    /// 슬라이드 중에도 낮은 자세(IsCrouching)로 취급하고, 일어서는 시점은 PlayerSlide가 정한다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(PlayerMovement), typeof(PlayerInputReader))]
    public class PlayerCrouch : MonoBehaviour
    {
        [Tooltip("일어설 때 머리 위를 막는 레이어")]
        [SerializeField] LayerMask obstacleLayers;
        [Tooltip("임시 시각 표현. 앉으면 높이를 줄인다 (아트 적용 후 애니메이션으로 대체)")]
        [SerializeField] Transform visual;

        BoxCollider2D box;
        PlayerMovement movement;
        PlayerInputReader input;
        PlayerSlide slide;

        Vector2 standingSize;
        Vector2 standingOffset;
        Vector3 visualStandingScale;
        Vector3 visualStandingPosition;

        public bool IsCrouching { get; private set; }

        void Reset()
        {
            obstacleLayers = LayerMask.GetMask("Ground");
        }

        void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            movement = GetComponent<PlayerMovement>();
            input = GetComponent<PlayerInputReader>();
            slide = GetComponent<PlayerSlide>();

            standingSize = box.size;
            standingOffset = box.offset;
            if (visual != null)
            {
                visualStandingScale = visual.localScale;
                visualStandingPosition = visual.localPosition;
            }
        }

        void Update()
        {
            if (slide != null && slide.IsSliding) return;

            // 제자리 조준 중에는 W·S·좌우가 조준에 쓰이므로 앉거나 일어서지 않는다
            if (input.AimLockHeld) return;

            if (!IsCrouching)
            {
                if (input.DownPressedThisFrame && movement.IsGrounded)
                {
                    bool sliding = slide != null && slide.TryStart();
                    EnterLowPosture(sliding ? movement.Settings.SlideHeight : movement.Settings.CrouchHeight);
                }
                return;
            }

            // 앉은 상태에서 S를 한 번 더 누르면 모프볼 (M4에서 구현)
            bool wantsToStand = input.UpPressedThisFrame
                || input.HorizontalPressedThisFrame
                || input.JumpPressedThisFrame
                || !movement.IsGrounded;

            if (wantsToStand)
                TryStand();
        }

        void EnterLowPosture(float height)
        {
            IsCrouching = true;
            ApplyHeight(height);
        }

        /// <summary>머리 위가 비어 있으면 일어선다. 일어섰으면 true.</summary>
        public bool TryStand()
        {
            if (!IsCrouching) return true;
            if (!HasHeadroom()) return false;

            IsCrouching = false;
            ApplyHeight(standingSize.y);
            return true;
        }

        void ApplyHeight(float height)
        {
            // 발 위치를 고정한 채 높이만 바꾼다
            float shrink = standingSize.y - height;
            box.size = new Vector2(standingSize.x, height);
            box.offset = standingOffset + Vector2.down * (shrink * 0.5f);

            if (visual != null)
            {
                float ratio = height / standingSize.y;
                visual.localScale = new Vector3(visualStandingScale.x, visualStandingScale.y * ratio, visualStandingScale.z);
                visual.localPosition = visualStandingPosition + Vector3.down * (shrink * 0.5f);
            }
        }

        bool HasHeadroom()
        {
            GetHeadroomArea(out Vector2 center, out Vector2 size);
            return !Physics2D.OverlapBox(center, size, 0f, obstacleLayers);
        }

        // 앉은 머리 위 ~ 선 머리 위 사이의 영역
        void GetHeadroomArea(out Vector2 center, out Vector2 size)
        {
            Vector2 position = transform.position;
            float feetY = position.y + standingOffset.y - standingSize.y * 0.5f;
            float crouchTop = feetY + box.size.y;
            float standTop = feetY + standingSize.y;

            center = new Vector2(position.x + standingOffset.x, (crouchTop + standTop) * 0.5f);
            size = new Vector2(standingSize.x * 0.9f, Mathf.Max(standTop - crouchTop - 0.02f, 0.01f));
        }

        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !IsCrouching) return;

            GetHeadroomArea(out Vector2 center, out Vector2 size);
            Gizmos.color = HasHeadroom() ? Color.cyan : Color.magenta;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
