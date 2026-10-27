using System.Collections.Generic;
using Platformer.Enemies;
using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 사격 과열 게이지 (GDD 4.2). 탄약 대신 열로 사격을 제한한다.
    /// - 쏠 때마다 열이 오르고, 잠시 쏘지 않으면 식는다
    /// - 꽉 차면 과열: 일정 시간 사격 불가. 끝나면 열이 0이 된다
    /// - 과열 중 표시가 성공 구간에 있을 때 사격 키를 누르면 배출: 열이 즉시 0이 되고 주변 적을 밀쳐낸다.
    ///   구간 밖에서 누르면 과열 시간이 늘어나고 다시 시도할 수 없다
    /// </summary>
    public class PlayerHeat : MonoBehaviour
    {
        [SerializeField] PlayerCombatSettings settings;
        [Tooltip("충격파가 밀쳐내는 레이어")]
        [SerializeField] LayerMask shockwaveLayers;

        float lastHeatTime = float.NegativeInfinity;
        float overheatStartTime;
        float overheatEndTime;
        bool ventFailed;

        readonly List<Collider2D> shockwaveHits = new();
        ContactFilter2D shockwaveFilter;

        /// <summary>현재 열 (0 ~ 최대치)</summary>
        public float Heat { get; private set; }

        public float Ratio => Heat / settings.MaxHeat;

        public bool IsOverheated { get; private set; }

        /// <summary>과열 시간 중 지난 비율 0~1 (배출 타이밍 표시용). 실패해 늘어난 시간은 포함하지 않는다</summary>
        public float OverheatProgress =>
            IsOverheated ? Mathf.Clamp01((Time.time - overheatStartTime) / settings.OverheatTime) : 0f;

        public bool VentFailed => ventFailed;

        public float VentWindowStart => settings.VentWindowStart;
        public float VentWindowEnd => settings.VentWindowEnd;

        void Reset()
        {
            shockwaveLayers = LayerMask.GetMask("Enemy");
        }

        void Awake()
        {
            shockwaveFilter = new ContactFilter2D { useLayerMask = true, layerMask = shockwaveLayers, useTriggers = false };
        }

        void Update()
        {
            if (IsOverheated)
            {
                if (Time.time >= overheatEndTime)
                    EndOverheat();
                return;
            }

            if (Heat > 0f && Time.time - lastHeatTime >= settings.CoolDelay)
                Heat = Mathf.Max(0f, Heat - settings.CoolRate * Time.deltaTime);
        }

        /// <summary>열을 더한다. 최대치에 닿으면 과열된다.</summary>
        public void AddHeat(float amount)
        {
            if (IsOverheated) return;

            Heat = Mathf.Min(settings.MaxHeat, Heat + amount);
            lastHeatTime = Time.time;
            if (Heat >= settings.MaxHeat)
                BeginOverheat();
        }

        /// <summary>과열 중 배출을 시도한다. 성공하면 true.</summary>
        public bool TryVent()
        {
            if (!IsOverheated || ventFailed) return false;

            float progress = OverheatProgress;
            if (progress >= settings.VentWindowStart && progress <= settings.VentWindowEnd)
            {
                EndOverheat();
                Shockwave();
                return true;
            }

            ventFailed = true;
            overheatEndTime += settings.VentFailPenalty;
            return false;
        }

        void BeginOverheat()
        {
            IsOverheated = true;
            ventFailed = false;
            overheatStartTime = Time.time;
            overheatEndTime = Time.time + settings.OverheatTime;
        }

        void EndOverheat()
        {
            IsOverheated = false;
            ventFailed = false;
            Heat = 0f;
        }

        // 반경 안의 적을 플레이어 반대쪽으로 밀쳐내고 잠깐 멈춘다
        void Shockwave()
        {
            int count = Physics2D.OverlapCircle(transform.position, settings.ShockwaveRadius, shockwaveFilter, shockwaveHits);
            Vector2 push = settings.ShockwavePush;
            for (int i = 0; i < count; i++)
            {
                var enemy = shockwaveHits[i].GetComponentInParent<Enemy>();
                if (enemy == null) continue;

                float side = enemy.transform.position.x >= transform.position.x ? 1f : -1f;
                enemy.ApplyKnockback(new Vector2(side * push.x, push.y), settings.ShockwaveStun);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (settings == null) return;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, settings.ShockwaveRadius);
        }
    }
}
