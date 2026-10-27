using System.Collections.Generic;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 부서지는 환경 요소 (GDD 4.7).
    /// - X3 파괴 상자: 지형(Ground) 레이어라 플레이어·적·탄을 막는 엄폐물. 체력이 다하면 부서진다 (적 탄에도 깎인다)
    /// - X1 가스탱크: 적(Enemy) 레이어라 플레이어 탄에만 맞는다. 부서지면 폭발해 반경 안의 플레이어·적·다른 환경 요소에 피해를 준다
    ///   (방어 무시, 다른 가스탱크를 연쇄 폭발시킨다)
    /// 부서지면 지우지 않고 꺼 두며, 방이 초기화될 때 Restore로 되살린다.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Destructible : MonoBehaviour
    {
        [SerializeField] DestructibleSettings settings;
        [Tooltip("폭발 피해를 받는 레이어")]
        [SerializeField] LayerMask explosionLayers;
        [Tooltip("이 레이어는 플레이어용 폭발 피해를 받는다")]
        [SerializeField] LayerMask playerLayers;
        [Tooltip("폭발 이펙트 시트")]
        [SerializeField] SheetClip explosionEffect = new();

        Health health;

        void Reset()
        {
            explosionLayers = LayerMask.GetMask("Player", "Enemy", "Ground");
            playerLayers = LayerMask.GetMask("Player");
        }

        void Awake()
        {
            health = GetComponent<Health>();
            health.SetMax(settings.MaxHealth, true);
            if (settings.Explodes)
                explosionEffect.Build();
        }

        void OnEnable()
        {
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Died -= OnDied;
        }

        /// <summary>부서진 상태에서 되살린다 (방 초기화).</summary>
        public void Restore()
        {
            gameObject.SetActive(true);
            health.RestoreFull();
        }

        void OnDied()
        {
            if (settings.Explodes)
                Explode();
            gameObject.SetActive(false);
        }

        void Explode()
        {
            Vector2 center = transform.position;
            SpawnEffect(center);

            // 연쇄 폭발이 같은 목록을 건드리지 않도록 폭발마다 새 목록을 쓴다 (드물게 일어나는 일이라 할당은 괜찮다)
            var hits = new List<Collider2D>();
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = explosionLayers, useTriggers = false };
            Physics2D.OverlapCircle(center, settings.ExplosionRadius, filter, hits);

            var damaged = new HashSet<IDamageable>();
            foreach (Collider2D hit in hits)
            {
                var target = hit.GetComponentInParent<IDamageable>();
                if (target == null || ReferenceEquals(target, health) || !damaged.Add(target)) continue;

                bool isPlayer = (playerLayers.value & (1 << hit.gameObject.layer)) != 0;
                int amount = isPlayer ? settings.PlayerExplosionDamage : settings.ExplosionDamage;
                target.TakeDamage(new DamageInfo(amount, center, false, settings.ExplosionKnockbackScale, true));
            }
        }

        void SpawnEffect(Vector2 center)
        {
            if (explosionEffect.Length == 0) return;

            var go = new GameObject("Explosion");
            float scale = settings.EffectScale;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "FX";
            // 이펙트 피벗이 아래 중앙이므로 그림 절반 높이만큼 내려 폭발 중심에 맞춘다
            float halfHeight = explosionEffect.Frame(0).bounds.size.y * scale * 0.5f;
            go.transform.position = center - new Vector2(0f, halfHeight);
            go.AddComponent<OneShotEffect>().Play(explosionEffect.Frames, explosionEffect.FramesPerSecond);
        }

        void OnDrawGizmosSelected()
        {
            if (settings == null || !settings.Explodes) return;
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, settings.ExplosionRadius);
        }
    }
}
