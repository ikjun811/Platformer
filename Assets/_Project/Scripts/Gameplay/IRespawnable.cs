using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 죽으면 정해진 위치에서 다시 시작하는 대상 (플레이어). 방이 시작될 때 방 입구를 부활 위치로 정한다 (GDD 7).
    /// </summary>
    public interface IRespawnable
    {
        /// <summary>부활 위치를 정한다 (발 위치, 월드 좌표).</summary>
        void SetRespawnPoint(Vector2 feet);
    }
}
