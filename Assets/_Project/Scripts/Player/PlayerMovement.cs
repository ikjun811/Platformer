using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 플레이어 달리기와 점프.
    /// 가감속, 코요테 타임, 점프 입력 버퍼, 가변 점프 높이, 빠른 낙하를 지원한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlayerInputReader))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] PlayerMovementSettings settings;
        [Tooltip("밟고 설 수 있는 레이어")]
        [SerializeField] LayerMask groundLayers;
        [Tooltip("발밑 바닥 감지 두께 (유닛)")]
        [SerializeField] float groundCheckDistance = 0.05f;

        Rigidbody2D body;
        BoxCollider2D box;
        PlayerInputReader input;
        PlayerCrouch crouch;
        PlayerSlide slide;
        PlayerAim aim;

        float lastGroundedTime = float.NegativeInfinity;
        bool isJumping;
        float stunEndTime;

        public bool IsGrounded { get; private set; }

        /// <summary>바라보는 방향: 1(오른쪽), -1(왼쪽)</summary>
        public int FacingDirection { get; private set; } = 1;

        public Vector2 Velocity => body.linearVelocity;

        public PlayerMovementSettings Settings => settings;

        /// <summary>
        /// true인 동안 달리기·점프·중력 처리를 멈춘다 (난간 매달리기 등 다른 컴포넌트가 몸을 제어할 때).
        /// </summary>
        public bool ControlLocked { get; set; }

        /// <summary>피격 경직 중인지. 경직 중에는 달리기·점프 입력을 받지 않지만 중력과 착지는 그대로 처리한다</summary>
        public bool IsStunned => Time.time < stunEndTime;

        bool IsCrouching => crouch != null && crouch.IsCrouching;
        bool IsSliding => slide != null && slide.IsSliding;
        bool IsAimLocked => aim != null && aim.IsAimLocked;

        void Reset()
        {
            groundLayers = LayerMask.GetMask("Ground", "OneWay");
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            input = GetComponent<PlayerInputReader>();
            crouch = GetComponent<PlayerCrouch>();
            slide = GetComponent<PlayerSlide>();
            aim = GetComponent<PlayerAim>();

            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        void Update()
        {
            if (IsSliding)
                FacingDirection = slide.Direction;
            else if (input.Horizontal != 0)
                FacingDirection = input.Horizontal;
        }

        void FixedUpdate()
        {
            if (ControlLocked)
            {
                IsGrounded = false;
                return;
            }

            UpdateGrounded();
            // 슬라이드 중에는 PlayerSlide가, 경직 중에는 넉백이 수평 속도를 정한다
            if (!IsSliding && !IsStunned)
                Run();
            if (!IsStunned)
                TryJump();
            ApplyGravity();
        }

        void UpdateGrounded()
        {
            // 상승 중에는 착지로 보지 않는다 (점프 직후 바로 다시 착지 판정되는 것 방지)
            IsGrounded = body.linearVelocity.y <= 0.01f && CheckGround();

            if (IsGrounded)
            {
                lastGroundedTime = Time.time;
                isJumping = false;
            }
        }

        bool CheckGround()
        {
            Bounds b = box.bounds;
            Vector2 center = new(b.center.x, b.min.y - groundCheckDistance * 0.5f);
            Vector2 size = new(b.size.x * 0.9f, groundCheckDistance);
            return Physics2D.OverlapBox(center, size, 0f, groundLayers);
        }

        void Run()
        {
            // 앉아 있거나 땅에서 제자리 조준 중이면 수평 이동하지 않는다 (방향 전환만 한다)
            bool rooted = IsCrouching || (IsAimLocked && IsGrounded);
            int direction = rooted ? 0 : input.Horizontal;
            float targetSpeed = direction * settings.MaxSpeed;
            bool accelerating = direction != 0;

            float rate = IsGrounded
                ? (accelerating ? settings.GroundAcceleration : settings.GroundDeceleration)
                : (accelerating ? settings.AirAcceleration : settings.AirDeceleration);

            Vector2 velocity = body.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, rate * Time.fixedDeltaTime);
            body.linearVelocity = velocity;
        }

        void TryJump()
        {
            bool jumpBuffered = Time.time - input.LastJumpPressedTime <= settings.JumpBufferTime;
            bool canJump = Time.time - lastGroundedTime <= settings.CoyoteTime;
            if (!jumpBuffered || !canJump) return;

            // 앉은 채(슬라이드 포함) 머리 위가 막혀 일어서지 못했다면 점프하지 않는다
            if (IsCrouching) return;

            Jump();
        }

        /// <summary>일정 시간 달리기·점프 입력을 막는다.</summary>
        public void Stun(float duration)
        {
            stunEndTime = Mathf.Max(stunEndTime, Time.time + duration);
        }

        /// <summary>넉백: 속도를 바꾸고 경직시킨다. 공중으로 띄우므로 코요테 점프와 가변 점프는 끊는다.</summary>
        public void ApplyKnockback(Vector2 velocity, float stunDuration)
        {
            body.linearVelocity = velocity;
            isJumping = false;
            IsGrounded = false;
            lastGroundedTime = float.NegativeInfinity;
            Stun(stunDuration);
        }

        /// <summary>땅에 있지 않아도 즉시 점프한다 (난간에서 점프 등).</summary>
        public void ForceJump() => Jump();

        void Jump()
        {
            Vector2 velocity = body.linearVelocity;
            velocity.y = settings.JumpVelocity;
            body.linearVelocity = velocity;

            isJumping = true;
            IsGrounded = false;
            lastGroundedTime = float.NegativeInfinity;
            input.ConsumeJump();
        }

        void ApplyGravity()
        {
            Vector2 velocity = body.linearVelocity;

            float multiplier = 1f;
            if (velocity.y < 0f)
                multiplier = settings.FallGravityMultiplier;
            else if (isJumping && !input.JumpHeld)
                multiplier = settings.JumpCutGravityMultiplier;

            body.gravityScale = settings.Gravity / Mathf.Abs(Physics2D.gravity.y) * multiplier;

            if (velocity.y < -settings.MaxFallSpeed)
            {
                velocity.y = -settings.MaxFallSpeed;
                body.linearVelocity = velocity;
            }
        }

        void OnDrawGizmosSelected()
        {
            var collider = GetComponent<BoxCollider2D>();
            if (collider == null) return;

            Bounds b = collider.bounds;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(
                new Vector3(b.center.x, b.min.y - groundCheckDistance * 0.5f),
                new Vector3(b.size.x * 0.9f, groundCheckDistance));
        }
    }
}
