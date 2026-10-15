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
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<int, Material> _lookup;

        public Material Get(int colorId)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<int, Material>();
                foreach (Entry entry in entries)
                {
                    _lookup[entry.id] = entry.material;
                }
            }

            if (_lookup.TryGetValue(colorId, out Material material) && material != null)
            {
                return material;
            }

            Debug.LogWarning("[BlockMaterialSet] Không có material cho colorId = " + colorId);
            return null;
        }

        private void OnValidate()
        {
            _lookup = null;
        }
    }
}
