using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 무기 모듈 하나의 수치 (GDD 4.3). 블래스터에 끼우는 모듈마다 에셋을 하나씩 만든다.
    /// 단위는 유닛(= 타일)과 초. 차지 샷은 모듈과 상관없이 공통이다 (PlayerCombatSettings).
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponModule", menuName = "Platformer/Weapon Module")]
    public class WeaponModule : ScriptableObject
    {
        [Tooltip("HUD 표시 이름 (영어, UI 폰트에 한글이 없다)")]
        [SerializeField] string displayName = "RAPID";
        [Tooltip("탄 색 (임시 그래픽). 현재 모듈 표시에도 쓴다")]
        [SerializeField] Color color = new(1f, 0.9f, 0.3f);

        [Header("사격")]
        [Tooltip("탄 한 발의 피해")]
        [SerializeField] int damage = 10;
        [Tooltip("연사 간격 (초)")]
        [SerializeField] float fireCooldown = 0.12f;
        [Tooltip("한 번 쏠 때 오르는 열 (탄 수와 상관없이 한 번)")]
        [SerializeField] float heat = 10f;
        [Tooltip("한 번에 나가는 탄 수")]
        [SerializeField, Min(1)] int projectileCount = 1;
        [Tooltip("탄이 여러 발일 때 위아래로 벌어지는 전체 각도 (도)")]
        [SerializeField] float spreadAngle;
        [Tooltip("적을 뚫고 지나간다 (벽에서는 멈춘다)")]
        [SerializeField] bool pierceEnemies;

        [Header("탄")]
        [Tooltip("탄 속도 (타일/초)")]
        [SerializeField] float speed = 14f;
        [Tooltip("사거리 (타일)")]
        [SerializeField] float range = 9f;
        [Tooltip("충돌 반지름 (타일)")]
        [SerializeField] float radius = 0.06f;
        [Tooltip("탄 길이 (타일, 임시 그래픽)")]
        [SerializeField] float length = 0.44f;
        [Tooltip("탄 두께 (타일, 임시 그래픽)")]
        [SerializeField] float thickness = 0.08f;

        public string DisplayName => displayName;
        public Color Color => color;
        public int Damage => damage;
        public float FireCooldown => fireCooldown;
        public float Heat => heat;
        public int ProjectileCount => projectileCount;
        public float SpreadAngle => spreadAngle;
        public bool PierceEnemies => pierceEnemies;
        public float Speed => speed;
        public float Range => range;
        public float Radius => radius;
        public float Length => length;
        public float Thickness => thickness;
    }
}
