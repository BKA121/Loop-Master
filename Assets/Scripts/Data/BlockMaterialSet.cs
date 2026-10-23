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

        [Header("Khối bí ẩn (chưa lộ màu)")]
        [SerializeField, Tooltip("Material đen xám thay màu thật khi khối còn ẩn")] private Material secretMaterial;
        [SerializeField, Tooltip("Icon dấu hỏi chấm hiện trên khối còn ẩn")] private Sprite secretIcon;

        public Material SecretMaterial => secretMaterial;
        public Sprite SecretIcon => secretIcon;

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

        /// <summary>
        /// Màu hiển thị của khối (để nhuộm particle...): đọc <c>_BaseColor</c> của material toon, không có thì dùng <c>material.color</c>.
        /// Trắng nếu chưa có material cho màu này.
        /// </summary>
        public Color GetColor(int colorId)
        {
            Material material = Get(colorId);
            if (material == null)
            {
                return Color.white;
            }

            return material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
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
