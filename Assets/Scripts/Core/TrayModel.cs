using System.Collections.Generic;
using UnityEngine;
using LoopMaster.Data;

namespace LoopMaster.Core
{
    /// <summary>Một khối lớn trong khay. Mystery/khóa... bổ sung ở các mốc sau.</summary>
    public class BlockModel
    {
        public int colorId;
    }

    /// <summary>
    /// Dữ liệu logic của 1 khay. <see cref="Stack"/> theo đúng thứ tự DTO: phần tử đầu = đáy, cuối = trên cùng (LIFO).
    /// Hiện chỉ đọc; Pop/Push thêm ở mốc bấm khay. Không phụ thuộc hiển thị.
    /// </summary>
    public class TrayModel
    {
        public int Id { get; }
        public Vector3 Position { get; }
        public Vector3 EulerAngles { get; }
        public bool IsLocked { get; }
        public IReadOnlyList<BlockModel> Stack => _stack;

        private readonly List<BlockModel> _stack = new List<BlockModel>();

        public BlockModel Peek() => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        /// <summary>Số khối liên tiếp cùng màu với khối trên cùng, đếm từ trên xuống (0 nếu khay rỗng).</summary>
        public int TopRunLength()
        {
            if (_stack.Count == 0) return 0;

            int colorId = _stack[_stack.Count - 1].colorId;
            int count = 0;
            for (int i = _stack.Count - 1; i >= 0 && _stack[i].colorId == colorId; i--)
            {
                count++;
            }

            return count;
        }

        /// <summary>Bỏ <paramref name="count"/> khối trên cùng khỏi khay (lấy lên băng chuyền).</summary>
        public void PopTop(int count)
        {
            count = Mathf.Clamp(count, 0, _stack.Count);
            _stack.RemoveRange(_stack.Count - count, count);
        }

        private TrayModel(int id, Vector3 position, Vector3 eulerAngles, bool isLocked)
        {
            Id = id;
            Position = position;
            EulerAngles = eulerAngles;
            IsLocked = isLocked;
        }

        public static TrayModel FromDto(TrayDTO dto)
        {
            var model = new TrayModel(dto.i, ToVector3(dto.p), ToVector3(dto.r), dto.l == 1);

            if (dto.c != null)
            {
                foreach (BlockDTO block in dto.c)
                {
                    model._stack.Add(new BlockModel { colorId = block.t });
                }
            }

            return model;
        }

        private static Vector3 ToVector3(float[] v)
        {
            return v != null && v.Length >= 3 ? new Vector3(v[0], v[1], v[2]) : Vector3.zero;
        }
    }
}
