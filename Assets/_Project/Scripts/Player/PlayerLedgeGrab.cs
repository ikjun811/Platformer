using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 난간 잡기 (퓨전 방식).
    /// 공중에서 벽 쪽 방향키를 누른 채 떨어지다가 손 높이에 모서리가 오면 매달린다.
    /// 매달린 상태: W/벽 쪽 키 = 기어오르기, Space = 위로 점프, S/반대쪽 키 = 놓기.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerLedgeGrab : MonoBehaviour
    {
        enum State { None, Hanging, Climbing }

        const float Skin = 0.02f;

        [Tooltip("잡을 수 있는 모서리 레이어")]
        [SerializeField] LayerMask ledgeLayers;

        Rigidbody2D body;
        BoxCollider2D box;
        PlayerMovement movement;
        PlayerInputReader input;
        PlayerCrouch crouch;

        State state;
        int ledgeDirection;
        Vector2 climbTarget;
        float climbEndTime;
        float regrabAllowedTime;

        public bool IsHanging => state == State.Hanging;
        public bool IsClimbing => state == State.Climbing;

        void Reset()
        {
            ledgeLayers = LayerMask.GetMask("Ground");
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            movement = GetComponent<PlayerMovement>();
            input = GetComponent<PlayerInputReader>();
            crouch = GetComponent<PlayerCrouch>();
        }

        void Update()
        {
            if (state != State.Hanging) return;

            bool towardWall = input.HorizontalPressedThisFrame && input.Horizontal == ledgeDirection;
            bool awayFromWall = input.HorizontalPressedThisFrame && input.Horizontal == -ledgeDirection;

            if (input.JumpPressedThisFrame)
            {
                Release();
                movement.ForceJump();
            }
            else if (input.UpPressedThisFrame || towardWall)
            {
                StartClimb();
            }
            else if (input.DownPressedThisFrame || awayFromWall)
            {
                Release();
            }
        }

        void FixedUpdate()
        {
            switch (state)
            {
                case State.None:
                    TryGrab();
                    break;

                case State.Hanging:
                    body.linearVelocity = Vector2.zero;
                    break;

                case State.Climbing:
                    body.linearVelocity = Vector2.zero;
                    if (Time.time >= climbEndTime)
                        FinishClimb();
                    break;
            }
        }

        void TryGrab()
        {
            if (movement.IsGrounded || Time.time < regrabAllowedTime) return;
            if (crouch != null && crouch.IsCrouching) return;
            if (body.linearVelocity.y > 0f) return;

            // 벽 쪽 방향키를 누르고 있을 때만 잡는다
            int direction = input.Horizontal;
            if (direction == 0) return;

            if (FindLedge(direction, out float wallX, out float ledgeTop))
                Grab(direction, wallX, ledgeTop);
        }

        bool FindLedge(int direction, out float wallX, out float ledgeTop)
        {
            wallX = 0f;
            ledgeTop = 0f;

            Bounds b = box.bounds;
            float grabZone = movement.Settings.LedgeGrabZone;
            float reach = b.extents.x + 0.1f;

            // 1) 손이 닿는 범위의 아래쪽 높이에 벽이 있는가
            Vector2 wallOrigin = new(b.center.x, b.max.y - grabZone);
            RaycastHit2D wallHit = Physics2D.Raycast(wallOrigin, Vector2.right * direction, reach, ledgeLayers);
            if (!wallHit) return false;
            wallX = wallHit.point.x;

            // 2) 벽 안쪽을 머리 위에서 아래로 훑어 모서리 윗면을 찾는다.
            //    시작점부터 막혀 있으면(거리 0) 벽이 머리 위까지 이어진 것이므로 모서리가 아니다
            Vector2 topOrigin = new(wallX + direction * 0.05f, b.max.y + 0.05f);
            RaycastHit2D topHit = Physics2D.Raycast(topOrigin, Vector2.down, grabZone + 0.1f, ledgeLayers);
            if (!topHit || topHit.distance < 0.01f || topHit.normal.y < 0.7f) return false;
            ledgeTop = topHit.point.y;

            // 3) 모서리 위에 서 있을 공간이 있는가
            Vector2 size = box.size;
            Vector2 standCenter = new(wallX + direction * (size.x * 0.5f + 0.05f), ledgeTop + size.y * 0.5f + 0.05f);
            Vector2 standSize = new(size.x * 0.9f, size.y - 0.1f);
            return !Physics2D.OverlapBox(standCenter, standSize, 0f, ledgeLayers);
        }

        void Grab(int direction, float wallX, float ledgeTop)
        {
            state = State.Hanging;
            ledgeDirection = direction;

            movement.ControlLocked = true;
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;

            // 몸 옆면을 벽에, 머리 끝을 모서리 높이에 맞춘다
            Vector2 size = box.size;
            Vector2 offset = box.offset;
            float x = wallX - direction * (size.x * 0.5f + Skin) - offset.x;
            float y = ledgeTop - (size.y * 0.5f + offset.y);
            body.position = new Vector2(x, y);

            // 기어오른 뒤 설 위치: 모서리 위, 벽 끝에서 몸 절반만큼 안쪽
            climbTarget = new Vector2(
                wallX + direction * (size.x * 0.5f + 0.1f) - offset.x,
                ledgeTop + size.y * 0.5f - offset.y + Skin);
        }

        void StartClimb()
        {
            state = State.Climbing;
            climbEndTime = Time.time + movement.Settings.LedgeClimbDuration;
        }

        void FinishClimb()
        {
            // 임시: 순간 이동. 아트 적용 후 기어오르기 애니메이션으로 대체
            body.position = climbTarget;
            body.linearVelocity = Vector2.zero;
            state = State.None;
            movement.ControlLocked = false;
        }

        void Release()
        {
            state = State.None;
            movement.ControlLocked = false;
            regrabAllowedTime = Time.time + movement.Settings.LedgeRegrabDelay;
        }

        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || state == State.None) return;

            Gizmos.color = state == State.Hanging ? Color.yellow : Color.green;
            Gizmos.DrawWireCube(climbTarget + box.offset, box.size);
        }
    }
}
