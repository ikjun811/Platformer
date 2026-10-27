using Platformer.Gameplay;
using UnityEngine;

namespace Platformer.Enemies
{
    /// <summary>
    /// E1 러너 (GDD 4.6). 플레이어를 보면 달려와서 문다.
    /// 상태: 대기 → 추격 → 웅크림(마지막 신호 시간 동안 하얗게 깜빡임 = 카운터 판정) → 튀어 물기 → 쉬기 → 추격 …
    /// 같은 층(높이 차이 안)에 있고 가로 거리 안이며 사이에 벽이 없을 때만 쫓는다. 낭떠러지와 벽 앞에서는 멈춘다.
    /// 경직되면 하던 공격을 끊고, 경직이 끝나면 쉬기부터 다시 시작한다.
    /// 그림 원본은 왼쪽을 보고 있으므로 오른쪽을 볼 때 좌우를 뒤집는다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Enemy))]
    public class EnemyRunner : MonoBehaviour
    {
        enum State { Idle, Chase, Windup, Lunge, Recover }

        // 벽·바닥 감지 거리 (타일)
        const float WallCheckDistance = 0.1f;
        const float LedgeCheckDistance = 0.3f;
        // 신호 깜빡임 속도 (초당 켜짐 횟수)
        const float SignalBlinkRate = 16f;

        [SerializeField] EnemyRunnerSettings settings;
        [Tooltip("벽과 바닥으로 볼 레이어")]
        [SerializeField] LayerMask groundLayers;
        [Tooltip("물기로 피해를 주는 레이어")]
        [SerializeField] LayerMask targetLayers;

        [Header("그림")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [Tooltip("피격 번쩍임·공격 신호용 흰 실루엣 (몸 그림 위에 겹친다)")]
        [SerializeField] SpriteRenderer overlayRenderer;
        [SerializeField] SheetClip idle = new();
        [SerializeField] SheetClip run = new();
        [Tooltip("0~1 웅크림, 2~3 튀어 물기, 4 마무리")]
        [SerializeField] SheetClip bite = new();
        [SerializeField] SheetClip die = new();

        Rigidbody2D body;
        BoxCollider2D box;
        Enemy enemy;
        Transform target;
        // 시야를 가리는 레이어 (얇은 발판은 시야를 가리지 않는다)
        int wallLayers;

        State state = State.Idle;
        float stateStartTime;
        int direction = -1;
        bool biteLanded;
        float deathTime = -1f;

        /// <summary>카운터 판정 시간인지 (물기 직전 신호가 깜빡이는 동안)</summary>
        public bool IsSignaling =>
            state == State.Windup && !enemy.IsDead && StateTime >= settings.WindupTime - settings.SignalTime;

        float StateTime => Time.time - stateStartTime;

        void Reset()
        {
            groundLayers = LayerMask.GetMask("Ground", "OneWay");
            targetLayers = LayerMask.GetMask("Player");
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            enemy = GetComponent<Enemy>();
            body.freezeRotation = true;
            wallLayers = groundLayers & ~LayerMask.GetMask("OneWay");

            foreach (SheetClip clip in new[] { idle, run, bite, die })
                clip.Build();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        void FixedUpdate()
        {
            if (enemy.IsDead) return;

            // 경직 중에는 밀려나는 속도를 물리에 맡기고, 끝나면 쉬기부터 다시 시작한다
            if (enemy.IsStunned)
            {
                if (state != State.Recover)
                    SetState(State.Recover);
                stateStartTime = Time.time;
                return;
            }

            switch (state)
            {
                case State.Idle:
                    Stop();
                    if (CanSeeTarget())
                        SetState(State.Chase);
                    break;

                case State.Chase:
                    UpdateChase();
                    break;

                case State.Windup:
                    Stop();
                    if (StateTime >= settings.WindupTime)
                        SetState(State.Lunge);
                    break;

                case State.Lunge:
                    UpdateLunge();
                    break;

                case State.Recover:
                    Stop();
                    if (StateTime >= settings.RecoverTime)
                        SetState(CanSeeTarget() ? State.Chase : State.Idle);
                    break;
            }
        }

        void LateUpdate()
        {
            UpdateVisual();
        }

        void SetState(State next)
        {
            state = next;
            stateStartTime = Time.time;
            if (next == State.Lunge)
                biteLanded = false;
        }

        void UpdateChase()
        {
            if (!CanSeeTarget())
            {
                SetState(State.Idle);
                return;
            }

            direction = target.position.x >= transform.position.x ? 1 : -1;
            if (FrontGap() <= settings.AttackRange)
            {
                SetState(State.Windup);
                return;
            }

            bool blocked = !IsGrounded() || IsWallOrLedgeAhead();
            SetHorizontalVelocity(blocked ? 0f : direction * settings.ChaseSpeed);
        }

        void UpdateLunge()
        {
            // 낭떠러지로는 뛰어들지 않는다
            SetHorizontalVelocity(IsWallOrLedgeAhead() ? 0f : direction * settings.LungeSpeed);

            if (!biteLanded)
                TryBite();

            if (StateTime >= settings.LungeTime)
                SetState(State.Recover);
        }

        // 몸 앞쪽 판정 안에 플레이어가 있으면 한 번만 문다
        void TryBite()
        {
            Bounds b = box.bounds;
            Vector2 size = settings.BiteSize;
            var center = new Vector2(direction > 0 ? b.max.x + size.x * 0.5f : b.min.x - size.x * 0.5f, b.center.y);
            Collider2D hit = Physics2D.OverlapBox(center, size, 0f, targetLayers);
            if (hit == null) return;

            var damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null) return;

            damageable.TakeDamage(new DamageInfo(settings.BiteDamage, transform.position));
            biteLanded = true;
        }

        bool CanSeeTarget()
        {
            if (target == null) return false;

            Vector2 from = box.bounds.center;
            Vector2 to = target.position;
            if (Mathf.Abs(to.x - from.x) > settings.DetectRange) return false;
            if (Mathf.Abs(to.y - from.y) > settings.DetectHeight) return false;

            // 사이에 벽이 있으면 보지 못한다
            return !Physics2D.Linecast(from, to, wallLayers);
        }

        // 몸 앞면과 플레이어 몸 사이의 가로 거리
        float FrontGap()
        {
            Bounds b = box.bounds;
            float front = direction > 0 ? b.max.x : b.min.x;
            return Mathf.Max(0f, Mathf.Abs(target.position.x - front) - 0.15f);
        }

        bool IsGrounded()
        {
            Bounds b = box.bounds;
            return Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.15f, groundLayers);
        }

