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

        private ObjectPool<SliceView> _pool;

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
