using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 카운터와 카운터 처치 수치 (GDD 4.5). 단위는 유닛(= 타일)과 초.
    /// 카운터 판정 시간은 적의 신호 시간이다 (적 설정의 SignalTime).
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerCounterSettings", menuName = "Platformer/Player Counter Settings")]
    public class PlayerCounterSettings : ScriptableObject
    {
        [Header("카운터")]
        [Tooltip("몸 앞쪽으로 카운터가 닿는 거리 (타일)")]
        [SerializeField] float reach = 1f;
        [Tooltip("카운터 판정 높이 (타일, 몸 중앙 기준)")]
        [SerializeField] float height = 1f;
        [Tooltip("신호가 없을 때 누르면 사격·카운터를 못 하는 시간 (초)")]
        [SerializeField] float failRecovery = 0.4f;
        [Tooltip("성공하면 피해를 받지 않는 시간 (초). 바로 옆 다른 적의 공격을 막는다")]
        [SerializeField] float successEvade = 0.25f;

        [Header("카운터 경직")]
        [Tooltip("카운터에 당한 적이 경직되는 시간 (초). 이 동안 가까이서 쏘면 즉사")]
        [SerializeField] float staggerTime = 1.5f;
        [Tooltip("카운터에 당한 적이 밀려나는 속도 (x = 플레이어 반대쪽, y = 위)")]
        [SerializeField] Vector2 staggerPush = new(3f, 1.5f);

        [Header("카운터 처치")]
        [Tooltip("경직된 적이 몸 앞 이 거리 안에 있을 때 쏘면 즉사한다 (타일)")]
        [SerializeField] float killReach = 1.6f;
        [Tooltip("카운터 처치 때 내려가는 열")]
        [SerializeField] float killHeatReduction = 50f;
        [Tooltip("체력 조각 하나가 회복하는 체력")]
        [SerializeField] int shardHeal = 20;
        [Tooltip("체력 조각이 사라지기까지 시간 (초)")]
        [SerializeField] float shardLifetime = 10f;
        [Tooltip("체력 조각을 줍는 반지름 (타일)")]
        [SerializeField] float shardPickupRadius = 0.3f;

        public float Reach => reach;
        public float Height => height;
        public float FailRecovery => failRecovery;
        public float SuccessEvade => successEvade;
        public float StaggerTime => staggerTime;
        public Vector2 StaggerPush => staggerPush;
        public float KillReach => killReach;
        public float KillHeatReduction => killHeatReduction;
        public int ShardHeal => shardHeal;
        public float ShardLifetime => shardLifetime;
        public float ShardPickupRadius => shardPickupRadius;
    }
}
