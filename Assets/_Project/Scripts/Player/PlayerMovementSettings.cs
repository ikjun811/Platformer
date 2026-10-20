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
        [SerializeField] float maxSpeed = 7f;
        [Tooltip("지상 가속도 (타일/초²)")]
        [SerializeField] float groundAcceleration = 80f;
        [Tooltip("지상 감속도 (타일/초²)")]
        [SerializeField] float groundDeceleration = 90f;
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

        [Header("앉기")]
        [Tooltip("앉았을 때 충돌체 높이 (타일). 서 있을 때 높이는 BoxCollider2D 크기를 따른다")]
        [SerializeField] float crouchHeight = 1.5f;

        [Header("난간 잡기")]
        [Tooltip("머리 끝에서 아래로 이 범위 안에 모서리가 오면 잡는다 (타일)")]
        [SerializeField] float ledgeGrabZone = 0.5f;
        [Tooltip("기어오르기에 걸리는 시간 (초)")]
        [SerializeField] float ledgeClimbDuration = 0.25f;
        [Tooltip("손을 놓은 뒤 다시 잡을 수 없는 시간 (초)")]
        [SerializeField] float ledgeRegrabDelay = 0.25f;

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
        public float CrouchHeight => crouchHeight;
        public float LedgeGrabZone => ledgeGrabZone;
        public float LedgeClimbDuration => ledgeClimbDuration;
        public float LedgeRegrabDelay => ledgeRegrabDelay;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;

        // 원하는 점프 높이 h와 최고점 시간 t로부터 계산: g = 2h / t², v = g * t
        public float Gravity => 2f * jumpHeight / (timeToApex * timeToApex);
        public float JumpVelocity => Gravity * timeToApex;
    }
}
