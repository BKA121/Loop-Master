using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoopMaster.Data
{
    /// <summary>
    /// Nơi chứa material của khối theo mã màu (<c>BlockDTO.t</c>). Tạo asset qua Create → LoopMaster → Block Material Set,
    /// rồi thêm entry (id → material) trong Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "BlockMaterialSet", menuName = "LoopMaster/Block Material Set")]
    public class BlockMaterialSet : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public int id;
            public Material material;
            public Sprite icon;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<int, Entry> _lookup;

        private void EnsureLookup()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<int, Entry>();
            foreach (Entry entry in entries)
            {
                _lookup[entry.id] = entry;
            }
        }

        public Material Get(int colorId)
        {
            EnsureLookup();

            if (_lookup.TryGetValue(colorId, out Entry entry) && entry.material != null)
            {
                return entry.material;
            }

            Debug.LogWarning("[BlockMaterialSet] Không có material cho colorId = " + colorId);
            return null;
        }

        /// <summary>Icon của màu; null nếu chưa gán (không cảnh báo vì icon là tùy chọn).</summary>
        public Sprite GetIcon(int colorId)
        {
            EnsureLookup();
            return _lookup.TryGetValue(colorId, out Entry entry) ? entry.icon : null;
        }

        private void OnValidate()
        {
            _lookup = null;
        }
    }
}
