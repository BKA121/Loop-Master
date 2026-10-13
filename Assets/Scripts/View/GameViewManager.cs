using UnityEngine;
using LoopMaster.Core;
using LoopMaster.Data;

namespace LoopMaster.View
{
    /// <summary>
    /// Lớp quản lý view cấp cao nhất: chịu trách nhiệm DUY NHẤT gọi <see cref="LevelLoader"/> để load
    /// level data, dựng <see cref="ConveyorPath"/> MỘT LẦN, rồi truyền (inject) xuống từng view con
    /// (<see cref="ConveyorRunner"/>, <see cref="ConveyorLineView"/>; sau này thêm TrayView/BlockView...).
    ///
    /// Các view con không tự load level, không tự dựng ConveyorPath — chỉ nhận qua Initialize của
    /// chính nó, để tách rõ "ai load level & dựng quỹ đạo" (lớp này) khỏi "ai hiển thị/vận hành theo
    /// quỹ đạo đó" (các view con). Dựng chung 1 ConveyorPath cũng tránh parse/dựng Spline lặp lại.
    /// Hiện vẫn HARDCODE load level 1 để dựng core trước, giống LevelLoader.
    /// </summary>
    public class GameViewManager : MonoBehaviour
    {
        [SerializeField] private ConveyorRunner conveyorRunner;
        [SerializeField] private ConveyorLineView conveyorLineView;

        private LevelDataDTO _levelData;

        private void Awake()
        {
            _levelData = LevelLoader.LoadLevel1();
            if (_levelData == null)
            {
                Debug.LogError("[GameViewManager] Không load được level data.");
                return;
            }

            InitConveyor();
        }

        private void InitConveyor()
        {
            if (_levelData.p == null || _levelData.p.Count == 0)
            {
                Debug.LogError("[GameViewManager] Level data không có quỹ đạo băng chuyền (p rỗng).");
                return;
            }

            // Level hiện tại chỉ có 1 quỹ đạo (p[0]). Nếu sau này có nhiều băng chuyền, chỗ này cần
            // ánh xạ từng view với đúng phần tử trong levelData.p tương ứng.
            var conveyorPath = new ConveyorPath(_levelData.p[0]);

            if (conveyorRunner != null)
            {
                conveyorRunner.Initialize(conveyorPath, _levelData.ps);
            }
            else
            {
                Debug.LogWarning("[GameViewManager] Chưa gán ConveyorRunner trong Inspector.");
            }

            if (conveyorLineView != null)
            {
                conveyorLineView.Initialize(conveyorPath);
            }
            else
            {
                Debug.LogWarning("[GameViewManager] Chưa gán ConveyorLineView trong Inspector.");
            }
        }
    }
}
