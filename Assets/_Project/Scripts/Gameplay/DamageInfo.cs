using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>피해 한 번의 정보.</summary>
    public readonly struct DamageInfo
    {
        public DamageInfo(int amount, Vector2 sourcePosition, bool isHazard = false)
        {
            Amount = amount;
            SourcePosition = sourcePosition;
            IsHazard = isHazard;
        }

        public int Amount { get; }

        /// <summary>피해를 준 위치 (넉백 방향 계산용)</summary>
        public Vector2 SourcePosition { get; }

        /// <summary>가시·용암 같은 환경 피해인지. 플레이어는 마지막 안전한 발판으로 되돌아간다 (GDD 4.3)</summary>
        public bool IsHazard { get; }
    }

    /// <summary>피해를 받을 수 있는 대상.</summary>
    public interface IDamageable
    {
        /// <summary>피해를 받았으면 true (무적 등으로 무시되면 false).</summary>
        bool TakeDamage(in DamageInfo damage);
    }
}
