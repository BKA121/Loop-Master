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
/// Hướng tiếp tuyến (đã chuẩn hóa) của quỹ đạo tại khoảng cách đã đi, cùng tham số t với
/// <see cref="GetPointAtDistance"/> nên luôn khớp với vị trí trả về. Dùng để xoay vật thể theo khúc cua.
/// </summary>
public Vector3 GetTangentAtDistance(float distanceFromStart)
{
    if (_length <= 0f) return Vector3.forward;

    float t = (distanceFromStart % _length) / _length;
    if (t < 0f) t += 1f;

    float3 tangent = _spline.EvaluateTangent(t);
    var dir = new Vector3(tangent.x, tangent.y, tangent.z);
    return dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector3.forward;
}

/// <summary>
/// Điểm trên quỹ đạo gần <paramref name="position"/> nhất (đo trên mặt phẳng XZ). Trả về khoảng cách dọc đường
/// (cùng quy ước với <see cref="GetPointAtDistance"/>) của điểm đó; luôn có kết quả nên bao quát cả khi mũi tên
/// hướng cắt hay không cắt quỹ đạo. Độ chính xác phụ thuộc <paramref name="samples"/>.
/// </summary>
public float GetNearestDistance(Vector3 position, int samples = 400)
{
    if (_length <= 0f || samples < 1) return 0f;

    var p = new Vector2(position.x, position.z);
    Vector3[] pts = Sample(samples);
    float bestSqr = float.MaxValue;
    float bestT = 0f;

    for (int i = 0; i < samples; i++)
    {
        var a = new Vector2(pts[i].x, pts[i].z);
        var e = new Vector2(pts[i + 1].x, pts[i + 1].z) - a;

        float eSqr = e.sqrMagnitude;
        float k = eSqr > 1e-12f ? Mathf.Clamp01(Vector2.Dot(p - a, e) / eSqr) : 0f;
        float sqr = (a + e * k - p).sqrMagnitude;

        if (sqr < bestSqr)
        {
            bestSqr = sqr;
            bestT = (i + k) / samples;
        }
    }

    return bestT * _length;
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
