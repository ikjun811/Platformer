using UnityEngine;

namespace Platformer.Enemies
{
    /// <summary>
    /// E1 러너 수치 (GDD 4.6). 단위는 유닛(= 타일)과 초.
    /// E2 브루트도 같은 행동(EnemyRunner)을 쓰므로 이 설정을 다른 값으로 하나 더 만들어 쓴다.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyRunnerSettings", menuName = "Platformer/Enemy Runner Settings")]
    public class EnemyRunnerSettings : EnemySettings
    {
        [Header("감지")]
        [Tooltip("플레이어를 알아채는 가로 거리 (타일)")]
        [SerializeField] float detectRange = 6f;
        [Tooltip("이 높이 차이 안에 있어야 같은 층으로 보고 쫓는다 (타일)")]
        [SerializeField] float detectHeight = 1.5f;

        [Header("추격")]
        [Tooltip("달리는 속도 (타일/초). 플레이어보다 조금 느리게")]
        [SerializeField] float chaseSpeed = 3.2f;
        [Tooltip("몸 앞면과 플레이어 사이가 이 거리 안이면 물기를 준비한다 (타일)")]
        [SerializeField] float attackRange = 0.5f;

        [Header("물기")]
        [Tooltip("물기 전 웅크리는 전체 시간 (초). 마지막 신호 시간 동안 하얗게 깜빡인다")]
        [SerializeField] float windupTime = 0.5f;
        [Tooltip("물기 직전 신호(흰 깜빡임) 시간 = 카운터 판정 시간 (초, GDD 4.5)")]
        [SerializeField] float signalTime = 0.2f;
        [Tooltip("물 때 앞으로 튀어 나가는 속도 (타일/초)")]
        [SerializeField] float lungeSpeed = 6f;
        [Tooltip("튀어 나가는 시간 (초)")]
        [SerializeField] float lungeTime = 0.15f;
        [Tooltip("물기 피해")]
        [SerializeField] int biteDamage = 15;
        [Tooltip("물린 플레이어가 밀려나는 세기 배율 (1 = 기본)")]
        [SerializeField] float biteKnockbackScale = 1f;
        [Tooltip("물기 판정 크기 (몸 앞쪽, 타일)")]
        [SerializeField] Vector2 biteSize = new(0.5f, 0.5f);
        [Tooltip("물고 난 뒤 쉬는 시간 (초)")]
        [SerializeField] float recoverTime = 0.6f;

        public float DetectRange => detectRange;
        public float DetectHeight => detectHeight;
        public float ChaseSpeed => chaseSpeed;
        public float AttackRange => attackRange;
        public float WindupTime => windupTime;
        public float SignalTime => signalTime;
        public float LungeSpeed => lungeSpeed;
        public float LungeTime => lungeTime;
        public int BiteDamage => biteDamage;
        public float BiteKnockbackScale => biteKnockbackScale;
        public Vector2 BiteSize => biteSize;
        public float RecoverTime => recoverTime;
    }
}
