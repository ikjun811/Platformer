using System;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 소탕 방(R1) 하나의 웨이브 구성 (GDD 5.2). 방마다 에셋을 하나씩 만든다.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomWaveData", menuName = "Platformer/Room Wave Data")]
    public class RoomWaveData : ScriptableObject
    {
        [Serializable]
        public class Spawn
        {
            [Tooltip("생성할 적 프리팹 (Prefabs/Enemies)")]
            [SerializeField] GameObject enemyPrefab;
            [Tooltip("방의 출현 지점 번호 (Room의 spawnPoints 순서, 0부터)")]
            [SerializeField] int spawnPoint;
            [Tooltip("웨이브 시작 후 이 시간이 지나면 생성한다 (초)")]
            [SerializeField] float delay;

            public GameObject EnemyPrefab => enemyPrefab;
            public int SpawnPoint => spawnPoint;
            public float Delay => delay;
        }

        [Serializable]
        public class Wave
        {
            [SerializeField] Spawn[] spawns = Array.Empty<Spawn>();
            [Tooltip("이 웨이브의 적이 모두 나온 뒤, 살아 있는 적이 이 수 이하가 되면 다음 웨이브를 시작한다")]
            [SerializeField, Min(0)] int advanceWhenRemaining;

            public Spawn[] Spawns => spawns;
            public int AdvanceWhenRemaining => advanceWhenRemaining;
        }

        [SerializeField] Wave[] waves = Array.Empty<Wave>();

        public Wave[] Waves => waves;
    }
}
