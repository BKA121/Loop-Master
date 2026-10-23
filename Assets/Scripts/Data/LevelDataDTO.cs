using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoopMaster.Data
{
    /// <summary>
    /// Data Transfer Object (DTO) chính chứa toàn bộ thông tin của một level.
    /// Tên biến được giữ nguyên (v, h, s, l...) để tương thích hoàn toàn với JsonUtility của Unity.
    /// Ý nghĩa từng ký hiệu lấy theo tên dài gốc trong Level6 (ghi ở mỗi field: "Tên gốc").
    /// Level6 còn có "g" (Groups) và "c" (Covers): hiện luôn rỗng, chưa rõ kiểu phần tử nên chưa khai báo (JsonUtility bỏ qua).
    /// </summary>
    [Serializable]
    public class LevelDataDTO
    {
        /// <summary>
        /// Tên gốc: Version. Phiên bản cấu trúc dữ liệu của file JSON.
        /// </summary>
        public int v;

        /// <summary>
        /// Tên gốc: HardLevel. Độ khó của level (0 = thường).
        /// </summary>
        public int h;

        /// <summary>
        /// Tên gốc: Size. Kích thước Camera (Camera Size) - điều chỉnh độ zoom của camera trong màn chơi.
        /// Là orthographicSize theo chiều dọc ở tỉ lệ tham chiếu; GameViewManager.ApplyCameraSize tự nới ra
        /// khi màn hình hẹp hơn tỉ lệ đó để luôn thấy đủ bề ngang.
        /// </summary>
        public int s;

        /// <summary>
        /// Tên gốc: PathLength. Code đang dùng làm sức chứa tối đa của băng chuyền (Conveyor Capacity):
        /// hiển thị 0/x (Ví dụ l=7 thì chứa tối đa 7 khối lớn tương đương 70 khối nhỏ).
        /// Lưu ý: nghĩa gốc của tên là "PathLength"; chưa xác nhận đây đúng là sức chứa.
        /// </summary>
        public int l;

        /// <summary>
        /// Tên gốc: PathSectionDistance. Path space: khoảng cách giữa các sample của quỹ đạo (KHÔNG phải tốc độ; level data không có tốc độ băng chuyền).
        /// </summary>
        public float ps;

        /// <summary>
        /// Tên gốc: Boxes. Danh sách các khay chứa (Trays / Bins) trên bản đồ.
        /// </summary>
        public List<TrayDTO> b;

        /// <summary>
        /// Tên gốc: Paths. Mảng quỹ đạo di chuyển của băng chuyền (Conveyor Path).
        /// </summary>
        public List<PathDTO> p;

        /// <summary>
        /// Tên gốc: MapData. Danh sách các Decor (ảnh/vật thể trang trí) dán lên màn hình.
        /// (Ví dụ: Các nắp cổng Enter/Exit che 2 đầu đoạn băng chuyền hở).
        /// </summary>
        public List<DecorDTO> m;
    }

    [Serializable]
    public class TrayDTO
    {
        /// <summary>
        /// Tên gốc: BoxType (0 = khay thường).
        /// </summary>
        public int t;

        /// <summary>
        /// Tên gốc: PathIndex. Chỉ số vị trí trên quỹ đạo (không phải ID duy nhất); hiện chỉ dùng làm Id/tên "Tray_i" của khay.
        /// </summary>
        public int i;

        /// <summary>
        /// Tên gốc: PathIndexV2 (0 = mặc định).
        /// </summary>
        public int iv2;

        /// <summary>
        /// Tên gốc: Position. Tọa độ không gian 3D của khay [x, y, z].
        /// </summary>
        public float[] p;

        /// <summary>
        /// Tên gốc: Rotation. Góc xoay 3D của khay [x, y, z].
        /// </summary>
        public float[] r;

        /// <summary>
        /// Tên gốc: Cubes. Danh sách các khối đang nằm trong khay.
        /// Hoạt động như một Stack (phần tử đầu tiên là khối nằm dưới đáy, phần tử cuối là khối trên cùng).
        /// </summary>
        public List<BlockDTO> c;

        /// <summary>
        /// Tên gốc: LockColor (0 = không khóa theo màu).
        /// </summary>
        public int lc;

        /// <summary>
        /// Tên gốc: IsEnterOnly (1 = khay chỉ nhận khối vào). Chưa dùng trong code.
        /// </summary>
        public int e;

        /// <summary>
        /// Tên gốc: NumLock (0 = không khóa).
        /// </summary>
        public int n;

        /// <summary>
        /// Tên gốc: LimitedColor. Level6 gốc là một số int; ở file đã đổi thành mảng (như Level1–5), rỗng = không giới hạn màu.
        /// </summary>
        public List<int> lci;

        /// <summary>
        /// Tên gốc: IsAdjacentLocked (0 = không).
        /// </summary>
        public int al;

        /// <summary>
        /// KHÔNG có trong Level6 (chỉ có ở Level1–5, thêm vào Level6 với giá trị 0).
        /// Trạng thái khóa ban đầu: 0 = Mở (nhận khối), 1 = Khóa. TrayModel.FromDto dùng làm khay khóa lúc dựng.
        /// </summary>
        public int l;
    }

    [Serializable]
    public class BlockDTO
    {
        /// <summary>
        /// Tên gốc: Type. Mã ID màu sắc của khối. (Ví dụ: 1 = Đỏ, 2 = Xanh dương, 3 = Xanh lá).
        /// </summary>
        public int t;

        /// <summary>
        /// Tên gốc: IsSecret. 1 = khối bí ẩn (Mystery Block, ẩn màu), 0 = bình thường.
        /// </summary>
        public int s;

        /// <summary>
        /// Tên gốc: GroupKeyId. Mã nhóm khóa/chìa của khối (0 = không thuộc nhóm).
        /// Level1–5 còn có "l" và "f" trong khối; Level6 không có nên chưa khai báo.
        /// </summary>
        public int k;
    }

    [Serializable]
    public class PathDTO
    {
        /// <summary>
        /// Tên gốc: IsCircle. 1 = quỹ đạo là vòng kín, 0 = quỹ đạo hở.
        /// </summary>
        public int c;

        /// <summary>
        /// Tên gốc: ControlPoints. Danh sách các điểm Bezier Spline vẽ nên quỹ đạo của băng chuyền.
        /// </summary>
        public List<BezierPointDTO> p;
    }

    [Serializable]
    public class BezierPointDTO
    {
        /// <summary>
        /// Tên gốc: Position. Tọa độ tâm của điểm node [x, y, z].
        /// </summary>
        public float[] p;

        /// <summary>
        /// Tên gốc: InTangent. Tọa độ tiếp tuyến đi vào (In-tangent) để uốn cong spline [x, y, z].
        /// </summary>
        public float[] i;

        /// <summary>
        /// Tên gốc: OutTangent. Tọa độ tiếp tuyến đi ra (Out-tangent) để uốn cong spline [x, y, z].
        /// </summary>
        public float[] o;
    }

    [Serializable]
    public class DecorDTO
    {
        /// <summary>
        /// Tên gốc: Name. Tên của Decor (VD: "Decor1", "Enter", "Exit").
        /// </summary>
        public string n;

        /// <summary>
        /// Tên gốc: Position. Vị trí 3D [x, y, z].
        /// </summary>
        public float[] p;

        /// <summary>
        /// Tên gốc: Rotation. Góc xoay 3D [x, y, z].
        /// </summary>
        public float[] r;

        /// <summary>
        /// Tên gốc: Scale. Kích thước (Scale) [x, y, z].
        /// </summary>
        public float[] s;
    }
}
