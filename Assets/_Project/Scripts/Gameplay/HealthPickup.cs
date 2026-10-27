using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 체력 조각 (GDD 4.5 카운터 처치 보상). 떨어진 자리에 떠서 위아래로 흔들리고, 플레이어가 닿으면 체력을 회복한다.
    /// 물리 충돌 설정에 기대지 않도록 매 물리 프레임 겹침 검사로 줍는다. 일정 시간이 지나면 깜빡이다 사라진다.
    /// </summary>
    public class HealthPickup : MonoBehaviour
    {
        // 위아래로 흔들리는 폭 (타일)과 속도 (초당 회전)
        const float BobHeight = 0.05f;
        const float BobSpeed = 2f;
        // 사라지기 전 깜빡이는 시간 (초)
        const float BlinkTime = 2f;

        int healAmount;
        float pickupRadius;
        LayerMask collectorLayers;
        float expireTime;
        Vector2 basePosition;
        SpriteRenderer spriteRenderer;

        /// <summary>코드로 만든 직후 한 번 설정한다.</summary>
        public void Init(int heal, float radius, LayerMask collectors, float lifetime)
        {
            healAmount = heal;
            pickupRadius = radius;
            collectorLayers = collectors;
            expireTime = Time.time + lifetime;
            basePosition = transform.position;
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            float bob = Mathf.Sin(Time.time * BobSpeed * Mathf.PI * 2f) * BobHeight;
            transform.position = basePosition + new Vector2(0f, bob);

            if (spriteRenderer != null)
            {
                float remaining = expireTime - Time.time;
                spriteRenderer.enabled = remaining > BlinkTime || Mathf.Repeat(Time.time * 8f, 1f) < 0.5f;
            }

            if (Time.time >= expireTime)
                Destroy(gameObject);
        }

        void FixedUpdate()
        {
            Collider2D collector = Physics2D.OverlapCircle(basePosition, pickupRadius, collectorLayers);
            if (collector == null) return;

            var health = collector.GetComponentInParent<Health>();
            if (health == null || health.IsDead) return;

            health.Heal(healAmount);
            // 체력 표시(HUD, N5)가 생기기 전까지 로그로 확인한다
            Debug.Log($"[HealthPickup] 체력 조각 +{healAmount} → 체력 {health.Current}/{health.Max}");
            Destroy(gameObject);
        }
    }
}