        bool IsWallOrLedgeAhead()
        {
            Bounds b = box.bounds;
            float frontX = direction > 0 ? b.max.x : b.min.x;

            var wallOrigin = new Vector2(frontX, b.center.y);
            if (Physics2D.Raycast(wallOrigin, Vector2.right * direction, WallCheckDistance, groundLayers))
                return true;

            var ledgeOrigin = new Vector2(frontX + direction * WallCheckDistance, b.min.y + 0.05f);
            return !Physics2D.Raycast(ledgeOrigin, Vector2.down, LedgeCheckDistance, groundLayers);
        }

        void Stop() => SetHorizontalVelocity(0f);

        void SetHorizontalVelocity(float x) => body.linearVelocity = new Vector2(x, body.linearVelocity.y);

        // ───────── 그림 ─────────

        void UpdateVisual()
        {
            SheetClip clip;
            int frame;

            if (enemy.IsDead)
            {
                if (deathTime < 0f) deathTime = Time.time;
                clip = die;
                frame = die.FrameAt(Time.time - deathTime, false);
            }
            else
            {
                (clip, frame) = SelectFrame();
            }

            spriteRenderer.sprite = clip.Frame(frame);
            spriteRenderer.flipX = direction > 0;

            if (overlayRenderer == null) return;
            bool signalOn = IsSignaling && Mathf.Repeat(Time.time * SignalBlinkRate, 1f) < 0.5f;
            overlayRenderer.enabled = !enemy.IsDead && (enemy.IsHitFlashing || signalOn);
            overlayRenderer.sprite = clip.WhiteFrame(frame);
            overlayRenderer.flipX = spriteRenderer.flipX;
        }

        (SheetClip clip, int frame) SelectFrame()
        {
            float t = StateTime;
            switch (state)
            {
                case State.Chase:
                    return (run, run.FrameAt(t, true));
                case State.Windup:
                    // 신호 전까지 웅크리기 시작(0), 신호 동안 가장 웅크린 자세(1)
                    return (bite, IsSignaling ? 1 : 0);
                case State.Lunge:
                    return (bite, t < settings.LungeTime * 0.5f ? 2 : 3);
                case State.Recover:
                    if (t < 0.15f) return (bite, 4);
                    break;
            }
            return (idle, idle.FrameAt(Time.time, true));
        }

        void OnDrawGizmosSelected()
        {
            if (settings == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            Gizmos.DrawWireCube(transform.position, new Vector3(settings.DetectRange * 2f, settings.DetectHeight * 2f, 0f));
        }
    }
}
