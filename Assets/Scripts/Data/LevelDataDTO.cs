using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoopMaster.Data
{
    /// <summary>
    /// Data Transfer Object (DTO) chính chứa toàn bộ thông tin của một level.
    /// Tên biến được giữ nguyên (h, s, l...) để tương thích hoàn toàn với JsonUtility của Unity.
    /// </summary>
    [Serializable]
    public class LevelDataDTO
    {
        /// <summary>
        /// Phiên bản cấu trúc dữ liệu của file JSON (có ở các level sau).
        /// </summary>
        public int v;

        /// <summary>
        /// Cờ Hard Mode hoặc Hidden (0 = false, 1 = true).
        /// </summary>
        public int h;

        /// <summary>
        /// Kích thước Camera (Camera Size) - điều chỉnh độ zoom của camera trong màn chơi.
        /// </summary>
        public int s;

        /// <summary>
        /// Sức chứa tối đa của băng chuyền (Conveyor Capacity).
        /// Hiển thị trên giao diện dưới dạng 0/x (Ví dụ l=7 thì chứa tối đa 7 khối lớn tương đương 70 khối nhỏ).
        /// </summary>
        public int l;

        /// <summary>
        /// Tốc độ chạy của băng chuyền hoặc hệ số path scale (xuất hiện ở level 3).
        /// </summary>
        public float ps;

        /// <summary>
        /// Danh sách các khay chứa (Trays / Bins) trên bản đồ.
        /// </summary>
        public List<TrayDTO> b;

        /// <summary>
        /// Mảng quỹ đạo di chuyển của băng chuyền (Conveyor Path).
        /// </summary>
        public List<PathDTO> p;

        /// <summary>
        /// Danh sách các Decor (ảnh/vật thể trang trí) dán lên màn hình.
        /// (Ví dụ: Các nắp cổng Enter/Exit che 2 đầu đoạn băng chuyền hở).
        /// </summary>
        public List<DecorDTO> m;
    }

    [Serializable]
    public class TrayDTO
    {
        /// <summary>
        /// ID định danh của khay.
        /// </summary>
        public int i;

        /// <summary>
        /// Tọa độ không gian 3D của khay [x, y, z].
        /// </summary>
        public float[] p;

        /// <summary>
        /// Góc xoay 3D của khay [x, y, z].
        /// </summary>
        public float[] r;

        /// <summary>
        /// Danh sách các khối đang nằm trong khay.
        /// Hoạt động như một Stack (phần tử đầu tiên là khối nằm dưới đáy, phần tử cuối là khối trên cùng).
        /// </summary>
        public List<BlockDTO> c;

        /// <summary>
        /// Trạng thái khóa. 0 = Mở (nhận khối), 1 = Khóa (đã đầy 4 khối cùng màu).
        /// </summary>
        public int l;

        /// <summary>
        /// Trạng thái trống. 0 = Đang chứa khối, 1 = Trống rỗng.
        /// </summary>
        public int e;
    }

    [Serializable]
    public class BlockDTO
    {
        /// <summary>
        /// Mã ID màu sắc của khối. (Ví dụ: 1 = Đỏ, 2 = Xanh dương, 3 = Xanh lá).
        /// </summary>
        public int t;

        /// <summary>
        /// Trạng thái phụ của khối (0 = bình thường).
        /// </summary>
        public int s;

        /// <summary>
        /// Cờ đánh dấu các thuộc tính đặc biệt (như Mystery Block ẩn màu).
        /// </summary>
        public int k;
    }

    [Serializable]
    public class PathDTO
    {
        /// <summary>
        /// Đánh dấu loại quỹ đạo / đường dẫn. (0 = đường cong Bezier, vv...).
        /// </summary>
        public int c;

        /// <summary>
        /// Danh sách các điểm Bezier Spline vẽ nên quỹ đạo của băng chuyền.
        /// </summary>
        public List<BezierPointDTO> p;
    }

    [Serializable]
    public class BezierPointDTO
    {
        /// <summary>
        /// Tọa độ tâm của điểm node [x, y, z].
        /// </summary>
        public float[] p;

        /// <summary>
        /// Tọa độ tiếp tuyến đi vào (In-tangent) để uốn cong spline [x, y, z].
        /// </summary>
        public float[] i;

        /// <summary>
        /// Tọa độ tiếp tuyến đi ra (Out-tangent) để uốn cong spline [x, y, z].
        /// </summary>
        public float[] o;
    }

    [Serializable]
    public class DecorDTO
    {
        /// <summary>
        /// Tên của Decor (VD: "Enter", "Exit").
        /// </summary>
        public string n;

        /// <summary>
        /// Vị trí 3D [x, y, z].
        /// </summary>
        public float[] p;

        /// <summary>
        /// Góc xoay 3D [x, y, z].
        /// </summary>
        public float[] r;

        /// <summary>
        /// Kích thước (Scale) [x, y, z].
        /// </summary>
        public float[] s;
    }
}
