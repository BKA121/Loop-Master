using System.Collections.Generic;

namespace LoopMaster.Core
{
    /// <summary>
    /// Một khối nhỏ đang nằm trên băng chuyền. <see cref="distance"/> dùng cùng quy ước với
    /// <see cref="ConveyorPath.GetPointAtDistance"/>.
    /// </summary>
    public class ConveyorItem
    {
        public int id;
        public int colorId;
        public float distance;
    }

    /// <summary>
    /// Dữ liệu logic của băng chuyền: danh sách khối nhỏ + sức chứa. Không phụ thuộc Unity/hiển thị.
    /// Khối đang bay lên được "giữ chỗ" (Reserve) để tổng khối không bao giờ vượt <see cref="Capacity"/>.
    /// </summary>
    public class ConveyorModel
    {
        private readonly List<ConveyorItem> _items = new List<ConveyorItem>();
        private int _nextId = 1;
        private int _reserved;

        public int Capacity { get; }
        public int Count => _items.Count;
        public int Reserved => _reserved;
        public IReadOnlyList<ConveyorItem> Items => _items;

        public ConveyorModel(int capacity)
        {
            Capacity = capacity;
        }

        /// <summary>Giữ chỗ cho <paramref name="amount"/> khối sắp lên băng chuyền. False nếu không đủ chỗ.</summary>
        public bool TryReserve(int amount)
        {
            if (amount <= 0 || _items.Count + _reserved + amount > Capacity)
            {
                return false;
            }

            _reserved += amount;
            return true;
        }

        /// <summary>Đặt 1 khối đã giữ chỗ lên băng chuyền, trả về id của khối.</summary>
        public int AddReserved(int colorId, float distance)
        {
            if (_reserved > 0)
            {
                _reserved--;
            }

            var item = new ConveyorItem { id = _nextId++, colorId = colorId, distance = distance };
            _items.Add(item);
            return item.id;
        }

        public bool Remove(int id)
        {
            int index = _items.FindIndex(i => i.id == id);
            if (index < 0)
            {
                return false;
            }

            _items.RemoveAt(index);
            return true;
        }
    }
}
