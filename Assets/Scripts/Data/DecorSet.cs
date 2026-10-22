using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoopMaster.Data
{
    /// <summary>
    /// Bảng decor (tên trong level JSON → sprite/prefab). Tạo asset qua Create → LoopMaster → Decor Set,
    /// rồi thêm entry trong Inspector (ví dụ Decor1…Decor11 → decor_01…decor_11).
    /// </summary>
    [CreateAssetMenu(fileName = "DecorSet", menuName = "LoopMaster/Decor Set")]
    public class DecorSet : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("Tên decor đúng như trong level JSON (trường n), ví dụ Decor1, Enter, Exit")]
            public string name;

            [Tooltip("Sprite dựng nằm phẳng trên nền. Bỏ qua nếu đã gán prefab")]
            public Sprite sprite;

            [Tooltip("Tuỳ chọn: prefab dựng nguyên xi (ví dụ Enter/Exit có mesh cổng). Có prefab thì không dùng sprite")]
            public GameObject prefab;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, Entry> _lookup;

        public bool TryGet(string name, out Entry entry)
        {
            if (string.IsNullOrEmpty(name))
            {
                entry = default;
                return false;
            }

            if (_lookup == null)
            {
                _lookup = new Dictionary<string, Entry>();
                foreach (Entry e in entries)
                {
                    if (!string.IsNullOrEmpty(e.name))
                    {
                        _lookup[e.name] = e;
                    }
                }
            }

            return _lookup.TryGetValue(name, out entry);
        }

        private void OnValidate()
        {
            _lookup = null;
        }
    }
}
