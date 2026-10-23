using UnityEngine;
using UnityEngine.Pool;

namespace LoopMaster.View
{
    /// <summary>
    /// Object pool cho particle nổ nhỏ (BurstBubbles) phát mỗi khi một khối nhỏ hạ cánh vào khay: số lượng nhiều nên không Instantiate/Destroy.
    /// Mỗi lần phát đổi <c>startColor</c> theo màu khối; particle tự trả về pool khi chạy xong (stopAction = Callback, đặt lúc chạy
    /// nên không phải sửa prefab, vốn dùng Destroy).
    /// </summary>
    public class BurstPool : MonoBehaviour
    {
        [SerializeField, Tooltip("Prefab BurstBubbles (ParticleSystem ở root)")] private ParticleSystem prefab;
        [SerializeField, Min(1)] private int defaultCapacity = 20;
        [SerializeField, Min(1)] private int maxSize = 60;

        private ObjectPool<ParticleSystem> _pool;

        private void Start()
        {
            Prewarm(defaultCapacity);
        }

        private void EnsurePool()
        {
            if (_pool != null)
            {
                return;
            }

            _pool = new ObjectPool<ParticleSystem>(
                createFunc: () =>
                {
                    ParticleSystem ps = Instantiate(prefab, transform);
                    ParticleSystem.MainModule main = ps.main;
                    main.stopAction = ParticleSystemStopAction.Callback;
                    main.playOnAwake = false;
                    ps.gameObject.AddComponent<PooledParticle>().Init(this, ps);
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    return ps;
                },
                actionOnGet: ps => ps.gameObject.SetActive(true),
                actionOnRelease: ps => ps.gameObject.SetActive(false),
                actionOnDestroy: ps => { if (ps != null) Destroy(ps.gameObject); },
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        /// <summary>Tạo sẵn <paramref name="count"/> particle (tắt); bỏ qua nếu chưa gán prefab.</summary>
        public void Prewarm(int count)
        {
            if (prefab == null)
            {
                return;
            }

            EnsurePool();
            var temp = new ParticleSystem[count];
            for (int i = 0; i < count; i++) temp[i] = _pool.Get();
            for (int i = 0; i < count; i++) _pool.Release(temp[i]);
        }

        /// <summary>Nổ một lần tại <paramref name="position"/> với màu <paramref name="color"/>; bỏ qua (kèm cảnh báo) nếu chưa gán prefab.</summary>
        public void Play(Vector3 position, Color color)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[BurstPool] Chưa gán Prefab (BurstBubbles).");
                return;
            }

            EnsurePool();
            ParticleSystem ps = _pool.Get();

            // Giữ góc xoay và độ nâng đã chỉnh trên prefab (cùng quy ước với EffectPlayer).
            ps.transform.SetPositionAndRotation(position + prefab.transform.position, prefab.transform.rotation);

            ParticleSystem.MainModule main = ps.main;
            main.startColor = color;

            ps.GetComponent<PooledParticle>().Busy = true;
            ps.Clear();
            ps.Play();
        }

        private void Release(PooledParticle pooled, ParticleSystem ps)
        {
            // Callback cũng bắn khi tạo sẵn/tắt object; chỉ trả về pool khi đang thực sự phát, tránh Release hai lần.
            if (_pool != null && pooled.Busy)
            {
                pooled.Busy = false;
                _pool.Release(ps);
            }
        }

        /// <summary>Gắn vào mỗi particle pooled: khi chạy xong (stopAction = Callback) thì trả về pool.</summary>
        private class PooledParticle : MonoBehaviour
        {
            private BurstPool _owner;
            private ParticleSystem _ps;

            /// <summary>true từ lúc bắt đầu phát đến lúc trả về pool.</summary>
            public bool Busy { get; set; }

            public void Init(BurstPool owner, ParticleSystem ps)
            {
                _owner = owner;
                _ps = ps;
            }

            private void OnParticleSystemStopped()
            {
                _owner.Release(this, _ps);
            }
        }
    }
}
