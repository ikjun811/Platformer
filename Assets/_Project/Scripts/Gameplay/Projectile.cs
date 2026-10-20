using System;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 직선으로 날아가는 발사체 (빔 등).
    /// 충돌체 없이 매 물리 프레임 원형 캐스트로 앞을 확인해서, 빠르게 날아도 얇은 벽을 뚫지 않는다.
    /// 오브젝트 풀에서 꺼내 Launch로 발사하고, 부딪히거나 사거리가 끝나면 풀로 돌려보낸다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        Rigidbody2D body;
        Action<Projectile> release;

        Vector2 direction;
        float speed;
        float radius;
        float remainingDistance;
        LayerMask hitLayers;
        bool active;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        /// <summary>풀로 돌려보내는 방법을 정한다 (생성 직후 한 번).</summary>
        public void Init(Action<Projectile> releaseToPool) => release = releaseToPool;

        public void Launch(Vector2 position, Vector2 launchDirection, float launchSpeed, float range, float hitRadius, LayerMask layers)
        {
            direction = launchDirection.normalized;
            speed = launchSpeed;
            radius = hitRadius;
            remainingDistance = range;
            hitLayers = layers;
            active = true;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            body.position = position;
            body.rotation = angle;

            // 총구가 벽 안에 있으면 바로 사라진다
            if (Physics2D.OverlapCircle(position, radius, hitLayers))
                Despawn();
        }

        void FixedUpdate()
        {
            if (!active) return;

            float step = Mathf.Min(speed * Time.fixedDeltaTime, remainingDistance);
            RaycastHit2D hit = Physics2D.CircleCast(body.position, radius, direction, step, hitLayers);
            if (hit)
            {
                // 맞은 대상에 피해를 주는 처리는 적 구현(M2-4)에서 추가한다
                Despawn();
                return;
            }

            body.MovePosition(body.position + direction * step);
            remainingDistance -= step;
            if (remainingDistance <= 0f)
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
