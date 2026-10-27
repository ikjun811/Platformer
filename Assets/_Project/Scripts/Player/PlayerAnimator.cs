using Platformer.Gameplay;
using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 플레이어 스프라이트 애니메이션.
    /// 시트(SheetClip)를 플레이 시작 때 프레임으로 자르고, 매 프레임 플레이어 상태에 맞는 그림을 SpriteRenderer에 넣는다.
    /// 피벗은 서 있을 때 충돌체의 바닥 중앙이고, 왼쪽을 볼 때는 좌우 반전한다.
    /// Animator Controller 대신 코드로 상태를 고른다 (상태 판단을 이동 컴포넌트와 같은 기준으로 한곳에서 하기 위해).
    /// </summary>
    [RequireComponent(typeof(PlayerMovement), typeof(BoxCollider2D))]
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] SheetClip idle = new();
        [SerializeField] SheetClip run = new();
        [Tooltip("공중에 있는 동안 보여줄 시트")]
        [SerializeField] SheetClip jump = new();
        [Tooltip("공중에 있는 동안 고정해서 보여줄 프레임 번호 (GDD 3.3: 달리기 시트의 run_3 = 2)")]
        [SerializeField] int jumpFrame = 2;

        [Tooltip("이 속도(타일/초)보다 빠르게 움직이면 달리기 동작")]
        [SerializeField] float runSpeedThreshold = 0.5f;

        PlayerMovement movement;

        SheetClip current;
        float clipTime;

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();

            foreach (SheetClip clip in new[] { idle, run, jump })
                clip.Build();

            // 스프라이트 피벗(발)이 충돌체 바닥에 오도록 한다
            var box = GetComponent<BoxCollider2D>();
            spriteRenderer.transform.localPosition = new Vector3(box.offset.x, box.offset.y - box.size.y * 0.5f, 0f);
        }

        void LateUpdate()
        {
            SheetClip next = SelectState();
            if (next != current)
            {
                current = next;
                clipTime = 0f;
            }
            else
            {
                clipTime += Time.deltaTime;
            }

            if (current.Length > 0)
                spriteRenderer.sprite = current.Frame(SelectFrame());

            spriteRenderer.flipX = movement.FacingDirection < 0;
        }

        SheetClip SelectState()
        {
            if (!movement.IsGrounded) return jump;
            if (Mathf.Abs(movement.Velocity.x) > runSpeedThreshold) return run;
            return idle;
        }

        // 현재 동작(current)에서 보여줄 프레임 번호
        int SelectFrame()
        {
            // 전용 점프 그림이 없어 한 프레임을 고정해서 보여준다
            if (current == jump)
                return jumpFrame;

            return current.FrameAt(clipTime, true);
        }
    }
}
