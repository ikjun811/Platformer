using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 닿아 있는 대상에게 피해를 준다 (가시 타일, 시험용 피해 상자 등).
    /// 트리거·일반 충돌체 모두 지원한다. 무적 시간 동안은 Health가 피해를 무시하므로 매 물리 프레임 시도해도 된다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DamageZone : MonoBehaviour
    {
        [SerializeField] int damage = 10;
        [Tooltip("가시·용암 같은 환경 피해. 플레이어를 넉백 대신 마지막 안전한 발판으로 되돌린다")]
        [SerializeField] bool isHazard;

        Collider2D zone;

        void Awake()
        {
            zone = GetComponent<Collider2D>();
        }

        void OnTriggerStay2D(Collider2D other) => TryDamage(other);

        void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.collider);

        void TryDamage(Collider2D other)
        {
            var target = other.GetComponentInParent<IDamageable>();
            if (target == null) return;

            // 넉백 방향은 이 영역에서 대상과 가장 가까운 지점 기준
            Vector2 source = zone.ClosestPoint(other.bounds.center);
            target.TakeDamage(new DamageInfo(damage, source, isHazard));
        }
    }
}
