using System;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 체력 (플레이어의 에너지, 적의 체력 공용).
    /// 무적 시간 동안은 피해를 무시한다. 피해·사망은 이벤트로 알린다.
    /// 같은 오브젝트에 IDamageModifier가 있으면 받는 피해량을 그쪽에 물어 바꾼다.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [Tooltip("최대 체력. 플레이어는 PlayerHurt가 설정 에셋 값으로 덮어쓴다")]
        [SerializeField] int maxHealth = 10;

        IDamageModifier modifier;
        float invulnerableUntil;
        float evadingUntil;

        public int Current { get; private set; }
        public int Max => maxHealth;
        public bool IsDead => Current <= 0;

        /// <summary>피격 후 무적 중인지 (깜빡임 표시 대상)</summary>
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        /// <summary>회피 무적 중인지 (대시 등). 피해는 무시하지만 깜빡임은 표시하지 않는다</summary>
        public bool IsEvading => Time.time < evadingUntil;

        /// <summary>피해를 받은 직후 (Current는 이미 줄어든 값)</summary>
        public event Action<DamageInfo> Damaged;

        /// <summary>체력이 0이 된 직후 (Damaged 다음에 호출)</summary>
        public event Action Died;

        void Awake()
        {
            Current = maxHealth;
            modifier = GetComponent<IDamageModifier>();
        }

        public void SetMax(int max, bool refill)
        {
            maxHealth = Mathf.Max(1, max);
            Current = refill ? maxHealth : Mathf.Min(Current, maxHealth);
        }

        public bool TakeDamage(in DamageInfo damage)
        {
            if (IsDead || IsInvulnerable || IsEvading || damage.Amount <= 0) return false;

            DamageInfo applied = modifier != null ? damage.WithAmount(modifier.ModifyDamage(damage)) : damage;
            if (applied.Amount <= 0) return false;

            Current = Mathf.Max(0, Current - applied.Amount);
            Damaged?.Invoke(applied);
            if (Current == 0)
                Died?.Invoke();
            return true;
        }

        /// <summary>무적·피해 감소와 상관없이 즉시 죽인다 (카운터 처치).</summary>
        public void Kill(Vector2 sourcePosition)
        {
            if (IsDead) return;

            int amount = Current;
            Current = 0;
            Damaged?.Invoke(new DamageInfo(amount, sourcePosition));
            Died?.Invoke();
        }

        public void SetInvulnerable(float duration)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + duration);
        }

        public void SetEvading(float duration)
        {
            evadingUntil = Mathf.Max(evadingUntil, Time.time + duration);
        }

        public void RestoreFull()
        {
            Current = maxHealth;
        }

        /// <summary>체력을 회복한다 (최대치까지). 죽은 상태에서는 회복하지 않는다.</summary>
        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            Current = Mathf.Min(maxHealth, Current + amount);
        }
    }
}
