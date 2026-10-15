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

        /// <summary>Chỉ số cổng (mũi tên khay) đã nhận trước khối này; -1 nếu chưa khay nào nhận.</summary>
        public int claimedPort = -1;
    }

    /// <summary>Một khối vừa vượt qua một cổng (mũi tên khay) trong lần <see cref="ConveyorModel.Advance"/> gần nhất.</summary>
    public class PortCrossing
    {
        public ConveyorItem item;
        public int portIndex;

        /// <summary>Quãng đường khối đã vượt qua cổng trong frame này (nhỏ hơn = đi trước).</summary>
        public float overshoot;
    }

    /// <summary>
    /// Rào chắn tại điểm hạ cánh của các khối đang bay lên: mở từ lúc bấm khay tới khi khối cuối hạ cánh,
    /// trong lúc đó không cho khối nào từ phía sau chạy vào sát điểm hạ cánh.
    /// </summary>
    public class InsertionGate
    {
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
        private readonly List<InsertionGate> _gates = new List<InsertionGate>();
        private readonly List<ConveyorItem> _order = new List<ConveyorItem>();
        private readonly List<PortCrossing> _crossingPool = new List<PortCrossing>();
        private bool[] _stopped = new bool[0];

        public int Capacity { get; }
        public int Count => _items.Count;
        public int Reserved => _reserved;

        /// <summary>Số chỗ còn trống (đã trừ khối đang nằm trên băng chuyền và khối đã giữ chỗ).</summary>
        public int FreeSlots => Capacity - _items.Count - _reserved;
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

        /// <summary>Mở rào chắn tại điểm hạ cánh; nhớ <see cref="CloseGate"/> khi khối cuối cùng đã hạ cánh.</summary>
        public InsertionGate OpenGate(float landDistance)
        {
            var gate = new InsertionGate { distance = landDistance };
            _gates.Add(gate);
            return gate;
        }

        public void CloseGate(InsertionGate gate)
        {
            _gates.Remove(gate);
        }

        /// <summary>
        /// Cho các khối tiến lên <paramref name="delta"/>. Khi có rào mở, khối đến từ phía sau không được vào đoạn
        /// <paramref name="landingClearance"/> ngay trước điểm hạ cánh (khối đã nằm trong đoạn đó hoặc đã qua điểm
        /// hạ cánh đi tiếp bình thường). Một khối cũng đứng yên nếu khối ngay phía trước nó đang đứng yên và khe hở
        /// ≤ <paramref name="maxGap"/> (đuôi dãy dồn lại, không đè lên nhau).
        /// </summary>
        /// <param name="ports">Khoảng cách trên quỹ đạo của các cổng; khối nào vượt qua một cổng trong frame này được ghi vào <paramref name="crossings"/> (đi trước xếp trước).</param>
        public void Advance(float delta, float pathLength, float maxGap, float landingClearance,
            IReadOnlyList<float> ports, List<PortCrossing> crossings)
        {
            crossings.Clear();

            int n = _items.Count;
            if (n == 0)
            {
                return;
            }

            // Tái dùng bộ đệm để không sinh rác mỗi frame.
            _order.Clear();
            _order.AddRange(_items);
            _order.Sort(CompareByDistance);
            List<ConveyorItem> order = _order;

            if (_stopped.Length < n)
            {
                _stopped = new bool[n * 2];
            }

            bool[] stopped = _stopped;
            for (int k = 0; k < n; k++)
            {
                stopped[k] = IsBlockedByGate(order[k].distance, delta, pathLength, landingClearance);
            }

            if (n > 1)
            {
                // Hai vòng duyệt ngược để lan truyền "dừng" về sau, kể cả khi vòng quay qua điểm 0.
                for (int step = 0; step < 2 * n; step++)
                {
                    int k = (n - 1 - step % n + n) % n;
                    int next = (k + 1) % n;
                    float gap = ((order[next].distance - order[k].distance) % pathLength + pathLength) % pathLength;
                    if (stopped[next] && gap <= maxGap)
                    {
                        stopped[k] = true;
                    }
                }
            }

            for (int k = 0; k < n; k++)
            {
                if (stopped[k])
                {
                    continue;
                }

                ConveyorItem item = order[k];
                if (ports != null)
                {
                    for (int p = 0; p < ports.Count; p++)
                    {
                        float back = ((ports[p] - item.distance) % pathLength + pathLength) % pathLength;
                        if (back > 0f && back <= delta)
                        {
                            crossings.Add(GetCrossing(crossings.Count, item, p, delta - back));
                        }
                    }
                }

                item.distance = (item.distance + delta) % pathLength;
            }

            if (crossings.Count > 1)
            {
                crossings.Sort(CompareCrossings);
            }
        }

        /// <summary>
        /// Nhận trước cả cụm khối nối đuôi liên tục sau <paramref name="trigger"/> (khối vừa vượt cổng) cho cổng
        /// <paramref name="portIndex"/>: cùng màu, chưa khay nào nhận, khe với khối trước trong cụm ≤ <paramref name="maxGap"/>,
        /// tối đa <paramref name="maxCount"/> khối (tính cả trigger). Trả về số khối đã nhận.
        /// </summary>
        public int ClaimRun(ConveyorItem trigger, int portIndex, float pathLength, float maxGap, int maxCount)
        {
            trigger.claimedPort = portIndex;
            int count = 1;
            ConveyorItem prev = trigger;

            while (count < maxCount)
            {
                ConveyorItem next = null;
                float nextGap = float.MaxValue;
                foreach (ConveyorItem item in _items)
                {
                    if (item == prev)
                    {
                        continue;
                    }

                    float gap = ((prev.distance - item.distance) % pathLength + pathLength) % pathLength;
                    if (gap < nextGap)
                    {
                        nextGap = gap;
                        next = item;
                    }
                }

                if (next == null || next == trigger || nextGap > maxGap
                    || next.claimedPort != -1 || next.colorId != trigger.colorId)
                {
                    break;
                }

                next.claimedPort = portIndex;
                count++;
                prev = next;
            }

            return count;
        }

        private PortCrossing GetCrossing(int index, ConveyorItem item, int portIndex, float overshoot)
        {
            while (_crossingPool.Count <= index)
            {
                _crossingPool.Add(new PortCrossing());
            }

            PortCrossing crossing = _crossingPool[index];
            crossing.item = item;
            crossing.portIndex = portIndex;
            crossing.overshoot = overshoot;
            return crossing;
        }

        private static int CompareByDistance(ConveyorItem a, ConveyorItem b) => a.distance.CompareTo(b.distance);

        private static int CompareCrossings(PortCrossing a, PortCrossing b)
        {
            int byPort = a.portIndex.CompareTo(b.portIndex);
            return byPort != 0 ? byPort : b.overshoot.CompareTo(a.overshoot);
        }

        private bool IsBlockedByGate(float distance, float delta, float pathLength, float clearance)
        {
            foreach (InsertionGate gate in _gates)
            {
                float back = ((gate.distance - distance) % pathLength + pathLength) % pathLength;
                if (back > clearance && back - delta <= clearance)
                {
                    return true;
                }
            }

            return false;
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
