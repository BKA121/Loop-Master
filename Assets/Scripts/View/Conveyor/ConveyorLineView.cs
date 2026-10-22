using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using LoopMaster.Core;

namespace LoopMaster.View
{
    /// <summary>
    /// Vẽ băng chuyền ra màn hình (Game view/build, không chỉ Scene view như Gizmo), lấy mẫu điểm từ
    /// <see cref="ConveyorPath"/>, bằng 1 mesh dựng bằng code gồm 2 submesh (giống prefab GameRoad gốc):
    /// - Submesh 0 — road: mặt đường phẳng.
    /// - Submesh 1 — wall: 2 gờ viền (trong + ngoài) nhô lên sát hai mép mặt đường.
    /// Mesh được tạo bằng cách quét một tiết diện 2D cố định (<see cref="ProfileSide"/>/<see cref="ProfileHeight"/>)
    /// dọc quỹ đạo, nên đường và gờ cùng một nguồn hình học và luôn khớp nhau. Phía trong khúc cua, từng điểm
    /// tiết diện được kẹp theo bán kính cong cục bộ để mesh không gấp mép.
    ///
    /// KHÔNG tự load level và không tự dựng ConveyorPath — chỉ nhận qua <see cref="Initialize"/>,
    /// do <see cref="GameViewManager"/> gọi, cùng cách với <see cref="ConveyorView"/>.
    /// </summary>
    public class ConveyorLineView : MonoBehaviour
    {
        [SerializeField, Min(2), Tooltip("Số đoạn chia nhỏ quỹ đạo để lấy mẫu dựng mesh — càng lớn càng mượt, nhất là ở các góc cua gấp")]
        private int sampleCount = 200;

        [Header("Material")]
        [SerializeField, Tooltip("Material của mặt đường (ví dụ Track_path_road). Để trống = không vẽ mặt đường")]
        private Material roadMaterial;
        [SerializeField, Tooltip("Material của gờ viền (ví dụ Track_path_wall). Để trống = không vẽ gờ")]
        private Material wallMaterial;

        [Header("Tiết diện (profile)")]
        // Tiết diện gốc rộng 1.0 (Side từ -0.5 đến 0.5). Bán kính cong nhỏ nhất đo được trên level p542439 là
        // ~0.88 đơn vị; nửa bề rộng ngoài cùng = 0.5 * profileScale. Phía trong khúc cua mọi điểm tiết diện luôn
        // được kẹp theo bán kính cong cục bộ nên mesh không gấp mép, nhưng nếu vượt bán kính thì gờ bị ép hẹp ở góc đó.
        [SerializeField, Min(0.01f), Tooltip("Hệ số nhân theo chiều ngang của tiết diện (bề rộng tổng = 1.0 * hệ số). " +
            "Nửa bề rộng ngoài cùng (0.5 * hệ số) nên nhỏ hơn bán kính cong nhỏ nhất của quỹ đạo (~0.88 với level p542439).")]
        private float profileScale = 1.3f;
        [SerializeField, Min(0.01f), Tooltip("Hệ số nhân theo chiều cao của tiết diện (độ cao gờ, độ sâu chân gờ)")]
        private float heightScale = 1f;
        [SerializeField, Tooltip("Dịch cả tiết diện theo trục y. 0 = mặt đường nằm đúng độ cao quỹ đạo; 0.1 = đúng số thô của tiết diện gốc " +
            "(mặt đường cao hơn quỹ đạo 0.1, khối/slice có thể bị chìm)")]
        private float yOffset = 0f;

        [Header("Độ mượt của viền ở góc cua")]
        [SerializeField, Min(1), Tooltip("Cửa sổ (số mẫu mỗi phía) để đo độ cong; lớn hơn = độ cong ổn định hơn, ít gợn")]
        private int curvatureWindow = 3;
        [SerializeField, Min(0), Tooltip("Số lượt làm mượt độ cong dọc đường; lớn hơn = mép trong chuyển tiếp mượt hơn")]
        private int smoothPasses = 2;

