using System.Collections.Generic;
using Platformer.Enemies;
using Platformer.Gameplay;
using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 카운터와 카운터 처치 (GDD 4.5).
    /// - K: 몸 앞의 적이 공격 직전 신호 중이면 성공. 적을 밀쳐내고 경직시키며, 잠깐 피해를 받지 않는다.
    ///   신호 중인 적이 없으면 실패: 잠깐 사격·카운터를 못 한다 (남발 방지)
    /// - 카운터 처치: 카운터로 경직된 적이 몸 앞 가까이 있을 때 쏘면 즉사시킨다 (PlayerShooter가 쏠 때 호출).
    ///   체력 조각을 떨어뜨리고 열을 크게 내린다
    /// </summary>
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerInputReader), typeof(BoxCollider2D))]
    [RequireComponent(typeof(Health), typeof(PlayerHeat))]
    public class PlayerCounter : MonoBehaviour
    {
        [SerializeField] PlayerCounterSettings settings;
        [Tooltip("카운터 대상 레이어")]
        [SerializeField] LayerMask enemyLayers;
        [Tooltip("체력 조각 그림")]
        [SerializeField] Sprite shardSprite;

        PlayerMovement movement;
        PlayerInputReader input;
        BoxCollider2D box;
        Health health;
        PlayerHeat heat;

        ContactFilter2D enemyFilter;
        readonly List<Collider2D> hits = new();
        float recoveryEndTime;

        /// <summary>카운터 실패 후딜레이 중인지. 이 동안은 사격도 못 한다</summary>
        public bool IsRecovering => Time.time < recoveryEndTime;

        void Reset()
        {
            enemyLayers = LayerMask.GetMask("Enemy");
        }

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            input = GetComponent<PlayerInputReader>();
            box = GetComponent<BoxCollider2D>();
            health = GetComponent<Health>();
            heat = GetComponent<PlayerHeat>();
            enemyFilter = new ContactFilter2D { useLayerMask = true, layerMask = enemyLayers, useTriggers = false };
        }

        void Update()
        {
            if (!input.CounterPressedThisFrame) return;
            if (IsRecovering || movement.ControlLocked || movement.IsStunned) return;

            if (TryCounter())
                health.SetEvading(settings.SuccessEvade);
            else
                recoveryEndTime = Time.time + settings.FailRecovery;
        }

        // 몸 앞 판정 안에서 신호 중인 적을 모두 경직시킨다. 하나라도 성공하면 true
        bool TryCounter()
        {
            int count = OverlapFront(settings.Reach);
            bool success = false;
            int direction = movement.FacingDirection;
            Vector2 push = settings.StaggerPush;

            for (int i = 0; i < count; i++)
            {
                var counterable = hits[i].GetComponentInParent<ICounterable>();
                if (counterable == null || !counterable.IsCounterWindowOpen) continue;

                var enemy = hits[i].GetComponentInParent<Enemy>();
                if (enemy == null) continue;

                enemy.Stagger(new Vector2(direction * push.x, push.y), settings.StaggerTime);
                success = true;
            }
            return success;
        }

        /// <summary>
        /// 쏠 때 호출한다. 카운터로 경직된 적이 몸 앞 가까이 있으면 모두 즉사시키고 보상을 준다. 하나라도 처치하면 true.
        /// </summary>
        public bool TryCounterKill()
        {
            int count = OverlapFront(settings.KillReach);
            bool killed = false;

            for (int i = 0; i < count; i++)
            {
                var enemy = hits[i].GetComponentInParent<Enemy>();
                if (enemy == null || !enemy.IsCounterStaggered) continue;

                Vector2 position = enemy.transform.position;
                enemy.Kill(box.bounds.center);
                SpawnShard(position);
                killed = true;
            }

            if (killed)
                heat.ReduceHeat(settings.KillHeatReduction);
            return killed;
        }

        // 몸 앞면부터 바라보는 방향으로 distance만큼의 판정 상자에 든 적 충돌체 수 (결과는 hits)
        int OverlapFront(float distance)
        {
            Bounds b = box.bounds;
            int direction = movement.FacingDirection;
            float front = direction > 0 ? b.max.x : b.min.x;
            var center = new Vector2(front + direction * distance * 0.5f, b.center.y);
            var size = new Vector2(distance, settings.Height);
            return Physics2D.OverlapBox(center, size, 0f, enemyFilter, hits);
        }

        void SpawnShard(Vector2 position)
        {
            var go = new GameObject("HealthShard");
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = shardSprite;
            renderer.sortingLayerName = "FX";
            go.AddComponent<HealthPickup>().Init(settings.ShardHeal, settings.ShardPickupRadius,
                1 << gameObject.layer, settings.ShardLifetime);
        }

        void OnDrawGizmosSelected()
        {
            if (settings == null) return;
            var collider = GetComponent<BoxCollider2D>();
            if (collider == null) return;

            Bounds b = collider.bounds;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(new Vector3(b.max.x + settings.Reach * 0.5f, b.center.y), new Vector3(settings.Reach, settings.Height));
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
            Gizmos.DrawWireCube(new Vector3(b.max.x + settings.KillReach * 0.5f, b.center.y), new Vector3(settings.KillReach, settings.Height));
        }
    }
}
