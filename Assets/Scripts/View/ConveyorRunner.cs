using UnityEngine;
using LoopMaster.Core;

namespace LoopMaster.View
{
    /// <summary>
    /// Hiển thị/di chuyển 1 Transform placeholder (runner) dọc theo quỹ đạo băng chuyền.
    ///
    /// KHÔNG tự gọi LevelLoader và không tự dựng ConveyorPath — chỉ nhận một ConveyorPath đã dựng sẵn
    /// qua <see cref="Initialize"/>, do một lớp quản lý view (ví dụ <see cref="GameViewManager"/>) gọi,
    /// để tách rõ "ai load level & dựng quỹ đạo" (manager) khỏi "ai vận hành theo quỹ đạo đó" (view này).
    /// </summary>
    public class ConveyorRunner : MonoBehaviour
    {
        [SerializeField] private Transform runner;
        [SerializeField, Tooltip("0 = dùng tốc độ truyền vào qua Initialize")]
        private float speedOverride = 0f;

        private ConveyorPath _path;
        private float _traveledDistance;
        private float _speed;

        /// <summary>
        /// Nhận quỹ đạo + tốc độ. Gọi 1 lần khi khởi tạo, trước khi băng chuyền được phép chạy
        /// (Update sẽ không làm gì nếu chưa gọi hàm này).
        /// </summary>
        public void Initialize(ConveyorPath path, float speed)
        {
            _path = path;
            _speed = speedOverride > 0f ? speedOverride : speed;
            _traveledDistance = 0f;
        }

        private void Update()
        {
            if (_path == null || runner == null)
            {
                return;
            }

            _traveledDistance += _speed * Time.deltaTime;
            runner.position = _path.GetPointAtDistance(_traveledDistance);
        }

        private void OnDrawGizmos()
        {
            // Chỉ vẽ được sau khi Initialize() đã chạy (ví dụ: đang Play, hoặc GameViewManager chạy ở edit mode).
            if (_path == null)
            {
                return;
            }

            Vector3[] points = _path.Sample(50);
            Gizmos.color = Color.yellow;
            for (int i = 1; i < points.Length; i++)
            {
                Gizmos.DrawLine(points[i - 1], points[i]);
            }
        }
    }
}
