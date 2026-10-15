using UnityEngine;
using LoopMaster.Core;
using LoopMaster.Data;

namespace LoopMaster.View
{
    /// <summary>
    /// Lớp quản lý view cấp cao nhất: chịu trách nhiệm DUY NHẤT gọi <see cref="LevelLoader"/> để load
    /// level data, dựng <see cref="ConveyorPath"/> và các khay MỘT LẦN, rồi truyền (inject) xuống từng view con
    /// (<see cref="ConveyorView"/>, <see cref="ConveyorLineView"/>, <see cref="BigBlockSplitter"/>,
    /// <see cref="TrayAbsorber"/>, <see cref="TrayClickController"/>).
    ///
    /// Các view con không tự load level, không tự dựng ConveyorPath — chỉ nhận qua Initialize của
    /// chính nó, để tách rõ "ai load level & dựng quỹ đạo" (lớp này) khỏi "ai hiển thị/vận hành theo
    /// quỹ đạo đó" (các view con). Dựng chung 1 ConveyorPath cũng tránh parse/dựng Spline lặp lại.
    /// Hiện vẫn HARDCODE load level 1 để dựng core trước, giống LevelLoader.
    /// </summary>
    public class GameViewManager : MonoBehaviour
    {
        [SerializeField] private ConveyorLineView conveyorLineView;

        [SerializeField] private ConveyorView conveyorView;
        [SerializeField] private SlicePool slicePool;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("testSplitter")]
        private BigBlockSplitter splitter;
        [SerializeField, Tooltip("Hấp thụ khối nhỏ từ băng chuyền vào khay")] private TrayAbsorber trayAbsorber;
        [SerializeField, Tooltip("Nhận bấm khay và đưa khối lên băng chuyền")] private TrayClickController trayClickController;

        [SerializeField, Min(0f), Tooltip("Tốc độ băng chuyền (đơn vị/giây). Level data không có tốc độ nên đặt ở đây")]
        private float conveyorSpeed = 4f;

        [Header("Tray")]
        [SerializeField, Tooltip("Prefab Box (khay)")] private GameObject boxPrefab;
        [SerializeField, Tooltip("Prefab Cube (khối lớn)")] private GameObject cubePrefab;
        [SerializeField, Tooltip("Material theo mã màu (id → material)")] private BlockMaterialSet blockMaterials;
        [SerializeField, Tooltip("Cha của các khay được dựng; để trống = GameViewManager")] private Transform trayRoot;

        private readonly System.Collections.Generic.List<TrayView> _trayViews = new System.Collections.Generic.List<TrayView>();
        private ConveyorPath _conveyorPath;
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
            InitTrays();
        }

        private void InitTrays()
        {
            if (boxPrefab == null || cubePrefab == null)
            {
                Debug.LogWarning("[GameViewManager] Chưa gán Box Prefab / Cube Prefab nên không dựng khay.");
                return;
            }

            if (_levelData.b == null)
            {
                return;
            }

            Transform parent = trayRoot != null ? trayRoot : transform;
            foreach (TrayDTO trayData in _levelData.b)
            {
                TrayModel model = TrayModel.FromDto(trayData);
                GameObject go = Instantiate(boxPrefab, parent);
                go.name = "Tray_" + model.Id;

                TrayView view = go.GetComponent<TrayView>();
                if (view == null)
                {
                    view = go.AddComponent<TrayView>();
                }

                view.Initialize(model, blockMaterials, cubePrefab);
                _trayViews.Add(view);
            }

            if (trayAbsorber != null)
            {
                trayAbsorber.Initialize(_conveyorPath, conveyorView, slicePool, _trayViews);
            }
            else
            {
                Debug.LogWarning("[GameViewManager] Chưa gán TrayAbsorber nên khay không hấp thụ khối từ băng chuyền.");
            }

            if (trayClickController != null)
            {
                trayClickController.Initialize(conveyorView, splitter);
            }
            else
            {
                Debug.LogWarning("[GameViewManager] Chưa gán TrayClickController nên không bấm khay được.");
            }
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
            _conveyorPath = conveyorPath;

            if (conveyorView != null && slicePool != null)
            {
                // Sức chứa = l khối lớn × 10 khối nhỏ (GAME_DESIGN.md); level không khai báo l thì dùng 70.
                int capacity = _levelData.l > 0 ? _levelData.l * BlockModel.SlicesPerBlock : 70;
                conveyorView.Initialize(conveyorPath, conveyorSpeed, new ConveyorModel(capacity), slicePool);

                if (splitter != null)
                {
                    splitter.Initialize(conveyorPath, conveyorView, slicePool, blockMaterials);
                }
            }
            else if (splitter != null)
            {
                Debug.LogWarning("[GameViewManager] Thiếu ConveyorView hoặc SlicePool nên splitter không chạy.");
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
