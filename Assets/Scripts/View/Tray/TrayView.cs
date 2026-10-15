using System.Collections.Generic;
using UnityEngine;
using LoopMaster.Core;
using LoopMaster.Data;

namespace LoopMaster.View
{
    /// <summary>
    /// Hiển thị 1 khay (instance prefab Box) theo <see cref="TrayModel"/>: đặt vị trí/xoay và dựng các khối lớn.
    /// Chỉ thể hiện dữ liệu — không tự thay đổi model. Nhận mọi thứ qua <see cref="Initialize"/>.
    ///
    /// Bố cục khối: prefab Box có chỗ khối đầu tiên ở local (0, 0.579, -1.69) (đầu xa mũi tên) và mũi tên đầu ra ở
    /// z = +2; các khối xếp dọc theo +Z, khối trên cùng của stack nằm gần mũi tên nhất.
    /// </summary>
    public class TrayView : MonoBehaviour
    {
        [SerializeField, Tooltip("Vị trí local của khối đầu tiên (đáy stack) trong khay")]
        private Vector3 firstBlockLocalPosition = new Vector3(0f, 0.579f, -1.69f);
        [SerializeField, Tooltip("Khoảng cách local giữa 2 khối liên tiếp theo trục Z của khay")]
        private float blockSpacing = 1.1f;
        [SerializeField, Tooltip("Chiều dài một khối lớn đầy đủ theo trục Z local của khay (dùng để neo khối mỏng sát khối bên dưới)")]
        private float blockLength = 1f;

        private readonly List<BigBlockView> _blocks = new List<BigBlockView>();
        private Transform _arrow;
        private BlockMaterialSet _materials;
        private GameObject _cubePrefab;

        public TrayModel Model { get; private set; }

        /// <summary>Mũi tên đầu ra (BoxArrow) — nguồn bắn tia lên băng chuyền.</summary>
        public Transform ArrowTransform => _arrow;

        /// <summary>Khay đang chạy hoạt ảnh tách khối — bỏ qua click cho tới khi xong.</summary>
        public bool IsBusy { get; set; }

        /// <summary>Khay cho phép bấm: không khóa, không đang tách khối ra và không còn khối nào đã nhận trước/đang bay vào.</summary>
        public bool CanInteract => Model != null && !Model.IsLocked && !IsBusy && Model.IncomingSlices == 0;

        /// <summary>Vị trí thế giới của ô thứ <paramref name="stackIndex"/> trong stack (đích bay của khối nhỏ).</summary>
        public Vector3 GetSlotWorldPosition(int stackIndex)
        {
            return transform.TransformPoint(firstBlockLocalPosition + Vector3.forward * (stackIndex * blockSpacing));
        }

        /// <summary>
        /// Một khối nhỏ đã bay tới khay: cập nhật model rồi hiển thị — dày thêm khối trên cùng đang dở, hoặc tạo khối lớn mới (mỏng 1/10).
        /// </summary>
        public void OnSliceArrived(int colorId)
        {
            Model.ApplyArrival(colorId);

            BigBlockView top = _blocks.Count > 0 ? _blocks[_blocks.Count - 1] : null;
            if (top != null && top.ColorId == colorId && top.Slices < BlockModel.SlicesPerBlock)
            {
                top.SetSlices(top.Slices + 1);
            }
            else
            {
                CreateBlock(_blocks.Count, colorId, 1);
            }
        }

        /// <summary>Lấy <paramref name="count"/> khối lớn trên cùng, thứ tự từ trên cùng xuống.</summary>
        public List<BigBlockView> GetTopBlocks(int count)
        {
            var result = new List<BigBlockView>(count);
            for (int i = 0; i < count && i < _blocks.Count; i++)
            {
                result.Add(_blocks[_blocks.Count - 1 - i]);
            }

            return result;
        }

        /// <summary>Bỏ <paramref name="count"/> khối trên cùng (đã lấy hết) khỏi danh sách view; GameObject do bên gọi hủy sau hoạt ảnh.</summary>
        public void RemoveTopBlocks(int count)
        {
            count = Mathf.Clamp(count, 0, _blocks.Count);
            _blocks.RemoveRange(_blocks.Count - count, count);
        }

        public void Initialize(TrayModel model, BlockMaterialSet materials, GameObject cubePrefab)
        {
            Model = model;
            transform.SetPositionAndRotation(model.Position, Quaternion.Euler(model.EulerAngles));
            _arrow = FindChildRecursive(transform, "BoxArrow");
            _materials = materials;
            _cubePrefab = cubePrefab;

            // Box gốc có sẵn 1 con "Cube" đang tắt dùng làm mẫu vị trí; ta dựng khối từ cubePrefab nên giữ nguyên tắt.
            for (int i = 0; i < model.Stack.Count; i++)
            {
                CreateBlock(i, model.Stack[i].colorId, model.Stack[i].slices);
            }
        }

        /// <summary>Dựng 1 khối lớn ở ô <paramref name="slotIndex"/> với <paramref name="slices"/>/10 độ dày.</summary>
        private void CreateBlock(int slotIndex, int colorId, int slices)
        {
            GameObject go = Instantiate(_cubePrefab, transform);
            go.name = "BigBlock_" + slotIndex;
            go.transform.localPosition = firstBlockLocalPosition + Vector3.forward * (slotIndex * blockSpacing);
            go.transform.localRotation = Quaternion.identity;

            BigBlockView view = go.GetComponent<BigBlockView>();
            if (view == null)
            {
                view = go.AddComponent<BigBlockView>();
            }

            view.Initialize(blockLength);
            view.ColorId = colorId;
            view.SetSlices(slices);
            view.SetMaterial(_materials != null ? _materials.Get(colorId) : null);
            _blocks.Add(view);
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            foreach (Transform child in root)
            {
                if (child.name == childName) return child;
                Transform found = FindChildRecursive(child, childName);
                if (found != null) return found;
            }

            return null;
        }
    }
}
