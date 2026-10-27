using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>피해 한 번의 정보.</summary>
    public readonly struct DamageInfo
    {
        public DamageInfo(int amount, Vector2 sourcePosition, bool isHazard = false, float knockbackScale = 1f, bool ignoresArmor = false)
        {
            Amount = amount;
            SourcePosition = sourcePosition;
            IsHazard = isHazard;
            KnockbackScale = knockbackScale;
            IgnoresArmor = ignoresArmor;
        }

        public int Amount { get; }

        /// <summary>피해를 준 위치 (넉백 방향 계산용)</summary>
        public Vector2 SourcePosition { get; }

        /// <summary>가시·용암 같은 환경 피해인지. 플레이어는 마지막 안전한 발판으로 되돌아간다 (GDD 4.3)</summary>
        public bool IsHazard { get; }

        /// <summary>맞은 쪽 넉백 세기 배율 (1 = 기본). 브루트의 물기처럼 세게 밀어낼 때 키운다</summary>
        public float KnockbackScale { get; }

        /// <summary>정면 피해 감소 같은 방어를 무시하는지 (폭발 등 사격이 아닌 피해)</summary>
        public bool IgnoresArmor { get; }

        /// <summary>피해량만 바꾼 복사본.</summary>
        public DamageInfo WithAmount(int amount) => new(amount, SourcePosition, IsHazard, KnockbackScale, IgnoresArmor);
    }

    /// <summary>피해를 받을 수 있는 대상.</summary>
    public interface IDamageable
    {
        /// <summary>피해를 받았으면 true (무적 등으로 무시되면 false).</summary>
        bool TakeDamage(in DamageInfo damage);
    }

    /// <summary>
    /// Health와 같은 오브젝트에 붙어, 받는 피해량을 바꾼다 (예: 브루트의 정면 피해 감소).
    /// </summary>
    public interface IDamageModifier
    {
        int ModifyDamage(in DamageInfo damage);
    }
}
