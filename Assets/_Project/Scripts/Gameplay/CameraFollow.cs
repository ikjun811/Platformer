using Platformer.Player;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 플레이어를 따라가는 2D 카메라.
    /// 가로는 바라보는 방향 앞을 비추며 부드럽게 따라가고,
    /// 세로는 공중에서 데드존을 두어 점프할 때 화면이 덜 흔들리게 한다.
    /// 카메라가 보여주는 영역은 bounds 밖으로 나가지 않는다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] CameraSettings settings;
        [SerializeField] PlayerMovement target;
        [Tooltip("카메라가 보여줄 수 있는 월드 영역 (M3에서 방마다 자동 설정)")]
        [SerializeField] Rect bounds = new(-10f, -6f, 40f, 24f);

        Camera cam;
        float lookAhead;
        float lookAheadVelocity;
        float focusY;
        Vector2 velocity;

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        void Start()
        {
            SnapToTarget();
        }

        void LateUpdate()
        {
            if (target == null) return;

            Vector2 targetPosition = target.transform.position;
            Vector3 current = transform.position;

            float desiredLookAhead = target.FacingDirection * settings.LookAheadDistance;
            lookAhead = Mathf.SmoothDamp(lookAhead, desiredLookAhead, ref lookAheadVelocity, settings.LookAheadSmoothTime);
            float desiredX = targetPosition.x + lookAhead;

            UpdateFocusY(targetPosition.y + settings.VerticalOffset);

            float x = Mathf.SmoothDamp(current.x, desiredX, ref velocity.x, settings.HorizontalSmoothTime);
            float y = Mathf.SmoothDamp(current.y, focusY, ref velocity.y, settings.VerticalSmoothTime);

            Vector2 clamped = ClampToBounds(new Vector2(x, y));
            transform.position = new Vector3(clamped.x, clamped.y, current.z);
        }

        void UpdateFocusY(float playerY)
        {
            // 땅에 서 있으면 바로 맞추고, 공중에서는 데드존을 벗어난 만큼만 따라간다
            if (target.IsGrounded)
            {
                focusY = playerY;
                return;
            }

            float half = settings.VerticalDeadZone * 0.5f;
            if (playerY > focusY + half)
                focusY = playerY - half;
            else if (playerY < focusY - half)
                focusY = playerY + half;
        }

        Vector2 ClampToBounds(Vector2 position)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            return new Vector2(
                ClampAxis(position.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth),
                ClampAxis(position.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight));
        }

        // 영역이 화면보다 작으면 영역 중앙에 고정한다
        static float ClampAxis(float value, float min, float max) =>
            min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);

        /// <summary>보여줄 영역을 바꾼다 (방 전환 등).</summary>
        public void SetBounds(Rect newBounds) => bounds = newBounds;

        /// <summary>부드러운 이동 없이 즉시 목표 위치로 옮긴다.</summary>
        public void SnapToTarget()
        {
            if (target == null) return;

            Vector2 targetPosition = target.transform.position;
            lookAhead = target.FacingDirection * settings.LookAheadDistance;
            lookAheadVelocity = 0f;
            focusY = targetPosition.y + settings.VerticalOffset;
            velocity = Vector2.zero;

            Vector2 clamped = ClampToBounds(new Vector2(targetPosition.x + lookAhead, focusY));
            transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
