using Platformer.Player;
using UnityEngine;

namespace Platformer.UI
{
    /// <summary>
    /// 과열 게이지 임시 표시 (N5 HUD에서 교체한다). 플레이어 머리 위에 작은 막대로 그린다.
    /// - 평소: 열 비율만큼 채운다 (열이 0이면 숨김)
    /// - 과열: 붉은 막대 위에 배출 성공 구간(초록)과 움직이는 표시(흰색)를 보여준다. 실패하면 구간이 회색이 된다
    /// - 막대 왼쪽 작은 칸: 현재 무기 모듈 색 (항상 표시)
    /// </summary>
    public class PlayerHeatGauge : MonoBehaviour
    {
        [SerializeField] PlayerHeat heat;
        [Tooltip("현재 무기 모듈 색을 보여줄 사격 컴포넌트")]
        [SerializeField] PlayerShooter shooter;
        [Tooltip("막대를 그릴 단색 스프라이트 (임시 사각형)")]
        [SerializeField] Sprite barSprite;
        [Tooltip("막대 크기 (타일)")]
        [SerializeField] Vector2 size = new(0.6f, 0.06f);

        static readonly Color BackColor = new(0f, 0f, 0f, 0.6f);
        static readonly Color HeatColor = new(1f, 0.75f, 0.2f);
        static readonly Color OverheatColor = new(0.9f, 0.15f, 0.1f);
        static readonly Color WindowColor = new(0.3f, 1f, 0.4f);
        static readonly Color FailedWindowColor = new(0.5f, 0.5f, 0.5f);

        SpriteRenderer back;
        SpriteRenderer fill;
        SpriteRenderer window;
        SpriteRenderer marker;
        SpriteRenderer moduleIcon;
        float unitsPerSpriteX = 1f;
        float unitsPerSpriteY = 1f;

        void Awake()
        {
            if (barSprite != null)
            {
                unitsPerSpriteX = barSprite.bounds.size.x;
                unitsPerSpriteY = barSprite.bounds.size.y;
            }
            back = CreateBar("Back", 0);
            fill = CreateBar("Fill", 1);
            window = CreateBar("VentWindow", 2);
            marker = CreateBar("VentMarker", 3);
            moduleIcon = CreateBar("ModuleIcon", 1);
        }

        void LateUpdate()
        {
            UpdateModuleIcon();

            bool overheated = heat.IsOverheated;
            bool visible = overheated || heat.Heat > 0f;
            back.enabled = fill.enabled = visible;
            window.enabled = marker.enabled = overheated;
            if (!visible) return;

            SetBar(back, 0f, 1f, size.y, BackColor);
            if (!overheated)
            {
                SetBar(fill, 0f, heat.Ratio, size.y, HeatColor);
                return;
            }

            SetBar(fill, 0f, 1f, size.y, OverheatColor);
            SetBar(window, heat.VentWindowStart, heat.VentWindowEnd, size.y, heat.VentFailed ? FailedWindowColor : WindowColor);
            float p = heat.OverheatProgress;
            SetBar(marker, p - 0.015f, p + 0.015f, size.y * 2f, Color.white);
        }

        // 막대 왼쪽에 막대 높이의 두 배 크기 정사각형으로 현재 모듈 색을 보여준다
        void UpdateModuleIcon()
        {
            moduleIcon.enabled = shooter != null;
            if (!moduleIcon.enabled) return;

            float side = size.y * 2f;
            moduleIcon.transform.localPosition = new Vector3(-size.x * 0.5f - side, 0f, 0f);
            moduleIcon.transform.localScale = new Vector3(side / unitsPerSpriteX, side / unitsPerSpriteY, 1f);
            moduleIcon.color = shooter.CurrentModule.Color;
        }

        SpriteRenderer CreateBar(string barName, int order)
        {
            var go = new GameObject(barName);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = barSprite;
            renderer.sortingLayerName = "UI";
            renderer.sortingOrder = order;
            renderer.enabled = false;
            return renderer;
        }

        // 막대의 from~to 구간(0~1 비율)을 그린다. 이 오브젝트 위치가 막대 중앙이다
        void SetBar(SpriteRenderer bar, float from, float to, float height, Color color)
        {
            from = Mathf.Clamp01(from);
            to = Mathf.Clamp01(to);
            float width = (to - from) * size.x;
            float centerX = (from + to) * 0.5f * size.x - size.x * 0.5f;
            bar.transform.localPosition = new Vector3(centerX, 0f, 0f);
            bar.transform.localScale = new Vector3(width / unitsPerSpriteX, height / unitsPerSpriteY, 1f);
            bar.color = color;
        }
    }
}
