using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using LoopMaster.Core;

namespace LoopMaster.View
{
    /// <summary>
    /// Chiều băng chuyền → khay (đối xứng với <see cref="BigBlockSplitter"/> là khay → băng chuyền): mỗi mũi tên khay là một
    /// "cổng" trên băng chuyền. Khi khối nhỏ vượt cổng, <see cref="ConveyorView"/> hỏi lớp này khay có nhận không; nếu nhận,
    /// khay giữ chỗ cho cả cụm, rồi từng khối tới mũi tên thì bay cung vào khay.
    /// Nhận path/view/pool/khay qua <see cref="Initialize"/> từ <see cref="GameViewManager"/>.
    /// </summary>
    public class TrayAbsorber : MonoBehaviour, ISlicePortHandler
    {
        [SerializeField, Min(0.05f), Tooltip("Thời gian khối nhỏ bay từ băng chuyền vào khay (nên bằng Flight Time của BigBlockSplitter)")]
        private float flightTime = 0.6f;
        [SerializeField, Tooltip("Độ cao cung bay khi khối nhỏ vào khay (nên bằng Arc Height của BigBlockSplitter)")]
        private float arcHeight = 2f;
        [SerializeField] private Ease ease = Ease.InOutSine;
        [SerializeField, Min(0f), Tooltip("Khoảng cách tối thiểu giữa hai khối liên tiếp bay vào cùng một khay (nên bằng Split Interval của BigBlockSplitter)")]
        private float interval = 0.07f;

        private readonly List<float> _portDistances = new List<float>();
        private readonly List<TrayView> _portTrays = new List<TrayView>();
        private readonly List<float> _portNextAbsorbTime = new List<float>();
        private SlicePool _pool;

        /// <summary>
        /// Dựng cổng cho từng khay tại điểm tia từ mũi tên cắt quỹ đạo (cùng điểm khối bay lên), rồi đăng ký với băng chuyền.
        /// </summary>
        public void Initialize(ConveyorPath path, ConveyorView conveyorView, SlicePool pool, IReadOnlyList<TrayView> trays)
        {
            _pool = pool;
            _portDistances.Clear();
            _portTrays.Clear();
            _portNextAbsorbTime.Clear();

            if (path == null || conveyorView == null)
            {
                return;
            }

            foreach (TrayView tray in trays)
            {
                Transform arrow = tray.ArrowTransform;
                if (arrow != null && path.TryGetDistanceAlongRay(arrow.position, arrow.forward, out float distance))
                {
                    _portDistances.Add(distance);
                    _portTrays.Add(tray);
                    _portNextAbsorbTime.Add(0f);
                }
            }

            conveyorView.SetPorts(_portDistances, this);
        }

        public int GetAcceptCount(int portIndex, int colorId)
        {
            TrayView tray = _portTrays[portIndex];
            return tray.IsBusy ? 0 : tray.Model.AcceptableSlices(colorId);
        }

        public void OnClaimed(int portIndex, int colorId, int count)
        {
            _portTrays[portIndex].Model.Reserve(colorId, count);
        }

        public void OnAbsorb(int portIndex, SliceView slice, int colorId)
        {
            TrayView tray = _portTrays[portIndex];

            // Ô đích: khối đỉnh đang dở cùng màu (còn chỗ) thì bay vào đó, ngược lại ô kế tiếp (sẽ thành khối lớn mới).
            int stackCount = tray.Model.Stack.Count;
            BlockModel top = stackCount > 0 ? tray.Model.Stack[stackCount - 1] : null;
            bool fillsTop = top != null && top.colorId == colorId && top.slices < BlockModel.SlicesPerBlock;
            int slot = fillsTop ? stackCount - 1 : stackCount;

            // Hai khối liên tiếp vào cùng một khay cách nhau tối thiểu `interval` (bình thường delay ≈ 0 vì khoảng cách
            // trên băng chuyền đã bằng tốc độ × splitInterval); khối tới sát nhau thì chờ tại mũi tên tới lượt.
            float start = Mathf.Max(Time.time, _portNextAbsorbTime[portIndex]);
            _portNextAbsorbTime[portIndex] = start + interval;
            float delay = start - Time.time;

            Vector3 target = tray.GetSlotWorldPosition(slot);
            BlockFlight.Fly(slice.transform, target, tray.transform.rotation, flightTime, arcHeight, ease, () =>
                {
                    _pool.Release(slice);
                    tray.OnSliceArrived(colorId);
                })
                .SetDelay(delay);
        }
    }
}
