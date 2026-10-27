using UnityEngine;
using UnityEngine.InputSystem;

namespace Platformer.Player
{
    /// <summary>
    /// 프로젝트 전역 입력 액션(Player 맵)을 읽어 플레이어 컴포넌트들에 제공한다.
    /// 입력 읽기는 Update에서 하고, 물리 처리 쪽(FixedUpdate)은 이 값을 가져다 쓴다.
    /// 다른 플레이어 컴포넌트보다 먼저 실행되어 같은 프레임의 입력을 제공한다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        const float AxisThreshold = 0.5f;

        InputAction moveAction;
        InputAction jumpAction;
        InputAction fireAction;
        InputAction dashAction;
        InputAction swapWeaponAction;

        /// <summary>좌우 입력: -1, 0, 1</summary>
        public int Horizontal { get; private set; }

        /// <summary>상하 입력: -1(아래), 0, 1(위)</summary>
        public int Vertical { get; private set; }

        /// <summary>이번 프레임에 아래(S)를 새로 눌렀는지</summary>
        public bool DownPressedThisFrame { get; private set; }

        /// <summary>이번 프레임에 위(W)를 새로 눌렀는지</summary>
        public bool UpPressedThisFrame { get; private set; }

        /// <summary>이번 프레임에 좌우 입력이 새로 들어왔는지 (방향이 바뀐 경우 포함)</summary>
        public bool HorizontalPressedThisFrame { get; private set; }

        public bool JumpPressedThisFrame { get; private set; }

        public bool JumpHeld { get; private set; }

        /// <summary>이번 프레임에 사격(J)을 새로 눌렀는지</summary>
        public bool FirePressedThisFrame { get; private set; }

        public bool FireHeld { get; private set; }

        /// <summary>이번 프레임에 사격(J)을 뗐는지 (차지 빔 발사용)</summary>
        public bool FireReleasedThisFrame { get; private set; }

        /// <summary>이번 프레임에 페이즈 대시(L)를 새로 눌렀는지</summary>
        public bool DashPressedThisFrame { get; private set; }

        /// <summary>이번 프레임에 무기 모듈 교체(Q)를 새로 눌렀는지</summary>
        public bool SwapWeaponPressedThisFrame { get; private set; }

        /// <summary>점프 키를 마지막으로 누른 시각 (점프 입력 버퍼용)</summary>
        public float LastJumpPressedTime { get; private set; } = float.NegativeInfinity;

        void Awake()
        {
            moveAction = InputSystem.actions.FindAction("Player/Move", true);
            jumpAction = InputSystem.actions.FindAction("Player/Jump", true);
            fireAction = InputSystem.actions.FindAction("Player/Fire", true);
            dashAction = InputSystem.actions.FindAction("Player/Dash", true);
            swapWeaponAction = InputSystem.actions.FindAction("Player/SwapWeapon", true);
        }

        void OnEnable()
        {
            moveAction.Enable();
            jumpAction.Enable();
            fireAction.Enable();
            dashAction.Enable();
            swapWeaponAction.Enable();
        }

        void Update()
        {
            // WASD 대각선 입력은 정규화되어 0.7 정도가 되므로 -1/0/1로 변환한다
            Vector2 move = moveAction.ReadValue<Vector2>();
            int previousHorizontal = Horizontal;
            int previousVertical = Vertical;
            Horizontal = ToDigital(move.x);
            Vertical = ToDigital(move.y);

            DownPressedThisFrame = Vertical == -1 && previousVertical != -1;
            UpPressedThisFrame = Vertical == 1 && previousVertical != 1;
            HorizontalPressedThisFrame = Horizontal != 0 && Horizontal != previousHorizontal;

            JumpHeld = jumpAction.IsPressed();
            JumpPressedThisFrame = jumpAction.WasPressedThisFrame();
            if (JumpPressedThisFrame)
                LastJumpPressedTime = Time.time;

            FireHeld = fireAction.IsPressed();
            FirePressedThisFrame = fireAction.WasPressedThisFrame();
            FireReleasedThisFrame = fireAction.WasReleasedThisFrame();
            DashPressedThisFrame = dashAction.WasPressedThisFrame();
            SwapWeaponPressedThisFrame = swapWeaponAction.WasPressedThisFrame();
        }

        /// <summary>버퍼된 점프 입력을 사용 처리한다 (한 번 누름에 한 번만 점프).</summary>
        public void ConsumeJump() => LastJumpPressedTime = float.NegativeInfinity;

        static int ToDigital(float value)
        {
            if (value > AxisThreshold) return 1;
            if (value < -AxisThreshold) return -1;
            return 0;
        }
    }
}
