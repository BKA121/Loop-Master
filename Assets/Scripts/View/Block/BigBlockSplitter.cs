using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using LoopMaster.Core;
using LoopMaster.Data;

namespace LoopMaster.View
{
    /// <summary>
    /// Tách các khối lớn thành khối nhỏ và đưa lên băng chuyền: từng khối lớn (từ trên cùng xuống) co lại dần, cứ mỗi
    /// <see cref="splitInterval"/> giây tách ra 1 slice bay ngay lên băng chuyền, tại điểm tia từ mũi tên của khay cắt quỹ đạo.
    /// Các slice hạ cánh cùng một điểm, cách nhau splitInterval giây nên trên băng chuyền tự cách nhau đều
    /// (khoảng cách = tốc độ × splitInterval), kể cả giữa hai khối lớn liên tiếp.
    /// Nhận path/tốc độ/view/pool qua <see cref="Initialize"/> từ <see cref="GameViewManager"/>.
    /// </summary>
    public class BigBlockSplitter : MonoBehaviour
    {
        [SerializeField, Min(1)] private int sliceCount = 10;
        [SerializeField, Min(0.01f), Tooltip("Số giây giữa 2 lần tách; khoảng cách trên băng chuyền = tốc độ × giá trị này")]
        private float splitInterval = 0.08f;
        [SerializeField, Min(0.05f)] private float flightTime = 0.6f;
        [SerializeField] private float arcHeight = 2f;
        [SerializeField] private Ease ease = Ease.InOutSine;

        private ConveyorPath _path;
        private ConveyorView _conveyorView;
        private SlicePool _pool;
        private BlockMaterialSet _materials;

        public void Initialize(ConveyorPath path, ConveyorView conveyorView, SlicePool pool, BlockMaterialSet materials)
        {
            _path = path;
            _conveyorView = conveyorView;
            _pool = pool;
            _materials = materials;
        }

        /// <summary>
        /// Giữ chỗ trên băng chuyền cho toàn bộ <paramref name="blocks"/> (tất cả hoặc không) rồi bắt đầu tách.
        /// Trả về false (không thay đổi gì) nếu chưa Initialize, tia không cắt băng chuyền hoặc không đủ chỗ.
        /// <paramref name="onComplete"/> được gọi khi slice cuối cùng đã bay đi.
        /// </summary>
        public bool TrySplit(IList<BigBlockView> blocks, Transform arrow, int colorId, Action onComplete)
        {
            if (_path == null || _conveyorView == null || _pool == null)
            {
                Debug.LogWarning("[BigBlockSplitter] Chưa Initialize (path/conveyorView/pool).");
                return false;
            }

            if (arrow == null)
            {
                Debug.LogWarning("[BigBlockSplitter] Khay không có mũi tên đầu ra.");
                return false;
            }

            if (blocks == null || blocks.Count == 0)
            {
                return false;
            }

            if (!_path.TryGetDistanceAlongRay(arrow.position, arrow.forward, out float landDistance))
            {
                Debug.LogWarning("[BigBlockSplitter] Tia từ mũi tên không cắt băng chuyền.");
                return false;
            }

            int total = blocks.Count * sliceCount;
            if (!_conveyorView.TryReserve(total))
            {
                Debug.LogWarning("[BigBlockSplitter] Băng chuyền không đủ chỗ cho " + total + " khối nhỏ.");
                return false;
            }

            Vector3 endPos = _path.GetPointAtDistance(landDistance);
            Quaternion endRot = Quaternion.LookRotation(_path.GetTangentAtDistance(landDistance), Vector3.up);

            Sequence seq = DOTween.Sequence().SetTarget(this);
            for (int b = 0; b < blocks.Count; b++)
            {
                BigBlockView block = blocks[b];
                Vector3 baseScale = block.transform.localScale;

                for (int i = 0; i < sliceCount; i++)
                {
                    int index = i;
                    seq.AppendCallback(() => SpawnAndFly(block, baseScale, index, colorId, landDistance, endPos, endRot));
                    if (b < blocks.Count - 1 || i < sliceCount - 1)
                    {
                        seq.AppendInterval(splitInterval);
                    }
                }
            }

            seq.OnComplete(() => onComplete?.Invoke());
            return true;
        }

        private void SpawnAndFly(BigBlockView block, Vector3 baseScale, int index, int colorId,
            float landDistance, Vector3 endPos, Quaternion endRot)
        {
            SliceView slice = _pool.Get(block.transform.position, block.transform.rotation);
            slice.SetMaterial(_materials != null ? _materials.Get(colorId) : null);

            // Khối lớn co dần theo số slice đã tách; slice cuối cùng thì ẩn hẳn.
            float remaining = 1f - (index + 1f) / sliceCount;
            block.transform.localScale = baseScale * remaining;
            if (index == sliceCount - 1)
            {
                foreach (Renderer r in block.GetComponentsInChildren<Renderer>())
                {
                    r.enabled = false;
                }
            }

            BlockFlight.Fly(slice.transform, endPos, endRot, flightTime, arcHeight, ease,
                () => _conveyorView.Land(slice, colorId, landDistance));
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}
