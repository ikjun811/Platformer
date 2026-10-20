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

        public float BeamSpeed => beamSpeed;
        public float BeamRange => beamRange;
        public float BeamRadius => beamRadius;
        public float FireCooldown => fireCooldown;
        public float ShoulderDepth => shoulderDepth;
        public float MuzzleDistance => muzzleDistance;
        public float BeamLength => beamLength;
        public float BeamThickness => beamThickness;
    }
}
