using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 플레이어 에너지와 피격 수치 (GDD 4.1, 4.3).
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerHurtSettings", menuName = "Platformer/Player Hurt Settings")]
    public class PlayerHurtSettings : ScriptableObject
    {
        [Header("에너지")]
        [Tooltip("기본 최대 에너지 (에너지 탱크는 M4에서 추가)")]
        [SerializeField] int maxEnergy = 99;

        [Header("피격")]
        [Tooltip("피격 후 무적 시간 (초)")]
        [SerializeField] float invulnerableTime = 1f;
        [Tooltip("넉백 속도 (타일/초). x는 피해 반대 방향, y는 위로")]
        [SerializeField] Vector2 knockbackVelocity = new(6f, 7f);
        [Tooltip("넉백 후 조작이 안 되는 시간 (초)")]
        [SerializeField] float stunTime = 0.25f;
        [Tooltip("무적 중 깜빡임 횟수 (초당)")]
        [SerializeField] float blinkRate = 15f;

        public int MaxEnergy => maxEnergy;
        public float InvulnerableTime => invulnerableTime;
        public Vector2 KnockbackVelocity => knockbackVelocity;
        public float StunTime => stunTime;
        public float BlinkRate => blinkRate;
    }
}
