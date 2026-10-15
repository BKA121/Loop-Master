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
        [SerializeField, Min(0.01f), Tooltip("Số giây giữa 2 lần tách; khoảng cách trên băng chuyền = tốc độ × giá trị này")]
        private float splitInterval = 0.08f;
        [SerializeField, Min(0.05f)] private float flightTime = 0.6f;
        [SerializeField] private float arcHeight = 2f;
        [SerializeField] private Ease ease = Ease.InOutSine;

        /// <summary>Một khối lớn cần tách: lấy <see cref="takeSlices"/> khối nhỏ trong số <see cref="startSlices"/> đang còn.</summary>
        public struct Step
        {
            public BigBlockView block;
            public int startSlices;
            public int takeSlices;
        }

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
        /// Giữ chỗ trên băng chuyền cho toàn bộ khối nhỏ của <paramref name="steps"/> (tất cả hoặc không) rồi bắt đầu tách.
        /// Trả về false (không thay đổi gì) nếu chưa Initialize, tia không cắt băng chuyền hoặc không đủ chỗ.
        /// <paramref name="onComplete"/> được gọi khi slice cuối cùng đã bay đi.
        /// </summary>
        public bool TrySplit(IList<Step> steps, Transform arrow, int colorId, Action onComplete)
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

            if (steps == null || steps.Count == 0)
            {
                return false;
            }

            if (!_path.TryGetDistanceAlongRay(arrow.position, arrow.forward, out float landDistance))
            {
                Debug.LogWarning("[BigBlockSplitter] Tia từ mũi tên không cắt băng chuyền.");
                return false;
            }

            int total = 0;
            foreach (Step step in steps)
            {
                total += step.takeSlices;
            }

            if (!_conveyorView.TryReserve(total))
            {
                Debug.LogWarning("[BigBlockSplitter] Băng chuyền không đủ chỗ cho " + total + " khối nhỏ.");
                return false;
            }

            // Rào chắn tại điểm hạ cánh: khối đang chạy tới không được vào sát điểm đó cho tới khi slice cuối cùng hạ cánh.
            InsertionGate gate = _conveyorView.OpenGate(landDistance);
            int remaining = total;
            Action onSliceLanded = () =>
            {
                if (--remaining == 0)
                {
                    _conveyorView.CloseGate(gate);
                }
            };

            Vector3 endPos = _path.GetPointAtDistance(landDistance);
            Quaternion endRot = Quaternion.LookRotation(_path.GetTangentAtDistance(landDistance), Vector3.up);

            Sequence seq = DOTween.Sequence().SetTarget(this);
            for (int b = 0; b < steps.Count; b++)
            {
                Step step = steps[b];

                for (int i = 0; i < step.takeSlices; i++)
                {
                    int index = i;
                    seq.AppendCallback(() => SpawnAndFly(step, index, colorId, landDistance, endPos, endRot, onSliceLanded));
                    if (b < steps.Count - 1 || i < step.takeSlices - 1)
                    {
                        seq.AppendInterval(splitInterval);
                    }
                }
            }

            seq.OnComplete(() => onComplete?.Invoke());
            return true;
        }

        private void SpawnAndFly(Step step, int index, int colorId,
            float landDistance, Vector3 endPos, Quaternion endRot, Action onSliceLanded)
        {
            BigBlockView block = step.block;
            SliceView slice = _pool.Get(block.transform.position, block.transform.rotation);
            slice.SetMaterial(_materials != null ? _materials.Get(colorId) : null);

            // Khối lớn mỏng dần theo số slice đã tách (dọc trục Z của khay); lấy hết thì ẩn hẳn, lấy một phần thì dừng ở độ dày còn lại.
            int remainingSlices = step.startSlices - (index + 1);
            block.SetSlices(remainingSlices);
            if (remainingSlices <= 0)
            {
                foreach (Renderer r in block.GetComponentsInChildren<Renderer>())
                {
                    r.enabled = false;
                }
            }

            BlockFlight.Fly(slice.transform, endPos, endRot, flightTime, arcHeight, ease,
                () =>
                {
                    _conveyorView.Land(slice, colorId, landDistance);
                    onSliceLanded();
                });
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}
