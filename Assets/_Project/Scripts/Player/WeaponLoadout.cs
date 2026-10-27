using System.Collections.Generic;
using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 플레이어가 쓸 수 있는 무기 모듈 목록 (GDD 4.3). Q를 누르면 이 순서대로 바뀐다.
    /// 1차 목표에서는 처음부터 모두 들어 있다. 획득식으로 바꿀 때는 이 목록에 추가하는 방식으로 쓴다.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponLoadout", menuName = "Platformer/Weapon Loadout")]
    public class WeaponLoadout : ScriptableObject
    {
        [SerializeField] WeaponModule[] modules;

        public IReadOnlyList<WeaponModule> Modules => modules;
    }
}
