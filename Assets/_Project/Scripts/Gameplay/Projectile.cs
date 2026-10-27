using System;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 직선으로 날아가는 발사체 (빔 등).
    /// 충돌체 없이 매 물리 프레임 원형 캐스트로 앞을 확인해서, 빠르게 날아도 얇은 벽을 뚫지 않는다.
    /// 오브젝트 풀에서 꺼내 Launch로 발사하고, 부딪히거나 사거리가 끝나면 풀로 돌려보낸다.
    /// 맞은 대상이 피해를 받을 수 있으면(IDamageable) 피해를 준다.
    /// 관통 탄은 피해를 받을 수 있는 대상(적)을 뚫고 지나가며 대상마다 한 번씩만 피해를 주고, 벽에서 멈춘다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        static readonly Comparison<RaycastHit2D> ByDistance = (a, b) => a.distance.CompareTo(b.distance);

        Rigidbody2D body;
        Action<Projectile> release;

        Vector2 direction;
        float speed;
        float radius;
        float remainingDistance;
        int damage;
        bool pierce;
        ContactFilter2D hitFilter;
        bool active;

        readonly List<RaycastHit2D> castHits = new();
        readonly List<Collider2D> overlapHits = new();
        readonly HashSet<IDamageable> damaged = new();

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        /// <summary>풀로 돌려보내는 방법을 정한다 (생성 직후 한 번).</summary>
        public void Init(Action<Projectile> releaseToPool) => release = releaseToPool;

        public void Launch(Vector2 position, Vector2 launchDirection, float launchSpeed, float range, float hitRadius, int hitDamage,
            LayerMask layers, bool pierceTargets = false)
        {
            damage = hitDamage;
            direction = launchDirection.normalized;
            speed = launchSpeed;
            radius = hitRadius;
            remainingDistance = range;
            pierce = pierceTargets;
            hitFilter = new ContactFilter2D { useLayerMask = true, layerMask = layers, useTriggers = false };
            damaged.Clear();
            active = true;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            body.position = position;
            body.rotation = angle;

            // 총구가 벽이나 적 안에 있으면 바로 맞힌다
            int count = Physics2D.OverlapCircle(position, radius, hitFilter, overlapHits);
            for (int i = 0; i < count && active; i++)
                HandleHit(overlapHits[i], position);
        }

        void FixedUpdate()
        {
            if (!active) return;

            float step = Mathf.Min(speed * Time.fixedDeltaTime, remainingDistance);
            int count = Physics2D.CircleCast(body.position, radius, direction, hitFilter, castHits, step);
            if (count > 1)
                castHits.Sort(ByDistance);

            for (int i = 0; i < count; i++)
            {
                HandleHit(castHits[i].collider, castHits[i].point);
                if (!active) return;
            }

            body.MovePosition(body.position + direction * step);
            remainingDistance -= step;
            if (remainingDistance <= 0f)
                Despawn();
        }

        // 관통 탄이 피해를 받을 수 있는 대상에 맞으면 피해만 주고 계속 날아간다. 그 외에는 맞히고 사라진다
        void HandleHit(Collider2D target, Vector2 point)
        {
            var damageable = target.GetComponentInParent<IDamageable>();
            if (pierce && damageable != null)
            {
                if (damaged.Add(damageable))
                    damageable.TakeDamage(new DamageInfo(damage, point));
                return;
            }

            damageable?.TakeDamage(new DamageInfo(damage, point));
            Despawn();
        }

        void Despawn()
        {
            if (!active) return;
            active = false;
            release?.Invoke(this);
        }
    }
}
