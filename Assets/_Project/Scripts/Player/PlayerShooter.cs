using Platformer.Gameplay;
using UnityEngine;
using UnityEngine.Pool;

namespace Platformer.Player
{
    /// <summary>
    /// 수평 사격과 차지 샷 (GDD v2 4.3). 바라보는 방향으로만 쏜다. 과열·무기 모듈은 N2에서 추가한다.
    /// J를 누르는 순간 일반 탄을 쏘고 충전을 시작한다. 충전 시간 이상 누르고 있다가 떼면 차지 샷을 쏜다.
    /// 다 차기 전에 떼면 충전이 사라진다. 빔 오브젝트는 종류별 풀에 모아 두고 재사용한다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(PlayerMovement), typeof(PlayerInputReader))]
    public class PlayerShooter : MonoBehaviour
    {
        // 짧게 누른 사격마다 충전 표시가 깜빡이지 않도록, 이 시간 이상 누르고 있을 때부터 보여준다 (초)
        const float ChargeEffectDelay = 0.15f;

        [SerializeField] PlayerCombatSettings settings;
        [Tooltip("빔이 부딪히는 레이어")]
        [SerializeField] LayerMask hitLayers;
        [Tooltip("임시 빔 스프라이트 (아트 적용 후 교체)")]
        [SerializeField] Sprite beamSprite;
        [SerializeField] Color beamColor = new(1f, 0.9f, 0.3f);
        [SerializeField] Color chargeBeamColor = new(0.3f, 1f, 0.95f);
        [Tooltip("임시 충전 표시. 총구 위치에서 충전량만큼 커지고, 다 차면 깜빡인다 (아트 적용 후 교체)")]
        [SerializeField] SpriteRenderer chargeEffect;

        BoxCollider2D box;
        PlayerMovement movement;
        PlayerInputReader input;

        ObjectPool<Projectile> beamPool;
        ObjectPool<Projectile> chargeBeamPool;
        int projectileLayer;
        float nextFireTime;

        bool charging;
        float chargeStartTime;

        /// <summary>충전량 0~1. 1이면 떼는 순간 차지 빔이 나간다</summary>
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

            projectileLayer = LayerMask.NameToLayer("PlayerProjectile");
            beamPool = CreatePool("Beam", settings.BeamLength, settings.BeamThickness, beamColor);
            chargeBeamPool = CreatePool("ChargeBeam", settings.ChargeBeamLength, settings.ChargeBeamThickness, chargeBeamColor);
        }

        void OnDestroy()
        {
            beamPool?.Clear();
            chargeBeamPool?.Clear();
        }

        void Update()
        {
            if (input.FirePressedThisFrame)
            {
                FireBeam();
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

        void FireBeam()
        {
            if (movement.ControlLocked || Time.time < nextFireTime) return;

            nextFireTime = Time.time + settings.FireCooldown;
            beamPool.Get().Launch(MuzzlePosition, Direction, settings.BeamSpeed, settings.BeamRange, settings.BeamRadius,
                settings.BeamDamage, hitLayers);
        }

        void FireChargeBeam()
        {
            // 조작이 잠긴 동안(대시 등) 다 찬 충전을 떼면 쏘지 못하고 사라진다
            if (movement.ControlLocked) return;

            nextFireTime = Time.time + settings.FireCooldown;
            chargeBeamPool.Get().Launch(MuzzlePosition, Direction, settings.ChargeBeamSpeed, settings.ChargeBeamRange, settings.ChargeBeamRadius,
                settings.ChargeBeamDamage, hitLayers);
        }

        // 빔 크기와 색은 처음 만들 때 정해진다 (플레이 중에 설정을 바꾸면 이미 만든 빔에는 반영되지 않음)
        ObjectPool<Projectile> CreatePool(string beamName, float length, float thickness, Color color)
        {
            ObjectPool<Projectile> pool = null;
            pool = new ObjectPool<Projectile>(
                () => CreateBeam(beamName, length, thickness, color, pool),
                beam => beam.gameObject.SetActive(true),
                beam => beam.gameObject.SetActive(false),
                // 플레이 종료 시에는 장면이 빔을 먼저 파괴한 뒤 풀이 정리되므로, 이미 사라진 빔은 건너뛴다
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
            sprite.sortingOrder = 20;

            var beam = go.AddComponent<Projectile>();
            beam.Init(pool.Release);
            return beam;
        }
    }
}
