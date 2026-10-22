using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoopMaster.Data
{
    /// <summary>Các hiệu ứng particle trong game; thêm giá trị mới khi có particle mới rồi gán prefab trong <see cref="VfxSet"/>.</summary>
    public enum VfxType
    {
        TrayFinish,
    }

    /// <summary>
    /// Bảng hiệu ứng (loại → prefab particle). Tạo asset qua Create → LoopMaster → Vfx Set, rồi thêm entry trong Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "VfxSet", menuName = "LoopMaster/Vfx Set")]
    public class VfxSet : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public VfxType type;
            public GameObject prefab;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<VfxType, GameObject> _lookup;

        public GameObject Get(VfxType type)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<VfxType, GameObject>();
                foreach (Entry entry in entries)
                {
                    _lookup[entry.type] = entry.prefab;
                }
            }

            if (_lookup.TryGetValue(type, out GameObject prefab) && prefab != null)
            {
                return prefab;
            }

            Debug.LogWarning("[VfxSet] Không có prefab cho " + type);
            return null;
        }

        private void OnValidate()
        {
            _lookup = null;
        }
    }
}
