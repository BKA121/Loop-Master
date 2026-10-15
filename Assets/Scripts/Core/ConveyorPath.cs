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
/// Bắn tia từ <paramref name="origin"/> theo <paramref name="direction"/> (chiếu xuống mặt phẳng XZ) và tìm
/// giao điểm gần nhất phía trước với quỹ đạo. Trả về khoảng cách dọc đường (cùng quy ước với
/// <see cref="GetPointAtDistance"/>) của giao điểm đó. Độ chính xác phụ thuộc <paramref name="samples"/>.
/// </summary>
public bool TryGetDistanceAlongRay(Vector3 origin, Vector3 direction, out float distance, int samples = 400)
{
    distance = 0f;
    if (_length <= 0f || samples < 1) return false;

    var o = new Vector2(origin.x, origin.z);
    var d = new Vector2(direction.x, direction.z);
    if (d.sqrMagnitude < 1e-8f) return false;

    Vector3[] pts = Sample(samples);
    float bestS = float.MaxValue;
    float bestT = 0f;

    for (int i = 0; i < samples; i++)
    {
        var a = new Vector2(pts[i].x, pts[i].z);
        var e = new Vector2(pts[i + 1].x, pts[i + 1].z) - a;

        float denom = Cross(d, e);
        if (Mathf.Abs(denom) < 1e-8f) continue; // song song

        Vector2 ao = a - o;
        float s = Cross(ao, e) / denom; // tham số trên tia
        float k = Cross(ao, d) / denom; // tham số trên đoạn [0,1]

        if (s >= 0f && k >= 0f && k <= 1f && s < bestS)
        {
            bestS = s;
            bestT = (i + k) / samples;
        }
    }

    if (bestS == float.MaxValue) return false;

    distance = bestT * _length;
    return true;
}

private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

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
