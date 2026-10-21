using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;

namespace LoopMaster.View
{
    /// <summary>
    /// Object pool cho <see cref="SliceView"/>, bọc <see cref="ObjectPool{T}"/> của Unity.
    /// Pool được tạo lười (lần đầu Get/Release/Prewarm) nên không phụ thuộc thứ tự Awake giữa các object.
    /// Nếu chưa gán prefab, tạm dựng 1 khối cube nhỏ ngay lúc chạy để test; gán prefab thật để thay thế.
    /// </summary>
    public class SlicePool : MonoBehaviour
    {
        [SerializeField, Tooltip("Để trống = tự dựng cube nhỏ để test")] private SliceView prefab;
        [SerializeField, Min(1)] private int defaultCapacity = 20;
        [SerializeField, Min(1)] private int maxSize = 100;
        [SerializeField, Tooltip("Kích thước cube tạm khi không gán prefab")] private float fallbackSize = 0.3f;

        [Header("Khối hoàn thành")]
        [SerializeField, Tooltip("Prefab CubeFinish — khối lớn thay khay vừa đủ 4 khối cùng màu")] private GameObject finishPrefab;
        [SerializeField, Min(1), Tooltip("Số khối hoàn thành tạo sẵn và sức chứa tối đa của pool")] private int finishCapacity = 12;

        private ObjectPool<SliceView> _pool;
        private ObjectPool<BigBlockView> _finishPool;

        private void Start()
        {
            PrewarmFinish(finishCapacity);
        }

        private void EnsureFinishPool()
        {
            if (_finishPool != null)
            {
                return;
            }

            _finishPool = new ObjectPool<BigBlockView>(
                createFunc: () =>
                {
                    GameObject go = Instantiate(finishPrefab, transform);
                    BigBlockView view = go.GetComponent<BigBlockView>();
                    return view != null ? view : go.AddComponent<BigBlockView>();
                },
                actionOnGet: v => v.gameObject.SetActive(true),
                actionOnRelease: v =>
                {
                    DOTween.Kill(v.transform);
                    v.transform.SetParent(transform, false);
                    v.gameObject.SetActive(false);
                },
                actionOnDestroy: v => { if (v != null) Destroy(v.gameObject); },
                collectionCheck: true,
                defaultCapacity: finishCapacity,
                maxSize: finishCapacity);
        }

        /// <summary>Tạo sẵn <paramref name="count"/> khối hoàn thành (tắt); bỏ qua nếu chưa gán prefab.</summary>
        public void PrewarmFinish(int count)
        {
            if (finishPrefab == null)
            {
                return;
            }

            EnsureFinishPool();
            var temp = new BigBlockView[count];
            for (int i = 0; i < count; i++) temp[i] = _finishPool.Get();
            for (int i = 0; i < count; i++) _finishPool.Release(temp[i]);
        }

        /// <summary>Lấy một khối hoàn thành; null (kèm cảnh báo) nếu chưa gán prefab.</summary>
        public BigBlockView GetFinish()
        {
            if (finishPrefab == null)
            {
                Debug.LogWarning("[SlicePool] Chưa gán Finish Prefab (CubeFinish).");
                return null;
            }

            EnsureFinishPool();
            return _finishPool.Get();
        }

        public void ReleaseFinish(BigBlockView view)
        {
            EnsureFinishPool();
            _finishPool.Release(view);
        }

        private void EnsurePool()
        {
            if (_pool != null)
            {
                return;
            }

            if (prefab == null)
            {
                prefab = BuildFallbackPrefab();
            }

            _pool = new ObjectPool<SliceView>(
                createFunc: () => Instantiate(prefab, transform),
                actionOnGet: s => s.gameObject.SetActive(true),
                actionOnRelease: s =>
                {
                    DOTween.Kill(s.transform);
                    s.gameObject.SetActive(false);
                },
                actionOnDestroy: s => { if (s != null) Destroy(s.gameObject); },
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        private SliceView BuildFallbackPrefab()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "SliceTemplate";
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * fallbackSize;
            Destroy(go.GetComponent<Collider>());
            go.SetActive(false);
            return go.AddComponent<SliceView>();
        }

        /// <summary>Tạo sẵn <paramref name="count"/> slice để tránh giật khi lần đầu cần dùng.</summary>
        public void Prewarm(int count)
        {
            EnsurePool();
            var temp = new SliceView[count];
            for (int i = 0; i < count; i++) temp[i] = _pool.Get();
            for (int i = 0; i < count; i++) _pool.Release(temp[i]);
        }

        public SliceView Get(Vector3 position, Quaternion rotation)
        {
            EnsurePool();
            SliceView slice = _pool.Get();
            slice.ResetState();
            slice.transform.SetPositionAndRotation(position, rotation);
            return slice;
        }

        public void Release(SliceView slice)
        {
            EnsurePool();
            _pool.Release(slice);
        }

        public int CountActive => _pool?.CountActive ?? 0;
        public int CountAll => _pool?.CountAll ?? 0;
    }
}