        [Header("UV")]
        [SerializeField, Min(0.01f), Tooltip("Chiều dài dọc đường tương ứng 1 lần lặp UV (v) của texture")]
        private float tileLength = 1f;

        // Hệ số kẹp offset phía trong khúc cua: không cho offset vượt 95% bán kính cong cục bộ để mesh không gấp mép.
        private const float InnerClampFactor = 0.95f;

        // Độ rộng vùng chuyển tiếp của hàm kẹp mềm (đơn vị thế giới): trong vùng này offset giảm dần thay vì gập đột ngột.
        private const float SoftClampBlend = 0.3f;

        // Số điểm của tiết diện (mỗi mẫu trên quỹ đạo sinh ra đúng chừng này đỉnh dùng chung).
        private const int ProfileCount = 14;

        // Tiết diện cắt ngang băng chuyền: Side = lệch ngang so với tâm đường (+ = bên trái theo chiều đi),
        // Height = độ cao. Điểm 0–1: mặt đường. Điểm 2→7: gờ bên +Side (mặt đứng vào đường, vát, đỉnh, vát, mặt
        // đứng ra ngoài, chân xuống -0.2). Điểm 8→13: gờ bên -Side, đối xứng. Không nối 1→2 và 7→8.
        private static readonly float[] ProfileSide =
            { 0.35f, -0.35f, 0.35f, 0.35f, 0.37f, 0.48f, 0.5f, 0.5f,
              -0.5f, -0.5f, -0.48f, -0.37f, -0.35f, -0.35f };
        private static readonly float[] ProfileHeight =
            { 0.1f, 0.1f, -0.2f, 0.2f, 0.22f, 0.22f, 0.2f, -0.2f,
              -0.2f, 0.2f, 0.22f, 0.22f, 0.2f, -0.2f };

        // Đoạn (k → k+1) của tiết diện thuộc submesh nào.
        private static readonly int[] RoadSegments = { 0 };
        private static readonly int[] WallSegments = { 2, 3, 4, 5, 6, 8, 9, 10, 11, 12 };

        // Normal 2D (Side, Height) của từng điểm tiết diện, tiền tính 1 lần.
        private static readonly Vector2[] ProfileNormals = BuildProfileNormals();

        private static Vector2[] BuildProfileNormals()
        {
            var result = new Vector2[ProfileCount];

            // Mặt đường: normal hướng lên.
            result[0] = Vector2.up;
            result[1] = Vector2.up;

            FillPolylineNormals(result, 2, 7);
            FillPolylineNormals(result, 8, 13);
            return result;
        }

        // Normal đỉnh = trung bình chuẩn hóa normal các đoạn kề trong cùng polyline (đầu mút chỉ có 1 đoạn).
        private static void FillPolylineNormals(Vector2[] result, int first, int last)
        {
            for (int k = first; k <= last; k++)
            {
                Vector2 sum = Vector2.zero;
                if (k > first)
                {
                    sum += SegmentNormal(k - 1);
                }

                if (k < last)
                {
                    sum += SegmentNormal(k);
                }

                result[k] = sum.normalized;
            }
        }

        // Cả hai polyline gờ đều đi sao cho phần vật rắn của gờ nằm bên phải chiều đi, nên normal hướng ra
        // ngoài vật rắn là bên trái chiều đi: (-dHeight, dSide).
        private static Vector2 SegmentNormal(int k)
        {
            float ds = ProfileSide[k + 1] - ProfileSide[k];
            float dh = ProfileHeight[k + 1] - ProfileHeight[k];
            return new Vector2(-dh, ds).normalized;
        }

        private Transform _meshRoot;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;

        // Khung hình học tại 1 mẫu trên quỹ đạo.
        private struct Frame
        {
            public Vector3 position;
            public Vector3 left;      // pháp tuyến trái trên mặt XZ
            public int innerSide;     // +1: phía trái là phía trong khúc cua, -1: phía phải, 0: đoạn thẳng
            public float maxInner;    // offset tối đa cho phía trong
            public float v;           // toạ độ UV dọc đường (đã chia tileLength)
        }

