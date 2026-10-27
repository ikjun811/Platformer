using Platformer.Gameplay;
using Platformer.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Platformer.UI
{
    /// <summary>
    /// 전투 HUD (GDD N5, 시안 C 분산형). 640x360 캔버스 기준 픽셀 단위로 배치한다. 글자는 숫자만 쓴다.
    /// - 왼쪽 위: 큰 HP 숫자 + HP 막대, 그 아래 현재 무기 모듈 색 점
    /// - 오른쪽 위: 소탕 중 웨이브 "n/N" / 화면 가운데: 방 클리어 알림
    /// - 플레이어 머리 위 (따라다님): 열 막대(쌓였거나 과열일 때만, 과열 시 배출 구간·표시)와 대시 칸(하나라도 썼을 때만)
    /// 글자는 Righteous 크림색에 어두운 그림자. 색은 soft_retro 팔레트. 화면 요소는 Awake에서 코드로 만든다.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class GameHud : MonoBehaviour
    {
        // soft_retro 팔레트 (Curiosities)
        static readonly Color Cream = new Color32(0xFF, 0xEE, 0xCC, 0xFF);
        static readonly Color Teal = new Color32(0x3A, 0xB0, 0xC4, 0xFF);
        static readonly Color Red = new Color32(0xFF, 0x69, 0x73, 0xFF);
        static readonly Color Peach = new Color32(0xFF, 0xB0, 0xA3, 0xFF);
        static readonly Color Gray = new Color32(0x8A, 0x86, 0x9E, 0xFF);
        static readonly Color Shade = new Color32(0x1C, 0x1A, 0x28, 0xFF);
        static readonly Color BarBack = new Color32(0x1C, 0x1A, 0x28, 0xAA);
        static readonly Color EmptyPip = new Color32(0x50, 0x4C, 0x66, 0xFF);

        // 배치 (픽셀)
        const int BigFontSize = 22;
        const int HpBarX = 44;
        const int HpBarWidth = 90;
        const int HpBarHeight = 5;
        const int HeatWidth = 28;
        const int HeatHeight = 3;
        const int PipWidth = 6;
        const int PipGap = 2;
        const float ClearMessageTime = 2f;

        [Header("대상")]
        [SerializeField] Health playerHealth;
        [SerializeField] PlayerHeat heat;
        [SerializeField] PlayerDash dash;
        [SerializeField] PlayerShooter shooter;
        [Tooltip("웨이브를 보여줄 방 (없으면 표시하지 않는다)")]
        [SerializeField] Room room;
        [Tooltip("머리 위 표시를 플레이어 위치에서 이만큼 위에 둔다 (타일)")]
        [SerializeField] float overheadOffset = 0.45f;

        [Header("그림")]
        [SerializeField] Font font;
        [SerializeField] Sprite whiteSprite;
        [Tooltip("모듈 색 점")]
        [SerializeField] Sprite dotSprite;

        Canvas canvas;
        RectTransform canvasRect;
        Camera cam;

        Text hpText;
        RectTransform hpFill;
        Image moduleDot;
        Text waveText;
        Text clearText;
        float clearMessageEndTime;

        RectTransform overhead;
        GameObject heatGroup;
        Image heatFill;
        RectTransform heatFillRect;
        Image ventWindow;
        RectTransform ventWindowRect;
        RectTransform ventMarker;
        GameObject dashGroup;
        Image[] pips;

        void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvasRect = (RectTransform)transform;
            cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            BuildLayout();
        }

        void OnEnable()
        {
            if (room != null) room.Cleared += OnRoomCleared;
        }

        void OnDisable()
        {
            if (room != null) room.Cleared -= OnRoomCleared;
        }

        void LateUpdate()
        {
            UpdateHealth();
            UpdateModule();
            UpdateRoom();
            UpdateOverhead();
        }

        // ───────── 갱신 ─────────

        void UpdateHealth()
        {
            hpText.text = playerHealth.Current.ToString();
            float ratio = playerHealth.Max > 0 ? (float)playerHealth.Current / playerHealth.Max : 0f;
            SetWidth(hpFill, Mathf.RoundToInt(ratio * HpBarWidth));
        }

        void UpdateModule()
        {
            moduleDot.color = shooter.CurrentModule.Color;
        }

        void UpdateRoom()
        {
            bool fighting = room != null && room.CurrentState == Room.State.Fighting && room.CurrentWave >= 0;
            waveText.enabled = fighting;
            if (fighting)
                waveText.text = $"{room.CurrentWave + 1}/{room.WaveCount}";

            clearText.enabled = Time.time < clearMessageEndTime;
        }

        void UpdateOverhead()
        {
            // 플레이어 머리 위 월드 위치를 캔버스 픽셀 좌표로 옮긴다 (정수로 맞춰 픽셀이 흔들리지 않게)
            Vector3 world = playerHealth.transform.position + Vector3.up * overheadOffset;
            Vector2 screen = cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, canvas.worldCamera, out Vector2 local);
            overhead.anchoredPosition = new Vector2(Mathf.Round(local.x), Mathf.Round(local.y));

            UpdateHeat();
            UpdateDash();
        }

        void UpdateHeat()
        {
            bool overheated = heat.IsOverheated;
            heatGroup.SetActive(overheated || heat.Heat > 0f);
            ventWindow.enabled = overheated;
            ventMarker.gameObject.SetActive(overheated);

            if (!overheated)
            {
                heatFill.color = Peach;
                SetWidth(heatFillRect, Mathf.RoundToInt(heat.Ratio * HeatWidth));
                return;
            }

            heatFill.color = Red;
            SetWidth(heatFillRect, HeatWidth);

            int windowStart = Mathf.RoundToInt(heat.VentWindowStart * HeatWidth);
            int windowEnd = Mathf.RoundToInt(heat.VentWindowEnd * HeatWidth);
            ventWindowRect.anchoredPosition = new Vector2(-HeatWidth / 2 + windowStart, ventWindowRect.anchoredPosition.y);
            SetWidth(ventWindowRect, windowEnd - windowStart);
            ventWindow.color = heat.VentFailed ? Gray : Teal;

            int markerX = Mathf.RoundToInt(heat.OverheatProgress * HeatWidth);
            ventMarker.anchoredPosition = new Vector2(-HeatWidth / 2 + markerX - 1, ventMarker.anchoredPosition.y);
        }

        void UpdateDash()
        {
            int remaining = dash.RemainingCharges;
            dashGroup.SetActive(remaining < pips.Length);
            for (int i = 0; i < pips.Length; i++)
                pips[i].color = i < remaining ? Teal : EmptyPip;
        }

        void OnRoomCleared()
        {
            clearMessageEndTime = Time.time + ClearMessageTime;
        }

        // ───────── 배치 ─────────

        void BuildLayout()
        {
            // 왼쪽 위: HP 숫자와 막대, 모듈 점
            hpText = CreateText("Hp", canvasRect, new Vector2(10, -4), new Vector2(40, 26), BigFontSize, TextAnchor.UpperLeft);
            CreateImage("HpShadow", canvasRect, whiteSprite, Shade, new Vector2(HpBarX + 1, -15), new Vector2(HpBarWidth, HpBarHeight));
            CreateImage("HpBack", canvasRect, whiteSprite, BarBack, new Vector2(HpBarX, -14), new Vector2(HpBarWidth, HpBarHeight));
            hpFill = CreateImage("HpFill", canvasRect, whiteSprite, Red, new Vector2(HpBarX, -14), new Vector2(HpBarWidth, HpBarHeight)).rectTransform;
            CreateImage("ModuleShadow", canvasRect, dotSprite, Shade, new Vector2(HpBarX + 1, -24), new Vector2(7, 7));
            moduleDot = CreateImage("Module", canvasRect, dotSprite, Color.white, new Vector2(HpBarX, -23), new Vector2(7, 7));

            // 오른쪽 위: 웨이브, 가운데: 방 클리어
            waveText = CreateText("Wave", canvasRect, new Vector2(-10, -4), new Vector2(80, 26), BigFontSize, TextAnchor.UpperRight);
            SetAnchor(waveText.rectTransform, new Vector2(1f, 1f));
            clearText = CreateText("RoomClear", canvasRect, Vector2.zero, new Vector2(240, 30), BigFontSize, TextAnchor.MiddleCenter);
            clearText.text = "ROOM CLEAR";
            SetAnchor(clearText.rectTransform, new Vector2(0.5f, 0.5f));
            clearText.enabled = false;

            // 머리 위: 기준점(크기 0) 아래에 열 막대와 대시 칸. 자식 좌표는 기준점에서 위쪽이 양수
            var overheadObject = new GameObject("Overhead", typeof(RectTransform));
            overhead = (RectTransform)overheadObject.transform;
            overhead.SetParent(canvasRect, false);
            overhead.anchorMin = overhead.anchorMax = new Vector2(0.5f, 0.5f);
            overhead.sizeDelta = Vector2.zero;

            heatGroup = CreateGroup("Heat");
            CreateLocal("HeatShadow", heatGroup.transform, Shade, new Vector2(-HeatWidth / 2 + 1, 4), new Vector2(HeatWidth, HeatHeight));
            CreateLocal("HeatBack", heatGroup.transform, BarBack, new Vector2(-HeatWidth / 2, 5), new Vector2(HeatWidth, HeatHeight));
            heatFill = CreateLocal("HeatFill", heatGroup.transform, Peach, new Vector2(-HeatWidth / 2, 5), new Vector2(HeatWidth, HeatHeight));
            heatFillRect = heatFill.rectTransform;
            ventWindow = CreateLocal("VentWindow", heatGroup.transform, Teal, new Vector2(-HeatWidth / 2, 5), new Vector2(0, HeatHeight));
            ventWindowRect = ventWindow.rectTransform;
            ventMarker = CreateLocal("VentMarker", heatGroup.transform, Cream, new Vector2(-HeatWidth / 2, 4), new Vector2(2, HeatHeight + 2)).rectTransform;

            dashGroup = CreateGroup("Dash");
            pips = new Image[dash.MaxCharges];
            int totalWidth = pips.Length * PipWidth + (pips.Length - 1) * PipGap;
            for (int i = 0; i < pips.Length; i++)
                pips[i] = CreateLocal($"Pip{i}", dashGroup.transform, Teal, new Vector2(-totalWidth / 2 + i * (PipWidth + PipGap), 1), new Vector2(PipWidth, 2));
        }

        GameObject CreateGroup(string groupName)
        {
            var go = new GameObject(groupName, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(overhead, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            return go;
        }

        // 머리 위 기준점에서 왼쪽 아래 모서리 위치(위쪽 양수)로 놓는 단색 사각형
        Image CreateLocal(string objectName, Transform parent, Color color, Vector2 bottomLeft, Vector2 size)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = bottomLeft;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = whiteSprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        Image CreateImage(string objectName, RectTransform parent, Sprite sprite, Color color, Vector2 topLeft, Vector2 size)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            PlaceTopLeft(rect, parent, topLeft, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // 크림색 글자 + 어두운 그림자 (ROOM CLEAR와 같은 스타일)
        Text CreateText(string objectName, RectTransform parent, Vector2 topLeft, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            var rect = (RectTransform)go.transform;
            PlaceTopLeft(rect, parent, topLeft, size);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = Cream;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = Shade;
            shadow.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        // 부모의 왼쪽 위를 기준으로 픽셀 위치에 놓는다 (y는 아래로 갈수록 음수)
        static void PlaceTopLeft(RectTransform rect, RectTransform parent, Vector2 topLeft, Vector2 size)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
        }

        static void SetAnchor(RectTransform rect, Vector2 anchor)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
        }

        static void SetWidth(RectTransform rect, int width) =>
            rect.sizeDelta = new Vector2(Mathf.Max(0, width), rect.sizeDelta.y);
    }
}
