using System;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 프레임을 가로로 이어붙인 시트(Tools/ArtPipeline/convert_marsh.py 결과)를 플레이 시작 때 프레임으로 잘라 쓰는 동작 하나.
    /// 흰 실루엣 시트(_White)가 있으면 같은 번호의 실루엣 프레임도 함께 만든다 (피격 번쩍임·공격 신호용).
    /// 피벗은 칸 바닥의 pivotX 위치 (발 중앙)다.
    /// </summary>
    [Serializable]
    public class SheetClip
    {
        // GDD v2 8장: 1타일 = 64px
        const float PixelsPerUnit = 64f;

        [Tooltip("프레임을 가로로 이어붙인 시트")]
        [SerializeField] Texture2D sheet;
        [Tooltip("같은 배치의 흰 실루엣 시트 (없어도 된다)")]
        [SerializeField] Texture2D whiteSheet;
        [Tooltip("프레임 한 칸의 가로 크기 (px). 세로는 시트 높이")]
        [SerializeField] int cellWidth = 64;
        [Tooltip("칸 안에서 피벗(발 중앙)의 x 위치 (px)")]
        [SerializeField] int pivotX = 32;
        [Tooltip("초당 프레임 수 (시간으로 재생할 때)")]
        [SerializeField] float framesPerSecond = 10f;

        public Sprite[] Frames { get; private set; } = Array.Empty<Sprite>();
        public Sprite[] WhiteFrames { get; private set; } = Array.Empty<Sprite>();
        public float FramesPerSecond => framesPerSecond;
        public int Length => Frames.Length;

        public void Build()
        {
            Frames = Slice(sheet);
            WhiteFrames = Slice(whiteSheet);
        }

        /// <summary>재생 시간에 맞는 프레임 번호. loop가 아니면 마지막 프레임에서 멈춘다.</summary>
        public int FrameAt(float time, bool loop)
        {
            if (Frames.Length == 0) return 0;
            int step = Mathf.FloorToInt(time * framesPerSecond);
            return loop ? step % Frames.Length : Mathf.Min(step, Frames.Length - 1);
        }

        public Sprite Frame(int index) => Frames.Length > 0 ? Frames[Mathf.Clamp(index, 0, Frames.Length - 1)] : null;

        public Sprite WhiteFrame(int index) =>
            WhiteFrames.Length > 0 ? WhiteFrames[Mathf.Clamp(index, 0, WhiteFrames.Length - 1)] : null;

        Sprite[] Slice(Texture2D texture)
        {
            if (texture == null || cellWidth <= 0) return Array.Empty<Sprite>();

            int count = texture.width / cellWidth;
            var pivot = new Vector2((float)pivotX / cellWidth, 0f);
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                var rect = new Rect(i * cellWidth, 0, cellWidth, texture.height);
                frames[i] = Sprite.Create(texture, rect, pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
                frames[i].name = $"{texture.name}_{i}";
            }
            return frames;
        }
    }
}
