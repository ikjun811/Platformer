using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 시트 프레임을 한 번 재생하고 사라지는 이펙트 (폭발 등). 코드로 만든 직후 Play를 호출한다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class OneShotEffect : MonoBehaviour
    {
        SpriteRenderer spriteRenderer;
        Sprite[] frames;
        float framesPerSecond;
        float startTime;

        public void Play(Sprite[] effectFrames, float fps)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            frames = effectFrames;
            framesPerSecond = Mathf.Max(fps, 1f);
            startTime = Time.time;
            if (frames.Length > 0)
                spriteRenderer.sprite = frames[0];
        }

        void Update()
        {
            if (frames == null || frames.Length == 0)
            {
                Destroy(gameObject);
                return;
            }

            int index = Mathf.FloorToInt((Time.time - startTime) * framesPerSecond);
            if (index >= frames.Length)
            {
                Destroy(gameObject);
                return;
            }
            spriteRenderer.sprite = frames[index];
        }
    }
}