        /// <summary>
        /// Nhận quỹ đạo đã dựng sẵn và dựng mesh ngay (mặt đường + gờ viền). Gọi 1 lần khi khởi tạo.
        /// </summary>
        public void Initialize(ConveyorPath path)
        {
            if (path == null)
            {
                Debug.LogError("[ConveyorLineView] ConveyorPath null, không thể vẽ quỹ đạo.");
                return;
            }

            // Prefab cũ còn LineRenderer (hướng 1): tắt đi để không vẽ trùng với mesh.
            LineRenderer legacyLine = GetComponent<LineRenderer>();
            if (legacyLine != null)
            {
                legacyLine.enabled = false;
            }

            BuildMesh(path.Sample(sampleCount));
        }

        private void EnsureMeshObject()
        {
            if (_meshRoot == null)
            {
                var go = new GameObject("RoadMesh");
                go.transform.SetParent(transform, false);
                _meshRoot = go.transform;

                var filter = go.AddComponent<MeshFilter>();
                _meshRenderer = go.AddComponent<MeshRenderer>();
                _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;

                _mesh = new Mesh { name = "ConveyorRoad" };
                filter.sharedMesh = _mesh;
            }

            _meshRenderer.sharedMaterials = new[] { roadMaterial, wallMaterial };
        }

        /// <summary>
        /// Dựng 1 mesh 2 submesh bằng cách quét tiết diện (<see cref="ProfileSide"/>/<see cref="ProfileHeight"/>)
        /// dọc quỹ đạo: road (đoạn 0→1) và wall (2 gờ). Mỗi điểm tiết diện 1 đỉnh chung nên normal mượt qua mép vát.
        /// </summary>
        private void BuildMesh(Vector3[] points)
        {
            bool buildRoad = roadMaterial != null;
            bool buildWall = wallMaterial != null;
            if (!buildRoad)
            {
                Debug.LogWarning("[ConveyorLineView] Chưa gán roadMaterial nên không vẽ mặt đường.");
            }

            if (!buildWall)
            {
                Debug.LogWarning("[ConveyorLineView] Chưa gán wallMaterial nên không vẽ gờ viền.");
            }

            int n = points.Length;
            if ((!buildRoad && !buildWall) || n < 2)
            {
                return;
            }

            EnsureMeshObject();

            Frame[] frames = ComputeFrames(points);

            var verts = new List<Vector3>(n * ProfileCount);
            var normals = new List<Vector3>(n * ProfileCount);
            var uvs = new List<Vector2>(n * ProfileCount);
            var roadTris = new List<int>();
            var wallTris = new List<int>();

            void AddQuad(List<int> tris, int a0, int a1, int b0, int b1)
            {
                // Đảo thứ tự tam giác nếu pháp tuyến hình học ngược với normal dự kiến, nên không phụ thuộc chiều đi của đường.
                Vector3 geometric = Vector3.Cross(verts[a1] - verts[a0], verts[b0] - verts[a0]);
                if (Vector3.Dot(geometric, normals[a0]) >= 0f)
                {
                    tris.Add(a0); tris.Add(a1); tris.Add(b0);
                    tris.Add(a1); tris.Add(b1); tris.Add(b0);
                }
                else
                {
                    tris.Add(a0); tris.Add(b0); tris.Add(a1);
                    tris.Add(a1); tris.Add(b0); tris.Add(b1);
                }
            }

            // Mỗi mẫu trên quỹ đạo sinh 1 "vòng" (ring) gồm ProfileCount đỉnh dùng chung, đặt theo tiết diện.
            // Luôn thêm đỉnh của cả road lẫn wall để chỉ số đỉnh cố định, kể cả khi thiếu 1 trong 2 material.
            for (int i = 0; i < n; i++)
            {
                Frame f = frames[i];
                for (int k = 0; k < ProfileCount; k++)
                {
                    float side = ProfileSide[k] * profileScale;
                    int sign = side > 0f ? 1 : (side < 0f ? -1 : 0);
                    if (sign != 0)
                    {
                        // Kẹp từng điểm tiết diện nằm phía trong khúc cua theo bán kính cong cục bộ (kẹp mềm,
                        // đơn điệu nên thứ tự các điểm được giữ, không gấp mép).
                        side = sign * Offset(Mathf.Abs(side), f, sign);
                    }

                    // Neo theo ProfileHeight[0] (mặt đường) để mặt đường nằm đúng độ cao quỹ đạo khi yOffset = 0.
                    float height = (ProfileHeight[k] - ProfileHeight[0]) * heightScale + yOffset;
                    Vector2 profileNormal = ProfileNormals[k];

                    verts.Add(f.position + f.left * side + Vector3.up * height);
                    normals.Add(f.left * profileNormal.x + Vector3.up * profileNormal.y);
                    uvs.Add(new Vector2(ProfileSide[k] + 0.5f, f.v));
                }
            }

            // Nối ring i với ring i+1 theo từng đoạn của tiết diện: road → submesh 0, wall → submesh 1.
            for (int i = 0; i < n - 1; i++)
            {
                int ringA = i * ProfileCount;
                int ringB = (i + 1) * ProfileCount;

                if (buildRoad)
                {
                    foreach (int seg in RoadSegments)
                    {
                        AddQuad(roadTris, ringA + seg, ringA + seg + 1, ringB + seg, ringB + seg + 1);
                    }
                }

                if (buildWall)
                {
                    foreach (int seg in WallSegments)
                    {
                        AddQuad(wallTris, ringA + seg, ringA + seg + 1, ringB + seg, ringB + seg + 1);
                    }
                }
            }

            // Đỉnh/normal đang ở toạ độ thế giới → đổi sang local của child.
            for (int i = 0; i < verts.Count; i++)
            {
                verts[i] = _meshRoot.InverseTransformPoint(verts[i]);
                normals[i] = _meshRoot.InverseTransformDirection(normals[i]).normalized;
            }

            _mesh.Clear();
            _mesh.indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            _mesh.SetVertices(verts);
            _mesh.SetNormals(normals);
            _mesh.SetUVs(0, uvs);
            _mesh.subMeshCount = 2;
            _mesh.SetTriangles(roadTris, 0);
            _mesh.SetTriangles(wallTris, 1);
            _mesh.RecalculateBounds();
        }

