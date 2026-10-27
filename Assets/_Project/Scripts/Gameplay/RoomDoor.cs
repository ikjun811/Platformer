using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 방의 출입구 차단 (GDD 5.2). 닫히면 충돌체와 벽 블록 그림을 켜서 지나갈 수 없게 하고, 열리면 둘 다 끈다.
    /// 충돌체는 지형(Ground) 레이어라 플레이어·적·탄 모두 막힌다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class RoomDoor : MonoBehaviour
    {
        [Tooltip("닫혀 있을 때만 보이는 그림")]
        [SerializeField] SpriteRenderer[] closedSprites;
        [Tooltip("시작할 때 열려 있는지")]
        [SerializeField] bool startOpen = true;

        Collider2D blocker;

        public bool IsOpen { get; private set; }

        void Awake()
        {
            blocker = GetComponent<Collider2D>();
            SetOpen(startOpen);
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            blocker.enabled = !open;
            foreach (SpriteRenderer sprite in closedSprites)
            {
                if (sprite != null) sprite.enabled = !open;
            }
        }
    }
}
