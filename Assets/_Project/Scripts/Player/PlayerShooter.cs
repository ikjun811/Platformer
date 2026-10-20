using Platformer.Gameplay;
using UnityEngine;
using UnityEngine.Pool;

namespace Platformer.Player
{
    /// <summary>
    /// 빔 사격. J를 누르면 조준 방향으로 빔을 쏜다 (차지는 M2-2에서 추가).
    /// 빔 오브젝트는 풀에 모아 두고 재사용한다.
    /// </summary>
    [RequireComponent(typeof(PlayerAim), typeof(PlayerMovement), typeof(PlayerInputReader))]
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] PlayerCombatSettings settings;
        [Tooltip("빔이 부딪히는 레이어")]
        [SerializeField] LayerMask hitLayers;
        [Tooltip("임시 빔 스프라이트 (아트 적용 후 교체)")]
        [SerializeField] Sprite beamSprite;
        [SerializeField] Color beamColor = new(1f, 0.9f, 0.3f);

        PlayerAim aim;
        PlayerMovement movement;
        PlayerInputReader input;

        ObjectPool<Projectile> pool;
        int projectileLayer;
        float nextFireTime;

        void Reset()
        {
            hitLayers = LayerMask.GetMask("Ground", "Enemy");
        }

        void Awake()
        {
            aim = GetComponent<PlayerAim>();
            movement = GetComponent<PlayerMovement>();
            input = GetComponent<PlayerInputReader>();

            projectileLayer = LayerMask.NameToLayer("PlayerProjectile");
            pool = new ObjectPool<Projectile>(
                CreateBeam,
                beam => beam.gameObject.SetActive(true),
                beam => beam.gameObject.SetActive(false),
                // 플레이 종료 시에는 장면이 빔을 먼저 파괴한 뒤 풀이 정리되므로, 이미 사라진 빔은 건너뛴다
                beam => { if (beam != null) Destroy(beam.gameObject); },
                defaultCapacity: 8,
                maxSize: 32);
        }

        void OnDestroy()
        {
            pool?.Clear();
        }

        void Update()
        {
            if (!input.FirePressedThisFrame) return;
            if (movement.ControlLocked || Time.time < nextFireTime) return;

            nextFireTime = Time.time + settings.FireCooldown;
            Projectile beam = pool.Get();
            beam.Launch(aim.MuzzlePosition, aim.Direction, settings.BeamSpeed, settings.BeamRange, settings.BeamRadius, hitLayers);
        }

        Projectile CreateBeam()
        {
            var go = new GameObject("Beam") { layer = projectileLayer };
            go.transform.localScale = new Vector3(settings.BeamLength, settings.BeamThickness, 1f);

            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = beamSprite;
            sprite.color = beamColor;
            sprite.sortingOrder = 20;

            var beam = go.AddComponent<Projectile>();
            beam.Init(pool.Release);
            return beam;
        }
    }
}
