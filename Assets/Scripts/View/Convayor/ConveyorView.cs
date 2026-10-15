using System.Collections.Generic;
using UnityEngine;
using LoopMaster.Core;

namespace LoopMaster.View
{
    /// <summary>
    /// Hiển thị các khối nhỏ trên băng chuyền theo <see cref="ConveyorModel"/>: mỗi frame đẩy
    /// <c>distance</c> của mọi khối tiến lên, đặt vị trí/xoay theo <see cref="ConveyorPath"/> (uốn theo khúc cua).
    /// Không tự load level; nhận mọi thứ qua <see cref="Initialize"/> từ <see cref="GameViewManager"/>.
    /// </summary>
    public class ConveyorView : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Hai khối liền kề cách nhau không quá giá trị này thì coi là cùng một dãy liên tục (khi dãy bị xen vào, đuôi dãy dừng chờ)")]
        private float contiguousGap = 0.5f;
        [SerializeField, Min(0f), Tooltip("Khi có khối đang bay lên, khối từ phía sau dừng cách điểm hạ cánh đoạn này; nên ≈ khoảng cách giữa 2 khối nhỏ (tốc độ × splitInterval)")]
        private float landingClearance = 0.35f;

        private ConveyorPath _path;
        private ConveyorModel _model;
        private SlicePool _pool;
        private float _speed;
        private readonly Dictionary<int, SliceView> _views = new Dictionary<int, SliceView>();
        private readonly List<PortCrossing> _crossings = new List<PortCrossing>();
        private IReadOnlyList<float> _ports;
        private ISlicePortHandler _portHandler;

        public void Initialize(ConveyorPath path, float speed, ConveyorModel model, SlicePool pool)
        {
            _path = path;
            _speed = speed;
            _model = model;
            _pool = pool;
        }

        /// <summary>Đặt các cổng (khoảng cách trên quỹ đạo của mũi tên mỗi khay) và bên xử lý khi khối vượt cổng.</summary>
        public void SetPorts(IReadOnlyList<float> portDistances, ISlicePortHandler handler)
        {
            _ports = portDistances;
            _portHandler = handler;
        }

        /// <summary>Số khối nhỏ còn có thể lên băng chuyền ngay bây giờ.</summary>
        public int FreeSlots => _model != null ? _model.FreeSlots : 0;

        /// <summary>Giữ chỗ trước cho khối sắp bay lên (cập nhật dữ liệu trước hoạt ảnh).</summary>
        public bool TryReserve(int amount) => _model != null && _model.TryReserve(amount);

        /// <summary>Mở rào chắn tại điểm hạ cánh: khối từ phía sau không chạy vào sát điểm đó cho tới <see cref="CloseGate"/>.</summary>
        public InsertionGate OpenGate(float landDistance) => _model?.OpenGate(landDistance);

        /// <summary>Đóng rào khi khối cuối cùng xen vào đã hạ cánh xong; các khối đang chờ chạy lại.</summary>
        public void CloseGate(InsertionGate gate) => _model?.CloseGate(gate);

        /// <summary>Khối đã hạ cánh: ghi vào dữ liệu và giao cho view này điều khiển.</summary>
        public void Land(SliceView slice, int colorId, float distance)
        {
            int id = _model.AddReserved(colorId, distance);
            _views[id] = slice;
        }

        /// <summary>Lấy khối khỏi băng chuyền và trả ngay về pool.</summary>
        public void Remove(int id)
        {
            if (_model.Remove(id) && _views.TryGetValue(id, out SliceView view))
            {
                _views.Remove(id);
                _pool.Release(view);
            }
        }

        private void Update()
        {
            if (_path == null || _model == null)
            {
                return;
            }

            _model.Advance(_speed * Time.deltaTime, _path.Length, contiguousGap, landingClearance, _ports, _crossings);

            foreach (ConveyorItem item in _model.Items)
            {
                if (_views.TryGetValue(item.id, out SliceView view))
                {
                    view.transform.SetPositionAndRotation(
                        _path.GetPointAtDistance(item.distance),
                        Quaternion.LookRotation(_path.GetTangentAtDistance(item.distance), Vector3.up));
                }
            }

            ProcessCrossings();
        }

        /// <summary>
        /// Khối vừa vượt cổng: nếu đã được cổng này nhận trước thì rời băng chuyền bay vào khay; nếu chưa khay nào nhận thì
        /// hỏi khay một lần và nhận trước cả cụm nối đuôi cùng màu phía sau; nếu cổng khác đã nhận thì bỏ qua.
        /// </summary>
        private void ProcessCrossings()
        {
            if (_portHandler == null)
            {
                return;
            }

            for (int i = 0; i < _crossings.Count; i++)
            {
                ConveyorItem item = _crossings[i].item;
                int port = _crossings[i].portIndex;

                if (item.claimedPort == -1)
                {
                    int accept = _portHandler.GetAcceptCount(port, item.colorId);
                    if (accept <= 0)
                    {
                        continue;
                    }

                    int claimed = _model.ClaimRun(item, port, _path.Length, contiguousGap, accept);
                    _portHandler.OnClaimed(port, item.colorId, claimed);
                }

                if (item.claimedPort == port && _views.TryGetValue(item.id, out SliceView slice))
                {
                    _model.Remove(item.id);
                    _views.Remove(item.id);
                    _portHandler.OnAbsorb(port, slice, item.colorId);
                }
            }
        }
    }
}