        /// <summary>
        /// Tính khung hình học (vị trí, pháp tuyến trái, độ cong) tại từng mẫu. Quỹ đạo khép kín (điểm đầu
        /// trùng điểm cuối, ví dụ p542439) thì mẫu đầu/cuối quấn vòng, bỏ điểm trùng.
        /// </summary>
        private Frame[] ComputeFrames(Vector3[] points)
        {
            int n = points.Length;
            bool closed = (points[0] - points[n - 1]).sqrMagnitude < 1e-6f;
            var frames = new Frame[n];

            Vector3 lastTangent = Vector3.forward;
            float v = 0f;

            // Độ cong có dấu (+ = rẽ trái) đo trên cửa sổ rộng rồi làm mượt, để phía trong khúc cua và mức kẹp offset
            // thay đổi liên tục dọc đường (độ cong đo trên 3 mẫu liền kề rất nhiễu → mép trong gợn/nhô).
            float[] curvature = new float[n];
            for (int i = 0; i < n; i++)
            {
                curvature[i] = SignedCurvature(points, i, closed, curvatureWindow);
            }

            SmoothCyclic(curvature, closed, curvatureWindow, smoothPasses);

            for (int i = 0; i < n; i++)
            {
                int prev = i > 0 ? i - 1 : (closed ? n - 2 : 0);
                int next = i < n - 1 ? i + 1 : (closed ? 1 : n - 1);

                Vector3 p = points[i];

                Vector3 tangent = Flat(points[next] - points[prev]);
                if (tangent.sqrMagnitude < 1e-10f)
                {
                    tangent = lastTangent;
                }

                tangent.Normalize();
                lastTangent = tangent;

                // Độ cong cục bộ: bên "trong" khúc cua không được dịch xa hơn ~bán kính cong, nếu không mép gấp lại.
                int innerSide = 0;
                float maxInner = float.MaxValue;
                float absCurvature = Mathf.Abs(curvature[i]);
                if (absCurvature > 1e-4f)
                {
                    // Độ cong > 0: rẽ trái → phía trái là phía trong.
                    innerSide = curvature[i] > 0f ? 1 : -1;
                    maxInner = InnerClampFactor / absCurvature;
                }

                if (i > 0)
                {
                    v += (points[i] - points[i - 1]).magnitude / tileLength;
                }

                frames[i] = new Frame
                {
                    position = p,
                    left = new Vector3(-tangent.z, 0f, tangent.x),
                    innerSide = innerSide,
                    maxInner = maxInner,
                    v = v,
                };
            }

            return frames;
        }

