using UnityEngine;
using UnityEngine.InputSystem;

namespace Platformer.Player
{
    /// <summary>
    /// 프로젝트 전역 입력 액션(Player 맵)을 읽어 플레이어 컴포넌트들에 제공한다.
    /// 입력 읽기는 Update에서 하고, 물리 처리 쪽(FixedUpdate)은 이 값을 가져다 쓴다.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        const float AxisThreshold = 0.5f;

        InputAction moveAction;
        InputAction jumpAction;

        /// <summary>좌우 입력: -1, 0, 1</summary>
        public int Horizontal { get; private set; }

        /// <summary>상하 입력: -1(아래), 0, 1(위)</summary>
        public int Vertical { get; private set; }

        public bool JumpHeld { get; private set; }

        /// <summary>점프 키를 마지막으로 누른 시각 (점프 입력 버퍼용)</summary>
        public float LastJumpPressedTime { get; private set; } = float.NegativeInfinity;

        void Awake()
        {
            moveAction = InputSystem.actions.FindAction("Player/Move", true);
            jumpAction = InputSystem.actions.FindAction("Player/Jump", true);
        }

        void OnEnable()
        {
            moveAction.Enable();
            jumpAction.Enable();
        }

        void Update()
        {
            // WASD 대각선 입력은 정규화되어 0.7 정도가 되므로 -1/0/1로 변환한다
            Vector2 move = moveAction.ReadValue<Vector2>();
            Horizontal = ToDigital(move.x);
            Vertical = ToDigital(move.y);

            JumpHeld = jumpAction.IsPressed();
            if (jumpAction.WasPressedThisFrame())
                LastJumpPressedTime = Time.time;
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
