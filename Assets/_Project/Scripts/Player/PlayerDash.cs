using Platformer.Gameplay;
using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 페이즈 대시 (GDD 4.4).
    /// L을 누르면 바라보는 방향(이동 키를 누르고 있으면 그 방향)으로 짧은 거리를 매우 빠르게 이동한다.
    /// 대시 중에는 중력을 받지 않고, 회피 무적이며, 적의 몸을 통과한다. 벽은 통과하지 못한다.
    /// 대시가 끝날 때 적과 겹쳐 있으면 빠져나올 때까지 조금 더 이어서 간다 (큰 적을 한 번에 통과하도록).
    /// 연결 시간 안에 다시 누르면 최대 횟수까지 이어서 쓸 수 있고, 다 쓰거나 연결이 끊기면 재사용 대기 후 횟수가 모두 찬다.
    /// 대시 이동은 물리 속도 대신 매 물리 프레임 직접 옮긴다. 빠른 속도로 계단을 지나면 한 프레임에 턱을 여러 개 만나므로,
    /// 이동을 잘게 나눠 낮은 턱은 올라타고 벽에서는 멈춘다.
    /// PlayerMovement보다 늦게 실행되어, 같은 물리 프레임에 정한 이동을 대시로 덮어쓴다.
    /// </summary>
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerInputReader), typeof(Health))]
    public class PlayerDash : MonoBehaviour
    {
        // 잔상 오브젝트 수. 대시 시간 / 잔상 간격보다 넉넉해야 한다
        const int AfterimagePoolSize = 12;
        // 한 물리 프레임에 넘을 수 있는 최대 턱 수 (대시 한 프레임 이동 0.3타일 / 계단 폭 0.25타일보다 넉넉히)
        const int MaxStepsPerFrame = 4;
        const float Skin = StepProbe.Skin;

        [SerializeField] PlayerDashSettings settings;
        [Tooltip("잔상으로 복사할 플레이어 그림")]
        [SerializeField] SpriteRenderer visual;
        [Tooltip("대시 중 몸이 통과하는 레이어")]
        [SerializeField] LayerMask passThroughLayers;

        PlayerMovement movement;
        PlayerInputReader input;
        Health health;
        Rigidbody2D body;
        BoxCollider2D box;

        bool dashing;
        int direction;
        float dashEndTime;
        float remainingDistance;
        float exitRemaining;
        float lastPressedTime = float.NegativeInfinity;

        int chainCount;
        float lastDashEndTime = float.NegativeInfinity;
        bool coolingDown;
        float cooldownEndTime;
        LayerMask savedExcludeLayers;

        SpriteRenderer[] afterimages;
        float[] afterimageStartTimes;
        int nextAfterimage;
        float nextAfterimageTime;

        public bool IsDashing => dashing;

        /// <summary>지금 쓸 수 있는 남은 대시 횟수 (HUD용)</summary>
        public int RemainingCharges => coolingDown ? 0 : settings.MaxChain - chainCount;

        /// <summary>연속으로 쓸 수 있는 최대 횟수 (HUD용)</summary>
        public int MaxCharges => settings.MaxChain;

        void Reset()
        {
            passThroughLayers = LayerMask.GetMask("Enemy");
        }

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            input = GetComponent<PlayerInputReader>();
            health = GetComponent<Health>();
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            CreateAfterimages();
        }

        void OnDisable()
        {
            if (dashing) EndDash();
        }

        void OnDestroy()
        {
            if (afterimages == null) return;
            foreach (SpriteRenderer image in afterimages)
            {
                if (image != null) Destroy(image.gameObject);
            }
        }

        void Update()
        {
            if (input.DashPressedThisFrame)
                lastPressedTime = Time.time;

            RefreshCharges();
            FadeAfterimages();
        }

        void FixedUpdate()
        {
            if (dashing)
            {
                float step = settings.Speed * Time.fixedDeltaTime;
                if (remainingDistance > 0f && Time.time < dashEndTime)
                {
                    step = Mathf.Min(step, remainingDistance);
                    remainingDistance -= step;
                    DashMove(step);
                }
                else if (exitRemaining > 0f && OverlapsPassThrough())
                {
                    // 적 몸 안에서 끝나지 않도록 빠져나올 때까지 이어 가고, 그동안 무적을 유지한다
                    step = Mathf.Min(step, exitRemaining);
                    exitRemaining -= step;
                    DashMove(step);
                    health.SetEvading(Time.fixedDeltaTime + settings.EvadeGrace);
                }
                else
                {
                    // 거리를 다 갔거나, 벽에 막혀 시간이 다 됐고 적과 겹쳐 있지 않으면 끝낸다
                    EndDash();
                }
                return;
            }

            bool buffered = Time.time - lastPressedTime <= settings.InputBufferTime;
            if (buffered && CanDash())
            {
                lastPressedTime = float.NegativeInfinity;
                StartDash();
            }
        }

        void LateUpdate()
        {
            if (dashing && Time.time >= nextAfterimageTime)
            {
                SpawnAfterimage();
                nextAfterimageTime = Time.time + settings.AfterimageInterval;
            }
        }

        bool CanDash() =>
            !coolingDown && chainCount < settings.MaxChain && !movement.IsStunned && !movement.ControlLocked;

        // 연결 시간이 지나면 재사용 대기를 시작하고, 대기가 끝나면 횟수를 모두 채운다
        void RefreshCharges()
        {
            if (dashing) return;

            if (!coolingDown && chainCount > 0 && Time.time > lastDashEndTime + settings.ChainWindow)
                BeginCooldown(lastDashEndTime + settings.ChainWindow);

            if (coolingDown && Time.time >= cooldownEndTime)
            {
                coolingDown = false;
                chainCount = 0;
            }
        }

        void BeginCooldown(float startTime)
        {
            coolingDown = true;
            cooldownEndTime = startTime + settings.Cooldown;
        }

        void StartDash()
        {
            dashing = true;
            chainCount++;
            direction = input.Horizontal != 0 ? input.Horizontal : movement.FacingDirection;
            remainingDistance = settings.Distance;
            exitRemaining = settings.MaxExitDistance;
            // 시작 프레임도 이동하므로 시간 제한은 막혔을 때만 쓰인다
            dashEndTime = Time.time + settings.Duration + Time.fixedDeltaTime;

            movement.ControlLocked = true;
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;

            // 적의 몸을 통과한다 (이 몸에만 적용되므로 프로젝트 충돌 설정은 바뀌지 않는다)
            savedExcludeLayers = body.excludeLayers;
            body.excludeLayers = savedExcludeLayers | passThroughLayers;
            health.SetEvading(settings.Duration + settings.EvadeGrace);

            SpawnAfterimage();
            nextAfterimageTime = Time.time + settings.AfterimageInterval;

            float step = Mathf.Min(settings.Speed * Time.fixedDeltaTime, remainingDistance);
            remainingDistance -= step;
            DashMove(step);
        }

        bool OverlapsPassThrough()
        {
            Bounds b = box.bounds;
            return Physics2D.OverlapBox(b.center, b.size, 0f, passThroughLayers) != null;
        }

        // 이번 물리 프레임의 대시 거리(distance)를 나눠 이동한다: 막힘 없으면 끝까지, 낮은 턱이면 올라타고 계속, 벽이면 멈춘다.
        // 적은 단단한 지형 레이어가 아니므로 판정에 걸리지 않는다 (몸 충돌은 excludeLayers로 끈다).
        // 막혀서 못 간 거리는 남겨 두지 않는다 (막힌 채로 남은 거리를 채우려 하지 않도록)
        void DashMove(float distance)
        {
            int layers = movement.SolidLayers;
            float maxStep = movement.Settings.MaxStepHeight;
            var forward = new Vector2(direction, 0f);

            Bounds start = box.bounds;
            Vector2 offset = (Vector2)start.center - body.position;
            Vector2 size = start.size;
            Vector2 position = body.position;
            float remaining = distance;

            for (int i = 0; i < MaxStepsPerFrame && remaining > 0f; i++)
            {
                var b = new Bounds(position + offset, size);
                RaycastHit2D hit = Physics2D.BoxCast(b.center, size - Vector2.one * Skin, 0f, forward, remaining, layers);
                if (!hit)
                {
                    position.x += direction * remaining;
                    break;
                }

                // 막힌 곳 바로 앞까지 간다
                float move = Mathf.Max(0f, hit.distance - Skin);
                position.x += direction * move;
                remaining -= move;

                b = new Bounds(position + offset, size);
                if (!StepProbe.Find(b, direction, Skin * 3f, maxStep, layers, out _, out float rise))
                    break;
                position.y += rise + Skin;
            }

            body.position = position;
        }

        void EndDash()
        {
            dashing = false;
            lastDashEndTime = Time.time;
            body.excludeLayers = savedExcludeLayers;
            movement.ControlLocked = false;

            // 대시가 끝나면 달리기 최고 속도로 이어 달린다. 키를 떼고 있으면 PlayerMovement가 감속시킨다
            body.linearVelocity = new Vector2(direction * movement.Settings.MaxSpeed, 0f);

            if (chainCount >= settings.MaxChain)
                BeginCooldown(Time.time);
        }

        // ───────── 잔상 ─────────

        void CreateAfterimages()
        {
            afterimages = new SpriteRenderer[AfterimagePoolSize];
            afterimageStartTimes = new float[AfterimagePoolSize];
            for (int i = 0; i < AfterimagePoolSize; i++)
            {
                // 플레이어를 따라 움직이지 않도록 부모 없이 만든다
                var go = new GameObject("DashAfterimage");
                var image = go.AddComponent<SpriteRenderer>();
                image.enabled = false;
                afterimages[i] = image;
            }
        }

        void SpawnAfterimage()
        {
            if (visual == null || visual.sprite == null) return;

            SpriteRenderer image = afterimages[nextAfterimage];
            afterimageStartTimes[nextAfterimage] = Time.time;
            nextAfterimage = (nextAfterimage + 1) % afterimages.Length;

            image.transform.SetPositionAndRotation(visual.transform.position, visual.transform.rotation);
            image.transform.localScale = visual.transform.lossyScale;
            image.sprite = visual.sprite;
            image.flipX = visual.flipX;
            image.sortingLayerID = visual.sortingLayerID;
            // 플레이어 그림 바로 뒤에 그린다
            image.sortingOrder = visual.sortingOrder - 1;
            image.color = settings.AfterimageColor;
            image.enabled = true;
        }

        void FadeAfterimages()
        {
            Color baseColor = settings.AfterimageColor;
            for (int i = 0; i < afterimages.Length; i++)
            {
                SpriteRenderer image = afterimages[i];
                if (!image.enabled) continue;

                float t = (Time.time - afterimageStartTimes[i]) / settings.AfterimageLifetime;
                if (t >= 1f)
                {
                    image.enabled = false;
                    continue;
                }
                Color color = baseColor;
                color.a = baseColor.a * (1f - t);
                image.color = color;
            }
        }
    }
}
