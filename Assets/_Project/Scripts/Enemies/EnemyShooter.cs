using Platformer.Gameplay;
using UnityEngine;

namespace Platformer.Enemies
{
    /// <summary>
    /// E5 감염 병사 (GDD 4.6). 같은 층의 플레이어를 보면 사거리까지 다가가 멈추고, 겨눈 뒤 수평으로 이어 쏜다.
    /// 상태: 대기 → 접근 → 겨눔 → 사격(연사) → 쉬기 → (계속 보이면) 겨눔 …
    /// 다른 층으로 피하거나 사이에 벽이 있으면 쏘지 않는다. 낭떠러지와 벽 앞에서는 멈춘다. 카운터되지 않는다.
    /// 경직(충격파)되면 하던 사격을 끊고 쉬기부터 다시 시작한다.
    /// 그림 원본(해병)은 오른쪽을 보고 있으므로 왼쪽을 볼 때 좌우를 뒤집는다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Enemy))]
    public class EnemyShooter : MonoBehaviour
    {
        enum State { Idle, Approach, Aim, Fire, Cooldown }

        // 벽·바닥 감지 거리 (타일)
        const float WallCheckDistance = 0.1f;
        const float LedgeCheckDistance = 0.3f;
        // 쏜 직후 사격 자세(두 번째 프레임)를 보여주는 시간 (초)
        const float ShotPoseTime = 0.08f;

        [SerializeField] EnemyShooterSettings settings;
        [Tooltip("벽과 바닥으로 볼 레이어")]
        [SerializeField] LayerMask groundLayers;
        [Tooltip("탄이 부딪히는 레이어 (지형, 플레이어)")]
        [SerializeField] LayerMask bulletHitLayers;
        [Tooltip("임시 탄 스프라이트")]
        [SerializeField] Sprite bulletSprite;

        [Header("그림")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [Tooltip("피격 번쩍임용 흰 실루엣 (몸 그림 위에 겹친다)")]
        [SerializeField] SpriteRenderer overlayRenderer;
        [SerializeField] SheetClip idle = new();
        [SerializeField] SheetClip run = new();
        [Tooltip("0 겨눔, 1 쏘는 순간")]
        [SerializeField] SheetClip shoot = new();
        [SerializeField] SheetClip die = new();

        Rigidbody2D body;
        BoxCollider2D box;
        Enemy enemy;
        Transform target;
        int wallLayers;
        int bulletLayer;

        State state = State.Idle;
        float stateStartTime;
        int direction = -1;
        int shotsFired;
        float nextShotTime;
        float lastShotTime = float.NegativeInfinity;
        float deathTime = -1f;

        float StateTime => Time.time - stateStartTime;

        void Reset()
        {
            groundLayers = LayerMask.GetMask("Ground", "OneWay");
            bulletHitLayers = LayerMask.GetMask("Ground", "Player");
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            enemy = GetComponent<Enemy>();
            body.freezeRotation = true;
            enemy.FacingDirection = direction;
            wallLayers = groundLayers & ~LayerMask.GetMask("OneWay");
            bulletLayer = LayerMask.NameToLayer("EnemyProjectile");

            foreach (SheetClip clip in new[] { idle, run, shoot, die })
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
                if (state != State.Cooldown)
                    SetState(State.Cooldown);
                stateStartTime = Time.time;
                return;
            }

            bool sees = CanSeeTarget();
            if (sees)
                Face(target.position.x);

            switch (state)
            {
                case State.Idle:
                    Stop();
                    if (sees)
                        SetState(State.Approach);
                    break;

                case State.Approach:
                    if (!sees)
                    {
                        SetState(State.Idle);
                        break;
                    }
                    if (Mathf.Abs(target.position.x - transform.position.x) <= settings.FireRange || IsWallOrLedgeAhead())
                    {
                        Stop();
                        SetState(State.Aim);
                        break;
                    }
                    SetHorizontalVelocity(IsGrounded() ? direction * settings.WalkSpeed : 0f);
                    break;

                case State.Aim:
                    Stop();
                    if (!sees)
                        SetState(State.Idle);
                    else if (StateTime >= settings.AimTime)
                    {
                        SetState(State.Fire);
                        shotsFired = 0;
                        nextShotTime = Time.time;
                    }
                    break;

                case State.Fire:
                    Stop();
                    if (Time.time >= nextShotTime)
                    {
                        FireBullet();
                        shotsFired++;
                        nextShotTime = Time.time + settings.BurstInterval;
                        if (shotsFired >= settings.BurstCount)
                            SetState(State.Cooldown);
                    }
                    break;

                case State.Cooldown:
                    Stop();
                    if (StateTime >= settings.CooldownTime)
                        SetState(sees ? State.Approach : State.Idle);
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
        }

        void Face(float targetX)
        {
            direction = targetX >= transform.position.x ? 1 : -1;
            enemy.FacingDirection = direction;
        }

        void FireBullet()
        {
            Bounds b = box.bounds;
            var forward = new Vector2(direction, 0f);
            var muzzle = new Vector2(b.center.x, b.max.y - settings.ShoulderDepth) + forward * settings.MuzzleDistance;

            // 쏘는 빈도가 낮아 풀 없이 만들고, 맞거나 사거리가 끝나면 지운다
            var go = new GameObject("EnemyBullet") { layer = bulletLayer };
            Vector2 size = settings.BulletSize;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = bulletSprite;
            sprite.color = settings.BulletColor;
            sprite.sortingLayerName = "Projectiles";

            var bullet = go.AddComponent<Projectile>();
            bullet.Init(p => Destroy(p.gameObject));
            bullet.Launch(muzzle, forward, settings.BulletSpeed, settings.BulletRange, settings.BulletRadius,
                settings.BulletDamage, bulletHitLayers);
            lastShotTime = Time.time;
        }

        bool CanSeeTarget()
        {
            if (target == null) return false;

            Vector2 from = box.bounds.center;
            Vector2 to = target.position;
            if (Mathf.Abs(to.x - from.x) > settings.DetectRange) return false;
            if (Mathf.Abs(to.y - from.y) > settings.DetectHeight) return false;
            return !Physics2D.Linecast(from, to, wallLayers);
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
            spriteRenderer.flipX = direction < 0;

            if (overlayRenderer == null) return;
            overlayRenderer.enabled = !enemy.IsDead && enemy.IsHitFlashing;
            overlayRenderer.sprite = clip.WhiteFrame(frame);
            overlayRenderer.flipX = spriteRenderer.flipX;
        }

        (SheetClip clip, int frame) SelectFrame()
        {
            switch (state)
            {
                case State.Approach:
                    return (run, run.FrameAt(StateTime, true));
                case State.Aim:
                    return (shoot, 0);
                case State.Fire:
                    return (shoot, Time.time - lastShotTime < ShotPoseTime ? 1 : 0);
            }
            return (idle, idle.FrameAt(Time.time, true));
        }

        void OnDrawGizmosSelected()
        {
            if (settings == null) return;
            Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.5f);
            Gizmos.DrawWireCube(transform.position, new Vector3(settings.DetectRange * 2f, settings.DetectHeight * 2f, 0f));
        }
    }
}
