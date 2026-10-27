using System;
using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 플레이어 스프라이트 애니메이션.
    /// 프레임을 가로로 이어붙인 시트(Tools/ArtPipeline/convert_marsh.py 결과)를 플레이 시작 때 프레임으로 자르고,
    /// 매 프레임 플레이어 상태에 맞는 그림을 SpriteRenderer에 넣는다.
    /// 피벗은 서 있을 때 충돌체의 바닥 중앙이고, 왼쪽을 볼 때는 좌우 반전한다.
    /// Animator Controller 대신 코드로 상태를 고른다 (상태 판단을 이동 컴포넌트와 같은 기준으로 한곳에서 하기 위해).
    /// </summary>
    [RequireComponent(typeof(PlayerMovement), typeof(BoxCollider2D))]
    public class PlayerAnimator : MonoBehaviour
    {
        // GDD v2 8장: 1타일 = 64px
        const float PixelsPerUnit = 64f;

        [Serializable]
        class Clip
        {
            [Tooltip("프레임을 가로로 이어붙인 시트")]
            [SerializeField] Texture2D sheet;
            [Tooltip("프레임 한 칸의 가로 크기 (px). 세로는 시트 높이")]
            [SerializeField] int cellWidth = 384;
            [Tooltip("칸 안에서 피벗(충돌체 바닥 중앙)의 x 위치 (px)")]
            [SerializeField] int pivotX = 192;
            [Tooltip("초당 프레임 수 (반복 재생하는 동작만 사용)")]
            [SerializeField] float framesPerSecond = 10f;

            public Sprite[] Frames { get; private set; } = Array.Empty<Sprite>();
            public float FramesPerSecond => framesPerSecond;

            public void Build()
            {
                if (sheet == null || cellWidth <= 0) return;

                int count = sheet.width / cellWidth;
                var pivot = new Vector2((float)pivotX / cellWidth, 0f);
                Frames = new Sprite[count];
                for (int i = 0; i < count; i++)
                {
                    var rect = new Rect(i * cellWidth, 0, cellWidth, sheet.height);
                    Frames[i] = Sprite.Create(sheet, rect, pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
                    Frames[i].name = $"{sheet.name}_{i}";
                }
            }
        }

        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Clip idle = new();
        [SerializeField] Clip run = new();
        [Tooltip("0 상승, 1 최고점, 2 낙하 시작, 3 낙하")]
        [SerializeField] Clip jump = new();

        [Tooltip("이 속도(타일/초)보다 빠르게 움직이면 달리기 동작")]
        [SerializeField] float runSpeedThreshold = 0.5f;
        [Tooltip("공중 프레임 기준 속도 (타일/초). 이보다 빠르게 오르면 상승, 느리면 최고점, 이 3배보다 빠르게 떨어지면 낙하")]
        [SerializeField] float jumpApexSpeed = 3f;

        PlayerMovement movement;

        Clip current;
        float clipTime;

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();

            foreach (Clip clip in new[] { idle, run, jump })
                clip.Build();

            // 스프라이트 피벗(발)이 충돌체 바닥에 오도록 한다
            var box = GetComponent<BoxCollider2D>();
            spriteRenderer.transform.localPosition = new Vector3(box.offset.x, box.offset.y - box.size.y * 0.5f, 0f);
        }

        void LateUpdate()
        {
            Clip next = SelectState();
            if (next != current)
            {
                current = next;
                clipTime = 0f;
            }
            else
            {
                clipTime += Time.deltaTime;
            }

            if (current.Frames.Length > 0)
                spriteRenderer.sprite = current.Frames[Mathf.Clamp(SelectFrame(), 0, current.Frames.Length - 1)];

            spriteRenderer.flipX = movement.FacingDirection < 0;
        }

        Clip SelectState()
        {
            if (!movement.IsGrounded) return jump;
            if (Mathf.Abs(movement.Velocity.x) > runSpeedThreshold) return run;
            return idle;
        }

        // 현재 동작(current)에서 보여줄 프레임 번호
        int SelectFrame()
        {
            if (current == jump)
            {
                float vy = movement.Velocity.y;
                if (vy > jumpApexSpeed) return 0;
                if (vy > -jumpApexSpeed) return 1;
                if (vy > -jumpApexSpeed * 3f) return 2;
                return 3;
            }

            int step = Mathf.FloorToInt(clipTime * current.FramesPerSecond);

            if (current == idle || current == run)
                return step % Mathf.Max(current.Frames.Length, 1);

            return 0;
        }
    }
}
