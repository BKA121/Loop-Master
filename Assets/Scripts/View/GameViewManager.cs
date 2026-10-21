using UnityEngine;
using LoopMaster.Core;
using LoopMaster.Data;

namespace LoopMaster.View
{
    /// <summary>
    /// Lớp quản lý view cấp cao nhất: chịu trách nhiệm DUY NHẤT gọi <see cref="LevelLoader"/> để load
    /// level data, áp Camera Size của level, dựng <see cref="ConveyorPath"/> và các khay MỘT LẦN, rồi truyền (inject) xuống từng view con
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
        [SerializeField, Tooltip("Camera màn chơi; để trống = Camera.main")] private Camera gameCamera;

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
        private readonly System.Collections.Generic.List<TrayModel> _trayModels = new System.Collections.Generic.List<TrayModel>();
        private ConveyorPath _conveyorPath;
        private ConveyorModel _conveyorModel;
        private LevelDataDTO _levelData;

        public GameOutcome Outcome { get; private set; }

        /// <summary>Bắn đúng một lần khi thắng/thua; UI kết quả sau này đăng ký ở đây.</summary>
        public event System.Action<GameOutcome> OnGameEnded;

        private void Awake()
        {
            _levelData = LevelLoader.LoadLevel1();
            if (_levelData == null)
            {
                Debug.LogError("[GameViewManager] Không load được level data.");
                return;
            }

            ApplyCameraSize();
            InitConveyor();
            InitTrays();
        }

        /// <summary>Áp Camera Size của level (<c>s</c>) vào camera orthographic; level không khai báo thì giữ size hiện có.</summary>
        private void ApplyCameraSize()
        {
            Camera cam = gameCamera != null ? gameCamera : Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[GameViewManager] Không tìm thấy camera để áp Camera Size của level.");
                return;
            }

            if (_levelData.s <= 0)
            {
                return;
            }

            if (!cam.orthographic)
            {
                Debug.LogWarning("[GameViewManager] Camera không phải orthographic nên không áp Camera Size.");
                return;
            }

            cam.orthographicSize = _levelData.s;
        }

        private void Update()
        {
            if (Outcome != GameOutcome.Playing || _conveyorModel == null)
            {
                return;
            }

            bool anyBusy = false;
            foreach (TrayView view in _trayViews)
            {
                if (view.IsBusy)
                {
                    anyBusy = true;
                    break;
                }
            }

            GameOutcome result = GameRules.Evaluate(_conveyorModel, _trayModels, anyBusy);
            if (result != GameOutcome.Playing)
            {
                EndGame(result);
            }
        }

        private void EndGame(GameOutcome result)
        {
            Outcome = result;

            if (result == GameOutcome.Won)
            {
                int locked = 0;
                foreach (TrayModel tray in _trayModels)
                {
                    if (tray.IsLocked) locked++;
                }

                Debug.Log($"[GameViewManager] THẮNG: hoàn thành {locked}/{_trayModels.Count} khay (khay thừa bỏ qua).");
            }
            else
            {
                Debug.Log($"[GameViewManager] THUA: băng chuyền đầy ({_conveyorModel.Count}/{_conveyorModel.Capacity}) và không khối nào hấp thụ được.");
            }

            // Kết thúc gameplay: không cho bấm khay và đóng băng băng chuyền.
            if (trayClickController != null) trayClickController.enabled = false;
            if (conveyorView != null) conveyorView.enabled = false;

            OnGameEnded?.Invoke(result);
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

                view.Initialize(model, blockMaterials, cubePrefab, slicePool);
                _trayViews.Add(view);
                _trayModels.Add(model);
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
                _conveyorModel = new ConveyorModel(capacity);
                conveyorView.Initialize(conveyorPath, conveyorSpeed, _conveyorModel, slicePool);

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
