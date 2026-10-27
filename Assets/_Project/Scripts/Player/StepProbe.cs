using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// 앞쪽의 낮은 턱(계단 한 칸 등)을 찾는 공용 판정. 걷기(PlayerMovement)와 대시(PlayerDash)가 함께 쓴다.
    /// </summary>
    static class StepProbe
    {
        /// <summary>판정이 바닥·벽과 겹쳐 시작하지 않도록 두는 여유 (유닛)</summary>
        public const float Skin = 0.02f;

        /// <summary>
        /// 몸 앞 distance 안에 높이 maxStep 이하의 턱이 있고, 한 단 위로는 막힘 없이 나아갈 수 있으면 true.
        /// faceDistance = 턱 면까지 거리, rise = 몸 바닥에서 턱 윗면까지 높이.
        /// </summary>
        public static bool Find(Bounds b, int direction, float distance, float maxStep, int layers,
            out float faceDistance, out float rise)
        {
            faceDistance = 0f;
            rise = 0f;
            var forward = new Vector2(direction, 0f);

            // 바닥 바로 위부터 턱 높이까지의 띠로 앞을 살핀다
            var footCenter = new Vector2(b.center.x, b.min.y + (maxStep + Skin) * 0.5f);
            var footSize = new Vector2(b.size.x - Skin, maxStep - Skin);
            RaycastHit2D wall = Physics2D.BoxCast(footCenter, footSize, 0f, forward, distance, layers);
            if (!wall || Mathf.Abs(wall.normal.x) < 0.7f) return false;

            // 한 단 올린 몸이 턱 위까지 나아갈 수 없으면 (벽, 낮은 천장) 턱이 아니다
            var raisedCenter = new Vector2(b.center.x, b.center.y + maxStep + Skin);
            var bodySize = new Vector2(b.size.x - Skin, b.size.y - Skin);
            if (Physics2D.BoxCast(raisedCenter, bodySize, 0f, forward, distance + Skin, layers)) return false;

            // 턱 윗면 높이를 잰다
            var probe = new Vector2(wall.point.x + direction * Skin, b.min.y + maxStep + Skin);
            RaycastHit2D top = Physics2D.Raycast(probe, Vector2.down, maxStep + Skin, layers);
            if (!top) return false;

            rise = top.point.y - b.min.y;
            if (rise <= 0f || rise > maxStep) return false;

            faceDistance = wall.distance;
            return true;
        }
    }
}
