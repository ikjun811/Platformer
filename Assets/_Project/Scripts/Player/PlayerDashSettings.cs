using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 페이즈 대시 수치 (GDD 4.4). 단위는 유닛(= 타일)과 초.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerDashSettings", menuName = "Platformer/Player Dash Settings")]
    public class PlayerDashSettings : ScriptableObject
    {
        [Header("대시")]
        [Tooltip("대시 한 번에 이동하는 거리 (타일)")]
        [SerializeField] float distance = 1.95f;
        [Tooltip("대시 한 번에 걸리는 시간 (초)")]
        [SerializeField] float duration = 0.13f;
        [Tooltip("대시가 끝난 뒤에도 피해를 무시하는 시간 (초)")]
        [SerializeField] float evadeGrace = 0.05f;

        [Header("연속 사용")]
        [Tooltip("이어서 쓸 수 있는 최대 횟수")]
        [SerializeField] int maxChain = 3;
        [Tooltip("이 시간 안에 다시 누르면 연속 대시로 이어진다 (초)")]
        [SerializeField] float chainWindow = 0.4f;
        [Tooltip("연속 대시가 끝난 뒤 횟수가 다시 채워지기까지 걸리는 시간 (초)")]
        [SerializeField] float cooldown = 1.2f;
        [Tooltip("이 시간 안에 누른 대시 입력은 대시할 수 있게 되는 순간 실행한다 (초)")]
        [SerializeField] float inputBufferTime = 0.1f;

        [Header("잔상")]
        [Tooltip("잔상을 남기는 간격 (초)")]
        [SerializeField] float afterimageInterval = 0.025f;
        [Tooltip("잔상이 사라지기까지 걸리는 시간 (초)")]
        [SerializeField] float afterimageLifetime = 0.2f;
        [Tooltip("잔상 색 (알파 = 처음 투명도)")]
        [SerializeField] Color afterimageColor = new(0.55f, 0.85f, 1f, 0.6f);

        public float Distance => distance;
        public float Duration => duration;
        public float Speed => distance / Mathf.Max(duration, 0.01f);
        public float EvadeGrace => evadeGrace;
        public int MaxChain => maxChain;
        public float ChainWindow => chainWindow;
        public float Cooldown => cooldown;
        public float InputBufferTime => inputBufferTime;
        public float AfterimageInterval => afterimageInterval;
        public float AfterimageLifetime => afterimageLifetime;
        public Color AfterimageColor => afterimageColor;
    }
}
