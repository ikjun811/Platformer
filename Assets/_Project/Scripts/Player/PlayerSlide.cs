using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 슬라이드 (드레드 방식).
    /// 빠르게 달리는 중에 S를 누르면 낮은 자세로 일정 시간 같은 속도로 미끄러진다.
    /// 시간이 끝나도 머리 위가 막혀 있으면 일어설 수 있을 때까지 계속 미끄러진다 (높이 2타일 통로 통과).
    /// 시작 요청과 충돌체 높이 변경은 PlayerCrouch가 하고, 이 컴포넌트는 속도와 종료 시점을 맡는다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerMovement), typeof(PlayerCrouch))]
    public class PlayerSlide : MonoBehaviour
    {
        // 이 속도보다 느리면 벽에 막힌 것으로 본다 (타일/초)
        const float BlockedSpeed = 0.1f;

        Rigidbody2D body;
        PlayerMovement movement;
        PlayerCrouch crouch;
        PlayerInputReader input;

        float endTime;

        public bool IsSliding { get; private set; }

        /// <summary>미끄러지는 방향: 1(오른쪽), -1(왼쪽)</summary>
        public int Direction { get; private set; } = 1;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            movement = GetComponent<PlayerMovement>();
            crouch = GetComponent<PlayerCrouch>();
            input = GetComponent<PlayerInputReader>();
        }

        /// <summary>달리는 속도와 방향이 조건을 만족하면 슬라이드를 시작한다. 시작했으면 true.</summary>
        public bool TryStart()
        {
            if (!movement.IsGrounded || input.Horizontal == 0) return false;

            // 누르고 있는 방향으로 충분히 빠르게 달리고 있어야 한다
            float speed = body.linearVelocity.x * input.Horizontal;
            if (speed < movement.Settings.SlideMinStartSpeed) return false;

            Direction = input.Horizontal;
            endTime = Time.time + movement.Settings.SlideDuration;
            IsSliding = true;
            return true;
        }

        void Update()
        {
            if (!IsSliding) return;

            // 시간이 끝났거나 점프를 누르면 일어서기를 시도한다. 머리 위가 막혀 있으면 계속 미끄러진다
            // (점프는 입력 버퍼에 남아 있다가 일어선 뒤 PlayerMovement가 처리한다)
            bool wantsToEnd = Time.time >= endTime || input.JumpPressedThisFrame;
            if (wantsToEnd && crouch.TryStand())
                IsSliding = false;
        }

        void FixedUpdate()
        {
            if (!IsSliding) return;

            Vector2 velocity = body.linearVelocity;

            // 낮은 통로 안에서 벽에 막혔을 때 반대 방향을 누르면 되돌아 나온다
            if (Mathf.Abs(velocity.x) < BlockedSpeed && input.Horizontal == -Direction)
                Direction = -Direction;

            velocity.x = Direction * movement.Settings.SlideSpeed;
            body.linearVelocity = velocity;
        }
    }
}
