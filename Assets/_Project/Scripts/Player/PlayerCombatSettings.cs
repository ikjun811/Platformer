using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 플레이어 사격 수치. 단위는 유닛(= 타일)과 초.
    /// 플레이 중에 값을 바꾸면 바로 반영된다.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerCombatSettings", menuName = "Platformer/Player Combat Settings")]
    public class PlayerCombatSettings : ScriptableObject
    {
        [Header("빔")]
        [Tooltip("빔 속도 (타일/초)")]
        [SerializeField] float beamSpeed = 24f;
        [Tooltip("빔 사거리 (타일). 이 거리를 날아가면 사라진다")]
        [SerializeField] float beamRange = 12f;
        [Tooltip("빔 충돌 반지름 (타일)")]
        [SerializeField] float beamRadius = 0.15f;
        [Tooltip("연사 간격 (초)")]
        [SerializeField] float fireCooldown = 0.12f;
        [Tooltip("빔 피해")]
        [SerializeField] int beamDamage = 10;

        [Header("차지 빔")]
        [Tooltip("J를 누르고 있어야 하는 충전 시간 (초). 다 차면 떼는 순간 차지 빔")]
        [SerializeField] float chargeTime = 0.9f;
        [Tooltip("차지 빔 속도 (타일/초)")]
        [SerializeField] float chargeBeamSpeed = 26f;
        [Tooltip("차지 빔 사거리 (타일)")]
        [SerializeField] float chargeBeamRange = 14f;
        [Tooltip("차지 빔 충돌 반지름 (타일)")]
        [SerializeField] float chargeBeamRadius = 0.35f;
        [Tooltip("차지 빔 피해")]
        [SerializeField] int chargeBeamDamage = 30;

        [Header("총구 위치")]
        [Tooltip("충돌체 윗면에서 어깨(조준 기준점)까지 내려간 거리 (타일)")]
        [SerializeField] float shoulderDepth = 0.65f;
        [Tooltip("어깨에서 조준 방향으로 총구까지의 거리 (타일)")]
        [SerializeField] float muzzleDistance = 0.6f;

        [Header("빔 모양 (임시 그래픽)")]
        [Tooltip("빔 길이 (타일)")]
        [SerializeField] float beamLength = 0.6f;
        [Tooltip("빔 두께 (타일)")]
        [SerializeField] float beamThickness = 0.2f;
        [Tooltip("차지 빔 길이 (타일)")]
        [SerializeField] float chargeBeamLength = 1.2f;
        [Tooltip("차지 빔 두께 (타일)")]
        [SerializeField] float chargeBeamThickness = 0.5f;

        public float BeamSpeed => beamSpeed;
        public float BeamRange => beamRange;
        public float BeamRadius => beamRadius;
        public float FireCooldown => fireCooldown;
        public int BeamDamage => beamDamage;
        public int ChargeBeamDamage => chargeBeamDamage;
        public float ChargeTime => chargeTime;
        public float ChargeBeamSpeed => chargeBeamSpeed;
        public float ChargeBeamRange => chargeBeamRange;
        public float ChargeBeamRadius => chargeBeamRadius;
        public float ShoulderDepth => shoulderDepth;
        public float MuzzleDistance => muzzleDistance;
        public float BeamLength => beamLength;
        public float BeamThickness => beamThickness;
        public float ChargeBeamLength => chargeBeamLength;
        public float ChargeBeamThickness => chargeBeamThickness;
    }
}
