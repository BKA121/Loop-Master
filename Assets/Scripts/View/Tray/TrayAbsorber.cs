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
        /// Dựng cổng cho từng khay tại điểm trên quỹ đạo gần mũi tên nhất (cùng điểm khối bay lên), rồi đăng ký với băng chuyền.
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
                if (arrow != null)
                {
                    _portDistances.Add(path.GetNearestDistance(arrow.position));
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

            // Hai khối liên tiếp vào cùng một khay cách nhau tối thiểu `interval` (bình thường delay ≈ 0 vì khoảng cách
            // trên băng chuyền đã bằng tốc độ × splitInterval); khối tới sát nhau thì chờ tại mũi tên tới lượt.
            float start = Mathf.Max(Time.time, _portNextAbsorbTime[portIndex]);
            _portNextAbsorbTime[portIndex] = start + interval;
            float delay = start - Time.time;

            Vector3 target = tray.ReserveLandingPosition();
            BlockFlight.Fly(slice.transform, target, tray.transform.rotation, flightTime, arcHeight, ease, () =>
                {
                    _pool.Release(slice);
                    tray.OnSliceArrived(colorId);
                })
                .SetDelay(delay);
        }
    }
}
