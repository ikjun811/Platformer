using System.Collections.Generic;
using Platformer.Gameplay;
using UnityEngine;
using UnityEngine.Pool;

namespace Platformer.Player
{
    /// <summary>
    /// 수평 사격, 무기 모듈, 차지 샷 (GDD v2 4.3). 바라보는 방향으로만 쏜다.
    /// J를 누르는 순간 현재 모듈의 탄을 쏘고 충전을 시작한다. 충전 시간 이상 누르고 있다가 떼면 차지 샷을 쏜다.
    /// 다 차기 전에 떼면 충전이 사라진다. 차지 샷은 모듈과 상관없이 공통이다.
    /// Q를 누르면 장착 목록(WeaponLoadout)의 다음 모듈로 바꾼다.
    /// 쏠 때마다 PlayerHeat에 열을 더하고, 과열 중에는 쏘지 못하며 사격 키는 배출 시도로 쓴다 (GDD 4.2).
    /// 쏘는 순간 PlayerCounter가 있으면 카운터 처치를 먼저 확인하고, 카운터 실패 후딜레이 중에는 쏘지 못한다 (GDD 4.5).
    /// 탄 오브젝트는 모듈별 풀에 모아 두고 재사용한다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(PlayerMovement), typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerHeat))]
    public class PlayerShooter : MonoBehaviour
    {
        // 짧게 누른 사격마다 충전 표시가 깜빡이지 않도록, 이 시간 이상 누르고 있을 때부터 보여준다 (초)
        const float ChargeEffectDelay = 0.15f;

        [SerializeField] PlayerCombatSettings settings;
        [Tooltip("쓸 수 있는 무기 모듈 목록. 첫 번째 모듈로 시작한다")]
        [SerializeField] WeaponLoadout loadout;
        [Tooltip("탄이 부딪히는 레이어")]
        [SerializeField] LayerMask hitLayers;
        [Tooltip("임시 탄 스프라이트 (아트 적용 후 교체)")]
        [SerializeField] Sprite beamSprite;
        [SerializeField] Color chargeBeamColor = new(0.3f, 1f, 0.95f);
        [Tooltip("임시 충전 표시. 총구 위치에서 충전량만큼 커지고, 다 차면 깜빡인다 (아트 적용 후 교체)")]
        [SerializeField] SpriteRenderer chargeEffect;

        BoxCollider2D box;
        PlayerMovement movement;
        PlayerInputReader input;
        PlayerHeat heat;
        // 없어도 된다 (카운터가 없는 테스트 구성)
        PlayerCounter counter;

        readonly Dictionary<WeaponModule, ObjectPool<Projectile>> modulePools = new();
        ObjectPool<Projectile> chargeBeamPool;
        int projectileLayer;
        int moduleIndex;
        float nextFireTime;

        bool charging;
        float chargeStartTime;

        /// <summary>현재 장착한 무기 모듈</summary>
        public WeaponModule CurrentModule => loadout.Modules[moduleIndex];

        /// <summary>충전량 0~1. 1이면 떼는 순간 차지 샷이 나간다</summary>
        public float ChargeProgress => charging ? Mathf.Clamp01((Time.time - chargeStartTime) / settings.ChargeTime) : 0f;

        /// <summary>사격 방향 (바라보는 방향의 수평)</summary>
        public Vector2 Direction => new(movement.FacingDirection, 0f);

        /// <summary>탄이 나가는 위치: 충돌체 윗면에서 어깨 깊이만큼 내려간 곳에서 바라보는 방향으로 총구 거리만큼 앞</summary>
        public Vector2 MuzzlePosition
        {
            get
            {
                Bounds b = box.bounds;
                var shoulder = new Vector2(b.center.x, b.max.y - settings.ShoulderDepth);
                return shoulder + Direction * settings.MuzzleDistance;
            }
        }

        void Reset()
        {
            hitLayers = LayerMask.GetMask("Ground", "Enemy");
        }

        void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            movement = GetComponent<PlayerMovement>();
            input = GetComponent<PlayerInputReader>();
            heat = GetComponent<PlayerHeat>();
            counter = GetComponent<PlayerCounter>();

            projectileLayer = LayerMask.NameToLayer("PlayerProjectile");
            foreach (WeaponModule module in loadout.Modules)
            {
                if (!modulePools.ContainsKey(module))
                    modulePools.Add(module, CreatePool(module.name, module.Length, module.Thickness, module.Color));
            }
            chargeBeamPool = CreatePool("ChargeBeam", settings.ChargeBeamLength, settings.ChargeBeamThickness, chargeBeamColor);
        }

        void OnDestroy()
        {
            foreach (ObjectPool<Projectile> pool in modulePools.Values)
                pool.Clear();
            chargeBeamPool?.Clear();
        }

