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
        private ConveyorPath _path;
        private ConveyorModel _model;
        private SlicePool _pool;
        private float _speed;
        private readonly Dictionary<int, SliceView> _views = new Dictionary<int, SliceView>();

        public void Initialize(ConveyorPath path, float speed, ConveyorModel model, SlicePool pool)
        {
            _path = path;
            _speed = speed;
            _model = model;
            _pool = pool;
        }

        /// <summary>Giữ chỗ trước cho khối sắp bay lên (cập nhật dữ liệu trước hoạt ảnh).</summary>
        public bool TryReserve(int amount) => _model != null && _model.TryReserve(amount);

        /// <summary>Khối đã hạ cánh: ghi vào dữ liệu và giao cho view này điều khiển.</summary>
        public void Land(SliceView slice, int colorId, float distance)
        {
            int id = _model.AddReserved(colorId, distance);
            _views[id] = slice;
        }

        /// <summary>Lấy khối khỏi băng chuyền và trả về pool (dùng cho hấp thụ vào khay sau này).</summary>
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

            float length = _path.Length;
            foreach (ConveyorItem item in _model.Items)
            {
                item.distance = (item.distance + _speed * Time.deltaTime) % length;

                if (_views.TryGetValue(item.id, out SliceView view))
                {
                    view.transform.SetPositionAndRotation(
                        _path.GetPointAtDistance(item.distance),
                        Quaternion.LookRotation(_path.GetTangentAtDistance(item.distance), Vector3.up));
                }
            }
        }
    }
}
