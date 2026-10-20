using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 패럴랙스 배경 한 겹. 카메라 이동의 일부만큼 따라 움직여서 지형보다 멀리 있는 것처럼 보이게 한다.
    /// factor 0 = 지형과 같은 거리(고정), 1 = 카메라와 함께 움직임(무한히 멂).
    /// 카메라가 anchor에 있을 때 이 레이어도 anchor에 놓인다.
    /// </summary>
    [DefaultExecutionOrder(100)]   // CameraFollow가 카메라를 옮긴 뒤에 따라간다
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;
        [Tooltip("카메라를 따라가는 비율. 클수록 멀리 있어 보인다")]
        [Range(0f, 1f)]
        [SerializeField] float factor = 0.5f;
        [Tooltip("기준 위치 (보통 방의 중심)")]
        [SerializeField] Vector2 anchor;

        void LateUpdate()
        {
            if (cameraTransform == null) return;

            Vector2 offset = ((Vector2)cameraTransform.position - anchor) * factor;
            transform.position = new Vector3(anchor.x + offset.x, anchor.y + offset.y, transform.position.z);
        }
    }
}