        void Update()
        {
            if (input.SwapWeaponPressedThisFrame)
                SwapModule();

            // 과열 중에는 충전을 끊고, 사격 키는 배출 시도로만 쓴다
            if (heat.IsOverheated)
            {
                charging = false;
                if (input.FirePressedThisFrame && !movement.ControlLocked)
                    heat.TryVent();
                return;
            }

            if (input.FirePressedThisFrame)
            {
                FireModule();
                charging = true;
                chargeStartTime = Time.time;
            }

            if (!charging) return;

            // 놓친 입력(창 전환 등)으로 버튼이 이미 떨어져 있어도 충전을 끝낸다
            if (input.FireReleasedThisFrame || !input.FireHeld)
            {
                bool fullyCharged = ChargeProgress >= 1f;
                charging = false;
                if (fullyCharged)
                    FireChargeBeam();
            }
        }

        void LateUpdate()
        {
            if (chargeEffect == null) return;

            bool visible = charging && Time.time - chargeStartTime >= ChargeEffectDelay;
            chargeEffect.enabled = visible;
            if (!visible) return;

            float progress = ChargeProgress;
            float size = Mathf.Lerp(0.15f, 0.5f, progress);
            chargeEffect.transform.position = MuzzlePosition;
            chargeEffect.transform.localScale = new Vector3(size, size, 1f);

            Color color = chargeBeamColor;
            color.a = progress >= 1f ? Mathf.Lerp(0.4f, 1f, Mathf.PingPong(Time.time * 8f, 1f)) : 0.7f;
            chargeEffect.color = color;
        }

        bool CanFire() => !movement.ControlLocked && (counter == null || !counter.IsRecovering);

        void SwapModule()
        {
            if (loadout.Modules.Count <= 1) return;
            moduleIndex = (moduleIndex + 1) % loadout.Modules.Count;
        }

        // 현재 모듈의 탄을 쏜다. 여러 발이면 위아래로 고르게 벌려 쏜다
        void FireModule()
        {
            if (!CanFire() || Time.time < nextFireTime) return;

            WeaponModule module = CurrentModule;
            nextFireTime = Time.time + module.FireCooldown;
            if (counter != null) counter.TryCounterKill();

            ObjectPool<Projectile> pool = modulePools[module];
            int count = module.ProjectileCount;
            float step = count > 1 ? module.SpreadAngle / (count - 1) : 0f;
            for (int i = 0; i < count; i++)
            {
                float angle = count > 1 ? -module.SpreadAngle * 0.5f + step * i : 0f;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle * movement.FacingDirection) * Direction;
                pool.Get().Launch(MuzzlePosition, direction, module.Speed, module.Range, module.Radius,
                    module.Damage, hitLayers, module.PierceEnemies);
            }
            heat.AddHeat(module.Heat);
        }

        void FireChargeBeam()
        {
            // 조작이 잠긴 동안(대시 등) 다 찬 충전을 떼면 쏘지 못하고 사라진다
            if (!CanFire()) return;

            nextFireTime = Time.time + CurrentModule.FireCooldown;
            if (counter != null) counter.TryCounterKill();
            chargeBeamPool.Get().Launch(MuzzlePosition, Direction, settings.ChargeBeamSpeed, settings.ChargeBeamRange, settings.ChargeBeamRadius,
                settings.ChargeBeamDamage, hitLayers);
            heat.AddHeat(settings.ChargeBeamHeat);
        }

        // 탄 크기와 색은 처음 만들 때 정해진다 (플레이 중에 설정을 바꾸면 이미 만든 탄에는 반영되지 않음)
        ObjectPool<Projectile> CreatePool(string beamName, float length, float thickness, Color color)
        {
            ObjectPool<Projectile> pool = null;
            pool = new ObjectPool<Projectile>(
                () => CreateBeam(beamName, length, thickness, color, pool),
                beam => beam.gameObject.SetActive(true),
                beam => beam.gameObject.SetActive(false),
                // 플레이 종료 시에는 장면이 탄을 먼저 파괴한 뒤 풀이 정리되므로, 이미 사라진 탄은 건너뛴다
                beam => { if (beam != null) Destroy(beam.gameObject); },
                defaultCapacity: 8,
                maxSize: 32);
            return pool;
        }

        Projectile CreateBeam(string beamName, float length, float thickness, Color color, ObjectPool<Projectile> pool)
        {
            var go = new GameObject(beamName) { layer = projectileLayer };
            go.transform.localScale = new Vector3(length, thickness, 1f);

            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = beamSprite;
            sprite.color = color;
            sprite.sortingLayerName = "Projectiles";

            var beam = go.AddComponent<Projectile>();
            beam.Init(pool.Release);
            return beam;
        }
    }
}