        /// <summary>
        /// Kẹp mềm offset theo bán kính cong nếu <paramref name="side"/> là phía trong khúc cua tại khung này.
        /// Dùng smooth-min (liên tục, không có điểm gập) thay cho Mathf.Min để mép không bị gấp khúc quanh ngưỡng.
        /// </summary>
        private static float Offset(float offset, Frame frame, int side)
        {
            if (side != frame.innerSide)
            {
                return offset;
            }

            float max = frame.maxInner;
            float h = Mathf.Max(SoftClampBlend - Mathf.Abs(offset - max), 0f) / SoftClampBlend;
            return Mathf.Min(offset, max) - h * h * SoftClampBlend * 0.25f;
        }

        /// <summary>Chỉ số mẫu sau khi quấn vòng (quỹ đạo khép kín: điểm đầu trùng điểm cuối) hoặc kẹp (quỹ đạo hở).</summary>
        private static int WrapIndex(int i, int n, bool closed)
        {
            if (!closed)
            {
                return Mathf.Clamp(i, 0, n - 1);
            }

            int cycle = n - 1;
            return ((i % cycle) + cycle) % cycle;
        }

        /// <summary>Độ cong có dấu (+ = rẽ trái) tại mẫu <paramref name="i"/>, đo giữa hai mẫu cách <paramref name="window"/> mỗi phía.</summary>
        private static float SignedCurvature(Vector3[] points, int i, bool closed, int window)
        {
            int n = points.Length;
            Vector3 a = points[WrapIndex(i - window, n, closed)];
            Vector3 b = points[WrapIndex(i, n, closed)];
            Vector3 c = points[WrapIndex(i + window, n, closed)];

            Vector3 d1 = Flat(b - a);
            Vector3 d2 = Flat(c - b);
            float l1 = d1.magnitude;
            float l2 = d2.magnitude;
            if (l1 < 1e-5f || l2 < 1e-5f)
            {
                return 0f;
            }

            float angle = Vector3.Angle(d1, d2) * Mathf.Deg2Rad;
            float sign = (d1.x * d2.z - d1.z * d2.x) > 0f ? 1f : -1f;
            return sign * angle / (0.5f * (l1 + l2));
        }

        /// <summary>Làm mượt bằng trung bình trượt (cửa sổ ±<paramref name="window"/>), quấn vòng nếu quỹ đạo khép kín.</summary>
        private static void SmoothCyclic(float[] values, bool closed, int window, int passes)
        {
            int n = values.Length;
            var temp = new float[n];
            for (int pass = 0; pass < passes; pass++)
            {
                for (int i = 0; i < n; i++)
                {
                    float sum = 0f;
                    for (int k = -window; k <= window; k++)
                    {
                        sum += values[WrapIndex(i + k, n, closed)];
                    }

                    temp[i] = sum / (2 * window + 1);
                }

                if (closed)
                {
                    temp[n - 1] = temp[0];
                }

                System.Array.Copy(temp, values, n);
            }
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }
    }
}
