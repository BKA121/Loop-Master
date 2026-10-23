using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using LoopMaster.Audio;
using LoopMaster.Core;
using LoopMaster.Data;

namespace LoopMaster.View
{
    /// <summary>
    /// Tách các khối lớn thành khối nhỏ và đưa lên băng chuyền: từng khối lớn (từ trên cùng xuống) co lại dần, cứ mỗi
    /// <see cref="splitInterval"/> giây tách ra 1 slice bay ngay lên băng chuyền, tại điểm trên quỹ đạo gần mũi tên của khay nhất.
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
        [SerializeField, Min(0.01f), Tooltip("Scale X của khối nhỏ khi hạ cánh trên băng chuyền; tween từ scale hiện tại cùng lúc với xoay")]
        private float landScaleX = 0.7f;

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
        /// Trả về false (không thay đổi gì) nếu chưa Initialize, khay không có mũi tên hoặc không đủ chỗ.
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

            float landDistance = _path.GetNearestDistance(arrow.position);

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

            // Giờ hạ cánh dự kiến của khối thứ k = giờ bắn khối đầu tiên + k × splitInterval + flightTime. Callback của sequence chỉ
            // chạy ở đầu frame (và có thể nhiều callback chung 1 frame khi giật), nên lúc hạ cánh phải bù phần trễ so với giờ này
            // để khoảng cách giữa các khối luôn = tốc độ × splitInterval, không phụ thuộc tốc độ khung hình.
            float baseTime = float.NaN;
            int order = 0;

            Sequence seq = DOTween.Sequence().SetTarget(this);
            for (int b = 0; b < steps.Count; b++)
            {
                Step step = steps[b];

                for (int i = 0; i < step.takeSlices; i++)
                {
                    int index = i;
                    seq.AppendCallback(() =>
                    {
                        if (float.IsNaN(baseTime))
                        {
                            baseTime = Time.time;
                        }

                        float plannedLandTime = baseTime + order * splitInterval + flightTime;
                        order++;
                        SpawnAndFly(step, index, colorId, landDistance, endPos, endRot, plannedLandTime, onSliceLanded);
                    });
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
            float landDistance, Vector3 endPos, Quaternion endRot, float plannedLandTime, Action onSliceLanded)
        {
            BigBlockView block = step.block;
            // Tách từ mép ngoài (lát +Z cuối cùng còn lại), không phải từ tâm khối; phải lấy vị trí trước khi khối mỏng đi.
            SliceView slice = _pool.Get(block.GetOuterSliceWorldPosition(), block.transform.rotation);
            slice.SetMaterial(_materials != null ? _materials.Get(colorId) : null);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(SfxType.SliceSplit);
            }

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
                    // Tween chỉ kết thúc ở đầu frame: bù quãng băng chuyền đã chạy trong lúc trễ so với giờ dự kiến.
                    float lateness = Mathf.Max(0f, Time.time - plannedLandTime);
                    _conveyorView.Land(slice, colorId, landDistance, lateness);
                    onSliceLanded();
                },
                endScaleX: landScaleX);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}
