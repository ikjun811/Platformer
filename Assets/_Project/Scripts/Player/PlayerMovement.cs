using System.Collections.Generic;
using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 플레이어 달리기와 점프.
    /// 가감속, 코요테 타임, 점프 입력 버퍼, 가변 점프 높이, 빠른 낙하를 지원한다.
    /// 계단 같은 낮은 턱(MaxStepHeight 이하)은 점프 없이 걸어서 오르내린다.
    /// 얇은 발판(OneWay) 위에서 아래 + 점프를 누르면 발판을 잠시 무시하고 아래로 떨어진다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlayerInputReader))]
    public class PlayerMovement : MonoBehaviour
    {
        const float StepSkin = StepProbe.Skin;
        // 턱을 찾을 때 이동 거리에 더해 앞을 살피는 거리 (유닛). 충돌체 사이 틈(약 0.02)보다 커야 한다
        const float StepLookAhead = 0.06f;

        [SerializeField] PlayerMovementSettings settings;
        [Tooltip("밟고 설 수 있는 레이어")]
        [SerializeField] LayerMask groundLayers;
        [Tooltip("발밑 바닥 감지 두께 (유닛)")]
        [SerializeField] float groundCheckDistance = 0.05f;

        Rigidbody2D body;
        BoxCollider2D box;
        PlayerInputReader input;
        // 턱 오르내리기는 단단한 지형만 대상으로 한다 (얇은 발판 제외)
        int stepLayers;
        int oneWayLayers;
        ContactFilter2D groundFilter;
        readonly List<Collider2D> groundHits = new();

        // 내려가는 중인 발판. 무시 시간이 끝나면 다시 충돌하게 한다
        Collider2D droppedPlatform;
        float dropThroughEndTime;

        float lastGroundedTime = float.NegativeInfinity;
        bool isJumping;
        float stunEndTime;

        public bool IsGrounded { get; private set; }

        /// <summary>바라보는 방향: 1(오른쪽), -1(왼쪽)</summary>
        public int FacingDirection { get; private set; } = 1;

        public Vector2 Velocity => body.linearVelocity;

        public PlayerMovementSettings Settings => settings;

        /// <summary>단단한 지형 레이어 (밟을 수 있는 레이어에서 얇은 발판을 뺀 것). 턱 판정에 쓴다</summary>
        public int SolidLayers => stepLayers;

        /// <summary>
        /// true인 동안 달리기·점프·중력 처리를 멈춘다 (대시 등 다른 컴포넌트가 몸을 제어할 때).
        /// </summary>
        public bool ControlLocked { get; set; }

        /// <summary>피격 경직 중인지. 경직 중에는 달리기·점프 입력을 받지 않지만 중력과 착지는 그대로 처리한다</summary>
        public bool IsStunned => Time.time < stunEndTime;

        void Reset()
        {
            groundLayers = LayerMask.GetMask("Ground", "OneWay");
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            input = GetComponent<PlayerInputReader>();
            oneWayLayers = LayerMask.GetMask("OneWay");
            stepLayers = groundLayers & ~oneWayLayers;
            groundFilter = new ContactFilter2D { useLayerMask = true, layerMask = groundLayers, useTriggers = false };

            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        void Update()
        {
            if (input.Horizontal != 0)
                FacingDirection = input.Horizontal;
        }

        void FixedUpdate()
        {
            if (ControlLocked)
            {
                IsGrounded = false;
                return;
            }

            RestoreDroppedPlatform();
            UpdateGrounded();
            // 경직 중에는 넉백이 수평 속도를 정한다
            if (!IsStunned)
            {
                Run();
                TryStepUp();
            }
            if (!IsStunned)
                TryJump();
            ApplyGravity();
        }

        void UpdateGrounded()
        {
            bool wasGrounded = IsGrounded;
            // 상승 중에는 착지로 보지 않는다 (점프 직후 바로 다시 착지 판정되는 것 방지)
            IsGrounded = body.linearVelocity.y <= 0.01f && CheckGround();

            // 걷다가 낮은 턱을 내려가는 순간에는 떨어지지 않고 아래 단에 붙는다
            if (!IsGrounded && wasGrounded && !isJumping)
                IsGrounded = TrySnapDown();

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

            // 내려가는 중인 발판은 바닥으로 치지 않는다
            int count = Physics2D.OverlapBox(center, size, 0f, groundFilter, groundHits);
            for (int i = 0; i < count; i++)
            {
                if (groundHits[i] != droppedPlatform) return true;
            }
            return false;
        }

        // 발밑이 얇은 발판이면 그 발판과의 충돌을 잠시 끄고 떨어진다. 떨어졌으면 true
        bool TryDropThrough()
        {
            if (!IsGrounded) return false;

            Bounds b = box.bounds;
            Vector2 center = new(b.center.x, b.min.y - groundCheckDistance * 0.5f);
            Vector2 size = new(b.size.x * 0.9f, groundCheckDistance);
            Collider2D platform = Physics2D.OverlapBox(center, size, 0f, oneWayLayers);
            if (platform == null) return false;

            RestoreDroppedPlatform(force: true);
            Physics2D.IgnoreCollision(box, platform, true);
            droppedPlatform = platform;
            dropThroughEndTime = Time.time + settings.DropThroughTime;

            IsGrounded = false;
            lastGroundedTime = float.NegativeInfinity;
            return true;
        }

        void RestoreDroppedPlatform(bool force = false)
        {
            if (droppedPlatform == null || (!force && Time.time < dropThroughEndTime)) return;

            Physics2D.IgnoreCollision(box, droppedPlatform, false);
            droppedPlatform = null;
        }

        void Run()
        {
            int direction = input.Horizontal;
            float targetSpeed = direction * settings.MaxSpeed;
            bool accelerating = direction != 0;

            float rate = IsGrounded
                ? (accelerating ? settings.GroundAcceleration : settings.GroundDeceleration)
                : (accelerating ? settings.AirAcceleration : settings.AirDeceleration);

            Vector2 velocity = body.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, rate * Time.fixedDeltaTime);
            body.linearVelocity = velocity;
        }

        // 앞쪽 발목 높이가 막혀 있고 한 단 위는 비어 있으면, 몸을 턱 위로 옮겨 그대로 걸어가게 한다
        void TryStepUp()
        {
            int direction = input.Horizontal;
            if (!IsGrounded || direction == 0) return;

            Bounds b = box.bounds;
            float step = settings.MaxStepHeight;
            // 이번 물리 프레임에 걸어갈 거리
            float travel = Mathf.Abs(body.linearVelocity.x) * Time.fixedDeltaTime;
            // 물리 엔진은 충돌체끼리 contact offset만큼 띄워 세우므로, 턱 앞에 멈춰 있어도 찾을 수 있게 넉넉히 살핀다
            float distance = travel + StepLookAhead;
            if (!StepProbe.Find(b, direction, distance, step, stepLayers, out float faceDistance, out float rise)) return;

            // 달리는 중에 이번 프레임에 닿지 않을 턱은 다음 프레임에 처리한다 (미리 올리면 닿기 전에 다시 떨어진다)
            if (faceDistance > travel && travel >= StepLookAhead) return;

            // 턱 높이만큼 올린다. 이번 프레임 이동으로 턱 위에 닿지 못하는 거리(멈춰 있다 출발할 때)만 앞으로 옮겨 준다.
            // 올린 뒤 한 프레임 동안 중력으로 내려가는 만큼을 StepSkin 여유로 둔다
            float advance = Mathf.Max(0f, faceDistance + StepSkin - travel);
            body.position += new Vector2(direction * advance, rise + StepSkin);
            Vector2 velocity = body.linearVelocity;
            velocity.y = 0f;
            body.linearVelocity = velocity;
        }

        // 발밑 바로 아래 턱 높이 안에 바닥이 있으면 그 위로 붙인다. 붙었으면 true
        bool TrySnapDown()
        {
            if (body.linearVelocity.y > 0.01f) return false;

            Bounds b = box.bounds;
            var origin = new Vector2(b.center.x, b.min.y + StepSkin * 0.5f);
            var size = new Vector2(b.size.x * 0.9f, StepSkin);
            RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, settings.MaxStepHeight + StepSkin, stepLayers);
            if (!hit || hit.normal.y < 0.7f) return false;

            float drop = hit.distance - StepSkin * 0.5f;
            if (drop > 0f)
                body.position += Vector2.down * drop;
            return true;
        }

        void TryJump()
        {
            bool jumpBuffered = Time.time - input.LastJumpPressedTime <= settings.JumpBufferTime;
            bool canJump = Time.time - lastGroundedTime <= settings.CoyoteTime;
            if (!jumpBuffered || !canJump) return;

            // 아래 + 점프: 얇은 발판 위면 내려가고, 단단한 바닥이면 평소처럼 점프한다
            if (input.Vertical < 0 && TryDropThrough())
            {
                input.ConsumeJump();
                return;
            }

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
