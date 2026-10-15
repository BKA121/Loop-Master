using System.Collections.Generic;
using UnityEngine;
using LoopMaster.Data;

namespace LoopMaster.Core
{
    /// <summary>Một khối lớn trong khay. Mystery/khóa... bổ sung ở các mốc sau.</summary>
    public class BlockModel
    {
        /// <summary>Số khối nhỏ mà một khối lớn đầy đủ tách ra (GAME_DESIGN.md).</summary>
        public const int SlicesPerBlock = 10;

        public int colorId;

        /// <summary>Số khối nhỏ còn lại; &lt; <see cref="SlicesPerBlock"/> nếu đã bay lên một phần.</summary>
        public int slices = SlicesPerBlock;
    }

    /// <summary>Một khối lớn bị lấy (toàn bộ hoặc một phần) trong <see cref="TakePlan"/>.</summary>
    public class TakeStep
    {
        public int startSlices;
        public int takeSlices;
        public bool IsWhole => takeSlices >= startSlices;
    }

    /// <summary>Kế hoạch lấy khối nhỏ từ trên xuống của một khay; chỉ là kế hoạch, chưa đổi dữ liệu.</summary>
    public class TakePlan
    {
        public readonly List<TakeStep> Steps = new List<TakeStep>();
        public int Total;

        /// <summary>Số khối lớn bị lấy hết (luôn là các khối trên cùng; khối lấy một phần, nếu có, là bước cuối).</summary>
        public int WholeCount
        {
            get
            {
                int count = 0;
                foreach (TakeStep step in Steps)
                {
                    if (step.IsWhole) count++;
                }

                return count;
            }
        }
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
        public bool IsLocked { get; private set; }
        public IReadOnlyList<BlockModel> Stack => _stack;

        /// <summary>Sức chứa tối đa của khay tính theo khối lớn (GAME_DESIGN.md).</summary>
        public const int MaxBlocks = 4;

        /// <summary>Số khối nhỏ đã được khay nhận trước nhưng chưa hạ cánh vào khay.</summary>
        public int IncomingSlices { get; private set; }

        private int _incomingColorId;

        private readonly List<BlockModel> _stack = new List<BlockModel>();

        public BlockModel Peek() => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        /// <summary>
        /// Lập kế hoạch lấy tối đa <paramref name="maxSlices"/> khối nhỏ từ nhóm khối cùng màu liên tiếp ở trên cùng
        /// (từ trên xuống; khối cuối cùng bị chạm tới có thể chỉ lấy một phần). Không đổi dữ liệu.
        /// </summary>
        public TakePlan PlanTake(int maxSlices)
        {
            var plan = new TakePlan();
            if (_stack.Count == 0 || maxSlices <= 0)
            {
                return plan;
            }

            int colorId = _stack[_stack.Count - 1].colorId;
            int budget = maxSlices;
            for (int i = _stack.Count - 1; i >= 0 && _stack[i].colorId == colorId && budget > 0; i--)
            {
                int take = Mathf.Min(_stack[i].slices, budget);
                plan.Steps.Add(new TakeStep { startSlices = _stack[i].slices, takeSlices = take });
                plan.Total += take;
                budget -= take;
            }

            return plan;
        }

        /// <summary>
        /// Số khối nhỏ màu <paramref name="colorId"/> khay còn nhận được ngay lúc này (0 nếu khóa hoặc khác màu).
        /// Khay trống nhận mọi màu; khay có khối (hoặc đã nhận trước khối) chỉ nhận màu đó. Đã trừ phần nhận trước.
        /// </summary>
        public int AcceptableSlices(int colorId)
        {
            if (IsLocked)
            {
                return 0;
            }

            if (IncomingSlices > 0)
            {
                if (_incomingColorId != colorId) return 0;
            }
            else if (_stack.Count > 0 && _stack[_stack.Count - 1].colorId != colorId)
            {
                return 0;
            }

            int total = 0;
            foreach (BlockModel block in _stack)
            {
                total += block.slices;
            }

            return Mathf.Max(0, MaxBlocks * BlockModel.SlicesPerBlock - total - IncomingSlices);
        }

        /// <summary>Giữ chỗ cho <paramref name="count"/> khối nhỏ sắp hạ cánh vào khay.</summary>
        public void Reserve(int colorId, int count)
        {
            IncomingSlices += count;
            _incomingColorId = colorId;
        }

        /// <summary>Một khối nhỏ đã hạ cánh vào khay: lấp vào khối trên cùng đang dở hoặc tạo khối lớn mới (1 khối nhỏ).</summary>
        public void ApplyArrival(int colorId)
        {
            if (IncomingSlices > 0)
            {
                IncomingSlices--;
            }

            BlockModel top = _stack.Count > 0 ? _stack[_stack.Count - 1] : null;
            if (top != null && top.colorId == colorId && top.slices < BlockModel.SlicesPerBlock)
            {
                top.slices++;
            }
            else
            {
                _stack.Add(new BlockModel { colorId = colorId, slices = 1 });
            }

            if (IsComplete())
            {
                IsLocked = true;
            }
        }

        /// <summary>Đủ <see cref="MaxBlocks"/> khối lớn đầy đủ cùng màu.</summary>
        private bool IsComplete()
        {
            if (_stack.Count < MaxBlocks)
            {
                return false;
            }

            int colorId = _stack[0].colorId;
            foreach (BlockModel block in _stack)
            {
                if (block.colorId != colorId || block.slices < BlockModel.SlicesPerBlock)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Áp dụng kế hoạch: bỏ các khối lấy hết, trừ số khối nhỏ của khối bị lấy một phần.</summary>
        public void ApplyTake(TakePlan plan)
        {
            foreach (TakeStep step in plan.Steps)
            {
                int top = _stack.Count - 1;
                if (step.IsWhole)
                {
                    _stack.RemoveAt(top);
                }
                else
                {
                    _stack[top].slices -= step.takeSlices;
                }
            }
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
