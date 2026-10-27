using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 플레이어 이동 수치. 단위는 유닛(= 타일)과 초.
    /// 플레이 중에 값을 바꾸면 바로 반영되고, 에셋이라 플레이를 멈춰도 값이 유지된다.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerMovementSettings", menuName = "Platformer/Player Movement Settings")]
    public class PlayerMovementSettings : ScriptableObject
    {
        [Header("달리기")]
        [Tooltip("최고 속도 (타일/초)")]
        [SerializeField] float maxSpeed = 9.5f;
        [Tooltip("지상 가속도 (타일/초²)")]
        [SerializeField] float groundAcceleration = 150f;
        [Tooltip("지상 감속도 (타일/초²)")]
        [SerializeField] float groundDeceleration = 150f;
        [Tooltip("공중 가속도 (타일/초²)")]
        [SerializeField] float airAcceleration = 50f;
        [Tooltip("공중 감속도 (타일/초²)")]
        [SerializeField] float airDeceleration = 30f;

        [Header("점프")]
        [Tooltip("점프 최고 높이 (타일)")]
        [SerializeField] float jumpHeight = 3.5f;
        [Tooltip("최고점까지 걸리는 시간 (초)")]
        [SerializeField] float timeToApex = 0.4f;
        [Tooltip("낙하할 때 중력 배율")]
        [SerializeField] float fallGravityMultiplier = 1.8f;
        [Tooltip("상승 중 점프 키를 떼면 적용되는 중력 배율 (짧은 점프)")]
        [SerializeField] float jumpCutGravityMultiplier = 2.5f;
        [Tooltip("최대 낙하 속도 (타일/초)")]
        [SerializeField] float maxFallSpeed = 20f;

        [Header("낮은 턱")]
        [Tooltip("이 높이 이하의 단차는 점프 없이 걸어서 오르내린다 (타일). 계단 한 칸 = 0.125")]
        [SerializeField] float maxStepHeight = 0.15f;

        [Header("발판 내려가기")]
        [Tooltip("아래 + 점프로 얇은 발판을 내려갈 때 그 발판을 무시하는 시간 (초)")]
        [SerializeField] float dropThroughTime = 0.25f;

        [Header("조작 보정")]
        [Tooltip("발판에서 떨어진 뒤에도 점프할 수 있는 시간 (초)")]
        [SerializeField] float coyoteTime = 0.1f;
        [Tooltip("착지 전에 미리 누른 점프를 기억하는 시간 (초)")]
        [SerializeField] float jumpBufferTime = 0.12f;

        public float MaxSpeed => maxSpeed;
        public float GroundAcceleration => groundAcceleration;
        public float GroundDeceleration => groundDeceleration;
        public float AirAcceleration => airAcceleration;
        public float AirDeceleration => airDeceleration;
        public float FallGravityMultiplier => fallGravityMultiplier;
        public float JumpCutGravityMultiplier => jumpCutGravityMultiplier;
        public float MaxFallSpeed => maxFallSpeed;
        public float MaxStepHeight => maxStepHeight;
        public float DropThroughTime => dropThroughTime;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;

        // 원하는 점프 높이 h와 최고점 시간 t로부터 계산: g = 2h / t², v = g * t
        public float Gravity => 2f * jumpHeight / (timeToApex * timeToApex);
        public float JumpVelocity => Gravity * timeToApex;
    }
}
