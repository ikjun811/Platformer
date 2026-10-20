using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// 카메라 추적 수치. 단위는 유닛(= 타일)과 초.
    /// 플레이 중에 값을 바꾸면 바로 반영된다.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "Platformer/Camera Settings")]
    public class CameraSettings : ScriptableObject
    {
        [Header("가로")]
        [Tooltip("가로 추적이 목표에 도달하는 시간 (초)")]
        [SerializeField] float horizontalSmoothTime = 0.15f;
        [Tooltip("바라보는 방향으로 앞을 비추는 거리 (타일)")]
        [SerializeField] float lookAheadDistance = 2f;
        [Tooltip("방향을 바꿨을 때 앞 비추기가 따라오는 시간 (초)")]
        [SerializeField] float lookAheadSmoothTime = 0.4f;

        [Header("세로")]
        [Tooltip("공중에 있을 때 카메라가 움직이지 않는 세로 범위 (타일)")]
        [SerializeField] float verticalDeadZone = 2f;
        [Tooltip("세로 추적이 목표에 도달하는 시간 (초)")]
        [SerializeField] float verticalSmoothTime = 0.2f;
        [Tooltip("플레이어보다 이만큼 위를 화면 중심으로 삼는다 (타일)")]
        [SerializeField] float verticalOffset = 1f;

        public float HorizontalSmoothTime => horizontalSmoothTime;
        public float LookAheadDistance => lookAheadDistance;
        public float LookAheadSmoothTime => lookAheadSmoothTime;
        public float VerticalDeadZone => verticalDeadZone;
        public float VerticalSmoothTime => verticalSmoothTime;
        public float VerticalOffset => verticalOffset;
    }
}
