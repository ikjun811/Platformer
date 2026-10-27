using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 플레이어 사격 공통 수치 (과열, 차지 샷, 총구 위치). 단위는 유닛(= 타일)과 초.
    /// 일반 탄 수치는 무기 모듈(WeaponModule)마다 따로 있다.
    /// 플레이 중에 값을 바꾸면 바로 반영된다.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerCombatSettings", menuName = "Platformer/Player Combat Settings")]
    public class PlayerCombatSettings : ScriptableObject
    {
        [Header("과열 (GDD 4.2)")]
        [Tooltip("게이지 최대치. 꽉 차면 과열")]
        [SerializeField] float maxHeat = 100f;
        [Tooltip("차지 샷 한 발에 오르는 열")]
        [SerializeField] float chargeBeamHeat = 35f;
        [Tooltip("마지막 사격 후 이 시간이 지나야 식기 시작한다 (초)")]
        [SerializeField] float coolDelay = 0.35f;
        [Tooltip("초당 식는 열")]
        [SerializeField] float coolRate = 45f;
        [Tooltip("과열되면 사격할 수 없는 시간 (초). 끝나면 열이 0이 된다")]
        [SerializeField] float overheatTime = 1.5f;

        [Header("과열 배출")]
        [Tooltip("과열 시간 중 배출 성공 구간 시작 (0~1 비율)")]
        [SerializeField, Range(0f, 1f)] float ventWindowStart = 0.4f;
        [Tooltip("과열 시간 중 배출 성공 구간 끝 (0~1 비율)")]
        [SerializeField, Range(0f, 1f)] float ventWindowEnd = 0.6f;
        [Tooltip("구간 밖에서 누르면 늘어나는 과열 시간 (초). 한 번 실패하면 다시 시도할 수 없다")]
        [SerializeField] float ventFailPenalty = 0.75f;
        [Tooltip("배출 성공 시 충격파 반지름 (타일)")]
        [SerializeField] float shockwaveRadius = 1.5f;
        [Tooltip("충격파에 맞은 적이 밀려나는 속도 (x = 수평, y = 위)")]
        [SerializeField] Vector2 shockwavePush = new(5f, 2.5f);
        [Tooltip("충격파에 맞은 적이 움직이지 못하는 시간 (초)")]
        [SerializeField] float shockwaveStun = 0.5f;

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

        [Header("차지 빔 모양 (임시 그래픽)")]
        [Tooltip("차지 빔 길이 (타일)")]
        [SerializeField] float chargeBeamLength = 1.2f;
        [Tooltip("차지 빔 두께 (타일)")]
        [SerializeField] float chargeBeamThickness = 0.5f;

        public int ChargeBeamDamage => chargeBeamDamage;
        public float MaxHeat => maxHeat;
        public float ChargeBeamHeat => chargeBeamHeat;
        public float CoolDelay => coolDelay;
        public float CoolRate => coolRate;
        public float OverheatTime => overheatTime;
        public float VentWindowStart => ventWindowStart;
        public float VentWindowEnd => ventWindowEnd;
        public float VentFailPenalty => ventFailPenalty;
        public float ShockwaveRadius => shockwaveRadius;
        public Vector2 ShockwavePush => shockwavePush;
        public float ShockwaveStun => shockwaveStun;
        public float ChargeTime => chargeTime;
        public float ChargeBeamSpeed => chargeBeamSpeed;
        public float ChargeBeamRange => chargeBeamRange;
        public float ChargeBeamRadius => chargeBeamRadius;
        public float ShoulderDepth => shoulderDepth;
        public float MuzzleDistance => muzzleDistance;
        public float ChargeBeamLength => chargeBeamLength;
        public float ChargeBeamThickness => chargeBeamThickness;
    }
}
