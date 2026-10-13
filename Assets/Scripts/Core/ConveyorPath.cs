using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using LoopMaster.Data;

namespace LoopMaster.Core
{
    /// <summary>
    /// Quỹ đạo băng chuyền, dựng từ <see cref="PathDTO"/> (dữ liệu quỹ đạo của 1 level) bằng gói
    /// com.unity.splines, và cho phép lấy vị trí thế giới tại một khoảng cách đã đi dọc theo quỹ đạo.
    ///
    /// Lưu ý quy ước dữ liệu: trong JSON gốc, "i"/"o" của mỗi điểm là TỌA ĐỘ TUYỆT ĐỐI của tay cầm
    /// tiếp tuyến (không phải offset tương đối), nên khi dựng <see cref="BezierKnot"/> phải trừ lại
    /// vị trí điểm neo (p) để ra offset tương đối đúng quy ước của Unity Splines.
    ///
    /// "Khép kín" theo GAME_DESIGN.md hiện được mô phỏng bằng cách LẶP (wrap) khoảng cách đã đi modulo
    /// tổng chiều dài đường — khối tới cuối đường (Exit) coi như nối liền lại đầu đường (Enter) —
    /// KHÔNG dựng hình học closed-loop thật, vì dữ liệu level 1 chỉ có 2 điểm Enter/Exit, không đủ để tạo
    /// một hình khép kín có ý nghĩa. Cần xác nhận lại giả định này nếu gặp level có quỹ đạo phức tạp hơn.
    /// </summary>
    public class ConveyorPath
    {
        private readonly Spline _spline;
        private readonly float _length;

        public float Length => _length;

        public ConveyorPath(PathDTO pathData)
        {
            _spline = BuildSpline(pathData);
            _length = _spline.GetLength();
        }

        private static Spline BuildSpline(PathDTO pathData)
        {
            var spline = new Spline();

            foreach (BezierPointDTO knotData in pathData.p)
            {
                float3 position = ToFloat3(knotData.p);
                float3 tangentIn = ToFloat3(knotData.i) - position;
                float3 tangentOut = ToFloat3(knotData.o) - position;

                spline.Add(new BezierKnot(position, tangentIn, tangentOut, quaternion.identity));
            }

            // Khép kín được mô phỏng bằng wrap khoảng cách (xem GetPointAtDistance), không phải hình học.
            spline.Closed = false;
            return spline;
        }

        private static float3 ToFloat3(float[] v)
        {
            return new float3(v[0], v[1], v[2]);
        }

        /// <summary>
        /// Trả về vị trí thế giới tại khoảng cách <paramref name="distanceFromStart"/> đã đi trên đường
        /// (tính từ điểm đầu), tự động lặp vòng (wrap) khi vượt quá tổng chiều dài đường.
        /// </summary>
        // public Vector3 GetPointAtDistance(float distanceFromStart)
        // {
        //     float wrapped = WrapDistance(distanceFromStart);
        //     SplineUtility.GetPointAtLinearDistance(_spline, 0f, wrapped, out float t);
        //     float3 pos = SplineUtility.EvaluatePosition(_spline, t);
        //     return new Vector3(pos.x, pos.y, pos.z);
        // }

        // private float WrapDistance(float distance)
        // {
        //     if (_length <= 0f)
        //     {
        //         return 0f;
        //     }

        //     float wrapped = distance % _length;
        //     return wrapped < 0f ? wrapped + _length : wrapped;
        // }

        // /// <summary>
        // /// Lấy mẫu <paramref name="sampleCount"/> + 1 điểm trải đều từ đầu tới cuối quỹ đạo (0..Length).
        // /// Dùng để vẽ đường (LineRenderer, Gizmo...) mà không cần mỗi nơi tự viết lại vòng lặp lấy mẫu.
        // /// </summary>
        // public Vector3[] Sample(int sampleCount)
        // {
        //     var points = new Vector3[sampleCount + 1];
        //     for (int i = 0; i <= sampleCount; i++)
        //     {
        //         float distance = _length * i / sampleCount;
        //         points[i] = GetPointAtDistance(distance);
        //     }

        //     return points;
        // }
        /// <summary>
/// Lấy vị trí thế giới tại khoảng cách đã đi, tự động wrap theo chu vi đường.
/// </summary>
public Vector3 GetPointAtDistance(float distanceFromStart)
{
    if (_length <= 0f) return Vector3.zero;

    // Chuyển đổi quãng đường đã đi thành tỷ lệ t từ 0.0 đến 1.0
    float t = (distanceFromStart % _length) / _length;
    if (t < 0f) t += 1f;

    // Lấy tọa độ trực tiếp từ Spline theo tỷ lệ t
    float3 pos = _spline.EvaluatePosition(t);
    return new Vector3(pos.x, pos.y, pos.z);
}

/// <summary>
/// Lấy mẫu đều các điểm từ đầu (t = 0) đến cuối (t = 1) để vẽ LineRenderer.
/// </summary>
public Vector3[] Sample(int sampleCount)
{
    var points = new Vector3[sampleCount + 1];
    for (int i = 0; i <= sampleCount; i++)
    {
        // t chạy đều đặn từ 0.0 đến 1.0
        float t = (float)i / sampleCount;
        float3 pos = _spline.EvaluatePosition(t);
        points[i] = new Vector3(pos.x, pos.y, pos.z);
    }

    return points;
}
    }
}
