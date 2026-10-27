using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 소탕 방 R1 (GDD 5.1, 5.2).
    /// 플레이어가 방 안쪽(시작 영역)에 들어오면 문을 모두 잠그고 웨이브를 차례로 진행한다.
    /// 웨이브의 적이 모두 나오고 살아 있는 적이 정해진 수 이하가 되면 다음 웨이브, 마지막 웨이브까지 다 처치하면 문을 연다.
    /// 적은 출현 지점(발 위치)에 프리팹으로 생성한다.
    /// 방이 시작되면 플레이어의 부활 위치를 방 입구로 정하고, 소탕 중 플레이어가 죽으면 방을 처음 상태로 되돌린다
    /// (웨이브 중단, 생성한 적 제거, 부서진 환경 요소 복구, 출입구 열림). 플레이어가 다시 들어오면 첫 웨이브부터 시작한다 (GDD 7).
    /// </summary>
    public class Room : MonoBehaviour
    {
        public enum State { Waiting, Fighting, Cleared }

        [SerializeField] RoomWaveData waveData;
        [Tooltip("방이 잠길 때 닫히고 클리어하면 열리는 문")]
        [SerializeField] RoomDoor[] doors;
        [Tooltip("적 출현 지점 (발 위치). 웨이브 데이터의 번호 순서")]
        [SerializeField] Transform[] spawnPoints;
        [Tooltip("플레이어가 이 영역(월드 좌표)에 들어오면 방이 시작된다. 문에서 충분히 안쪽으로 잡는다")]
        [SerializeField] Rect activationArea;
        [Tooltip("소탕 중 죽었을 때 다시 시작하는 위치 (발 위치, 방 입구 바깥)")]
        [SerializeField] Transform respawnPoint;
        [Tooltip("방이 초기화될 때 되살릴 환경 요소 (가스탱크, 파괴 상자)")]
        [SerializeField] Destructible[] props;

        Transform player;
        Health playerHealth;
        IRespawnable playerRespawn;
        int alive;
        readonly Dictionary<GameObject, float> spawnHeights = new();
        readonly List<GameObject> spawned = new();

        public State CurrentState { get; private set; } = State.Waiting;

        /// <summary>진행 중인 웨이브 번호 (0부터). 시작 전에는 -1</summary>
        public int CurrentWave { get; private set; } = -1;

        public int WaveCount => waveData != null ? waveData.Waves.Length : 0;

        public int AliveEnemies => alive;

        public event Action Started;
        public event Action Cleared;

        void Awake()
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found == null) return;

            player = found.transform;
            playerHealth = found.GetComponent<Health>();
            playerRespawn = found.GetComponent<IRespawnable>();
        }

        void OnEnable()
        {
            if (playerHealth != null)
                playerHealth.Died += OnPlayerDied;
        }

        void OnDisable()
        {
            if (playerHealth != null)
                playerHealth.Died -= OnPlayerDied;
        }

        void Update()
        {
            if (CurrentState != State.Waiting || player == null) return;
            if (activationArea.Contains((Vector2)player.position))
                Begin();
        }

        void Begin()
        {
            CurrentState = State.Fighting;
            SetDoorsOpen(false);
            if (playerRespawn != null && respawnPoint != null)
                playerRespawn.SetRespawnPoint(respawnPoint.position);
            Debug.Log($"[Room] {name} 시작 — 문 잠김, 웨이브 {WaveCount}개");
            Started?.Invoke();
            StartCoroutine(RunWaves());
        }

        IEnumerator RunWaves()
        {
            for (int i = 0; i < WaveCount; i++)
            {
                CurrentWave = i;
                RoomWaveData.Wave wave = waveData.Waves[i];
                Debug.Log($"[Room] 웨이브 {i + 1}/{WaveCount}");

                // 지연 시간 순서대로 생성한다
                var spawns = new List<RoomWaveData.Spawn>(wave.Spawns);
                spawns.Sort((a, b) => a.Delay.CompareTo(b.Delay));
                float waveStart = Time.time;
                foreach (RoomWaveData.Spawn spawn in spawns)
                {
                    while (Time.time < waveStart + spawn.Delay)
                        yield return null;
                    SpawnEnemy(spawn);
                }

                // 마지막 웨이브는 전부 처치해야 끝난다
                int threshold = i == WaveCount - 1 ? 0 : wave.AdvanceWhenRemaining;
                while (alive > threshold)
                    yield return null;
            }

            CurrentState = State.Cleared;
            SetDoorsOpen(true);
            Debug.Log($"[Room] {name} 클리어 — 문 열림");
            Cleared?.Invoke();
        }

        void SpawnEnemy(RoomWaveData.Spawn spawn)
        {
            if (spawn.EnemyPrefab == null || spawn.SpawnPoint < 0 || spawn.SpawnPoint >= spawnPoints.Length)
            {
                Debug.LogError($"[Room] {name} 웨이브 데이터 오류: 프리팹 없음 또는 출현 지점 {spawn.SpawnPoint} 없음");
                return;
            }

            // 출현 지점은 발 위치이므로 충돌체 절반 높이만큼 올려 세운다
            Vector2 feet = spawnPoints[spawn.SpawnPoint].position;
            Vector2 position = feet + new Vector2(0f, HalfHeight(spawn.EnemyPrefab));
            GameObject enemy = Instantiate(spawn.EnemyPrefab, position, Quaternion.identity);
            spawned.Add(enemy);

            var health = enemy.GetComponent<Health>();
            if (health == null) return;
            alive++;
            health.Died += () => alive--;
        }

        // 소탕 중 플레이어가 죽으면 방을 처음 상태로 되돌린다. 플레이어 이동은 부활 위치를 받은 쪽(IRespawnable)이 한다
        void OnPlayerDied()
        {
            if (CurrentState != State.Fighting) return;

            StopAllCoroutines();
            foreach (GameObject enemy in spawned)
            {
                if (enemy != null) Destroy(enemy);
            }
            spawned.Clear();
            foreach (Destructible prop in props)
            {
                if (prop != null) prop.Restore();
            }
            alive = 0;
            CurrentWave = -1;
            CurrentState = State.Waiting;
            SetDoorsOpen(true);
            Debug.Log($"[Room] {name} 실패 — 방 초기화, 입구에서 다시 시작");
        }

        float HalfHeight(GameObject prefab)
        {
            if (spawnHeights.TryGetValue(prefab, out float half)) return half;

            var box = prefab.GetComponent<BoxCollider2D>();
            half = box != null ? box.size.y * 0.5f + box.offset.y : 0f;
            spawnHeights.Add(prefab, half);
            return half;
        }

        void SetDoorsOpen(bool open)
        {
            foreach (RoomDoor door in doors)
            {
                if (door != null) door.SetOpen(open);
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(activationArea.center, activationArea.size);

            if (spawnPoints == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            foreach (Transform point in spawnPoints)
            {
                if (point != null) Gizmos.DrawWireSphere(point.position, 0.2f);
            }
        }
    }
}
