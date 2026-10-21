using System.Collections.Generic;

namespace LoopMaster.Core
{
    public enum GameOutcome
    {
        Playing,
        Won,
        Lost
    }

    /// <summary>
    /// Luật thắng/thua (GAME_DESIGN.md mục 5), chỉ đọc dữ liệu logic — không phụ thuộc hiển thị.
    /// Thắng: hết khối cần xếp (khay trống thừa bị bỏ qua). Thua: băng chuyền đầy và không khối nào còn hấp thụ được;
    /// chỉ chốt thua khi không còn khối đang bay/đã được khay nhận trước (tránh thua oan giữa hoạt ảnh).
    /// </summary>
    public static class GameRules
    {
        public static GameOutcome Evaluate(ConveyorModel conveyor, IReadOnlyList<TrayModel> trays, bool anyTrayBusy)
        {
            if (conveyor == null || trays == null || trays.Count == 0)
            {
                return GameOutcome.Playing;
            }

            if (IsWon(conveyor, trays, anyTrayBusy))
            {
                return GameOutcome.Won;
            }

            if (conveyor.FreeSlots > 0 || anyTrayBusy || conveyor.Reserved > 0)
            {
                return GameOutcome.Playing;
            }

            foreach (TrayModel tray in trays)
            {
                if (tray.IncomingSlices > 0)
                {
                    return GameOutcome.Playing;
                }
            }

            foreach (ConveyorItem item in conveyor.Items)
            {
                if (item.claimedPort != -1)
                {
                    return GameOutcome.Playing;
                }

                foreach (TrayModel tray in trays)
                {
                    if (tray.AcceptableSlices(item.colorId) > 0)
                    {
                        return GameOutcome.Playing;
                    }
                }
            }

            return GameOutcome.Lost;
        }

        /// <summary>
        /// Hết khối cần xếp: băng chuyền trống, không khối nào đang bay, và mọi khay còn chứa khối đều đã hoàn thành.
        /// Khay trống thừa được bỏ qua.
        /// </summary>
        private static bool IsWon(ConveyorModel conveyor, IReadOnlyList<TrayModel> trays, bool anyTrayBusy)
        {
            if (anyTrayBusy || conveyor.Count > 0 || conveyor.Reserved > 0)
            {
                return false;
            }

            foreach (TrayModel tray in trays)
            {
                if (tray.IncomingSlices > 0)
                {
                    return false;
                }

                if (!tray.IsLocked && tray.Stack.Count > 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
