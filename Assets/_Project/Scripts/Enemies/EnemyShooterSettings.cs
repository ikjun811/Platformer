using UnityEngine;

namespace Platformer.Enemies
{
    /// <summary>
    /// E5 감염 병사 수치 (GDD 4.6). 단위는 유닛(= 타일)과 초.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyShooterSettings", menuName = "Platformer/Enemy Shooter Settings")]
    public class EnemyShooterSettings : EnemySettings
    {
        [Header("감지")]
        [Tooltip("플레이어를 알아채는 가로 거리 (타일)")]
        [SerializeField] float detectRange = 8f;
        [Tooltip("이 높이 차이 안에 있어야 같은 층으로 보고 쏜다 (타일). 다른 층으로 피하면 쏘지 않는다")]
        [SerializeField] float detectHeight = 1f;

        [Header("이동")]
        [Tooltip("걷는 속도 (타일/초)")]
        [SerializeField] float walkSpeed = 1.5f;
        [Tooltip("이 거리 안이면 멈춰서 쏜다 (타일)")]
        [SerializeField] float fireRange = 6f;

        [Header("사격")]
        [Tooltip("멈춰서 겨누는 시간 (초). 쏘기 전 예고")]
        [SerializeField] float aimTime = 0.5f;
        [Tooltip("한 번에 이어 쏘는 탄 수")]
        [SerializeField, Min(1)] int burstCount = 3;
        [Tooltip("이어 쏘는 탄 사이 간격 (초)")]
        [SerializeField] float burstInterval = 0.2f;
        [Tooltip("다 쏜 뒤 쉬는 시간 (초)")]
        [SerializeField] float cooldownTime = 1.5f;

        [Header("탄")]
        [Tooltip("탄 피해")]
        [SerializeField] int bulletDamage = 10;
        [Tooltip("탄 속도 (타일/초). 플레이어 탄보다 느려서 보고 피할 수 있게")]
        [SerializeField] float bulletSpeed = 8f;
        [Tooltip("사거리 (타일)")]
        [SerializeField] float bulletRange = 9f;
        [Tooltip("충돌 반지름 (타일)")]
        [SerializeField] float bulletRadius = 0.06f;
        [Tooltip("탄 크기 (가로 x 세로, 타일, 임시 그래픽)")]
        [SerializeField] Vector2 bulletSize = new(0.3f, 0.08f);
        [Tooltip("탄 색 (임시 그래픽)")]
        [SerializeField] Color bulletColor = new(0.8f, 1f, 0.3f);

        [Header("총구 위치")]
        [Tooltip("충돌체 윗면에서 어깨까지 내려간 거리 (타일)")]
        [SerializeField] float shoulderDepth = 0.22f;
        [Tooltip("어깨에서 총구까지의 거리 (타일)")]
        [SerializeField] float muzzleDistance = 0.3f;

        public float DetectRange => detectRange;
        public float DetectHeight => detectHeight;
        public float WalkSpeed => walkSpeed;
        public float FireRange => fireRange;
        public float AimTime => aimTime;
        public int BurstCount => burstCount;
        public float BurstInterval => burstInterval;
        public float CooldownTime => cooldownTime;
        public int BulletDamage => bulletDamage;
        public float BulletSpeed => bulletSpeed;
        public float BulletRange => bulletRange;
        public float BulletRadius => bulletRadius;
        public Vector2 BulletSize => bulletSize;
        public Color BulletColor => bulletColor;
        public float ShoulderDepth => shoulderDepth;
        public float MuzzleDistance => muzzleDistance;
    }
}
