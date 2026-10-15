using UnityEngine;
using LoopMaster.Core;

namespace LoopMaster.View
{
    /// <summary>
    /// Vẽ hình dạng quỹ đạo băng chuyền ra màn hình (Game view/build, không chỉ Scene view như Gizmo)
    /// bằng <see cref="LineRenderer"/>, lấy mẫu điểm từ <see cref="ConveyorPath"/>.
    ///
    /// KHÔNG tự load level và không tự dựng ConveyorPath — chỉ nhận qua <see cref="Initialize"/>,
    /// do <see cref="GameViewManager"/> gọi, cùng cách với <see cref="ConveyorView"/>.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ConveyorLineView : MonoBehaviour
    {
        [SerializeField, Min(2), Tooltip("Số đoạn chia nhỏ quỹ đạo để lấy mẫu vẽ đường — càng lớn càng mượt, nhất là ở các góc cua gấp")]
        private int sampleCount = 200;

        // Bán kính cong nhỏ nhất đo được trên dữ liệu quỹ đạo hiện tại (level p542439, các góc cua ở khu
        // vực lõm Enter/Exit phía trên) là ~0.88 đơn vị. Width PHẢI nhỏ hơn 2 lần bán kính đó (~1.76),
        // nếu không mép trong của LineRenderer sẽ tự chồng lên nhau ở góc cua gấp nhất — đây là giới hạn
        // hình học, KHÔNG sửa được bằng cách tăng numCornerVertices/sampleCount. 1.0 chừa biên an toàn
        // (bán kính dải = 0.5 < 0.88) để nhìn dày như yêu cầu mà vẫn không bị khía ở góc hẹp nhất.
        [SerializeField, Tooltip("Bề rộng dải băng chuyền. Phải nhỏ hơn 2x bán kính cong nhỏ nhất của quỹ đạo " +
            "(~1.76 với level hiện tại), nếu không mép trong sẽ tự chồng lên nhau ở góc cua gấp (nham nhở).")]
        private float width = 1f;

        private LineRenderer _lineRenderer;

        private void InitLineRender()
        {
            _lineRenderer = GetComponent<LineRenderer>();

            // "Khép kín" của GAME_DESIGN.md hiện là wrap khoảng cách (xem ghi chú ở ConveyorPath),
            // không phải hình học khép kín thật — nhưng vì điểm đầu/cuối lấy mẫu trùng nhau khi quỹ đạo
            // thật sự khép kín (ví dụ level p542439), đường vẽ vẫn tự nối liền, không cần loop = true.
            _lineRenderer.loop = false;

            // Ribbon quay mặt theo camera (billboard) — phù hợp với camera nhìn từ trên xuống thẳng
            // (TopCamera), không bị xoắn giữa các đoạn vì mọi quad đều quay cùng 1 hướng.
            _lineRenderer.alignment = LineAlignment.View;

            // numCornerVertices: bo tròn mép NGOÀI tại chỗ nối giữa 2 đoạn, tránh tạo gai nhọn (miter spike).
            // Đặt cao (8) vì quỹ đạo có nhiều góc cua gấp (xem ConveyorLineView vs LineRenderer ở lượt trước).
            _lineRenderer.numCornerVertices = 8;
            _lineRenderer.numCapVertices = 8;

            // Width nhỏ hơn hẳn bán kính cong giúp mép TRONG không tự chồng lên nhau ở góc cua gấp —
            // đây là nguyên nhân chính gây "nham nhở". Vẫn chỉnh được qua Inspector nếu cần dải rộng hơn.
            _lineRenderer.startWidth = width;
            _lineRenderer.endWidth = width;
        }

        /// <summary>
        /// Nhận quỹ đạo đã dựng sẵn và vẽ ngay. Gọi 1 lần khi khởi tạo.
        /// </summary>
        public void Initialize(ConveyorPath path)
        {
            if (path == null)
            {
                Debug.LogError("[ConveyorLineView] ConveyorPath null, không thể vẽ quỹ đạo.");
                return;
            }
            InitLineRender();

            Vector3[] points = path.Sample(sampleCount);
            _lineRenderer.positionCount = points.Length;
            _lineRenderer.SetPositions(points);
        }
    }
}
