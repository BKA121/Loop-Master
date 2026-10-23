using System.Collections.Generic;
using UnityEngine;
using LoopMaster.Audio;
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
        [SerializeField, Tooltip("Lệch local của khối hoàn thành so với tâm 4 ô (tinh chỉnh khi mesh CubeFinish không khớp)")]
        private Vector3 finishLocalOffset = Vector3.zero;

        private readonly List<BigBlockView> _blocks = new List<BigBlockView>();
        private Transform _arrow;
        private BlockMaterialSet _materials;
        private GameObject _cubePrefab;
        private int _flyingCount;
        private SlicePool _pool;
        private EffectPlayer _effects;
        private BurstPool _burst;
        private BigBlockView _finishBlock;

        public TrayModel Model { get; private set; }

        /// <summary>Mũi tên đầu ra (BoxArrow) — nguồn bắn tia lên băng chuyền.</summary>
        public Transform ArrowTransform => _arrow;

        /// <summary>Khay đang chạy hoạt ảnh tách khối — bỏ qua click cho tới khi xong.</summary>
        public bool IsBusy { get; set; }

        /// <summary>Khay cho phép bấm: không khóa, không đang tách khối ra và không còn khối nào đã nhận trước/đang bay vào.</summary>
        public bool CanInteract => Model != null && !Model.IsLocked && !IsBusy && Model.IncomingSlices == 0;

        /// <summary>
        /// Cấp đích bay cho một khối nhỏ sắp vào khay: mép ngoài (phía +Z) của lát kế tiếp trong khối lớn đang dày dần.
        /// Coi khay là dãy lát liên tiếp (10 lát/khối); tính cả các khối nhỏ đã được cấp đích nhưng chưa hạ cánh
        /// để những khối bay cùng lúc không trùng đích. Mỗi lần gọi tương ứng đúng một lần <see cref="OnSliceArrived"/>.
        /// </summary>
        public Vector3 ReserveLandingPosition()
        {
            int stackCount = Model.Stack.Count;
            BlockModel top = stackCount > 0 ? Model.Stack[stackCount - 1] : null;
            bool topPartial = top != null && top.slices < BlockModel.SlicesPerBlock;
            int baseCount = topPartial
                ? (stackCount - 1) * BlockModel.SlicesPerBlock + top.slices
                : stackCount * BlockModel.SlicesPerBlock;

            int index = baseCount + _flyingCount;
            int slot = index / BlockModel.SlicesPerBlock;
            int within = index % BlockModel.SlicesPerBlock;
            _flyingCount++;

            // Khối neo ở −Z (xem BigBlockView.SetFill): lát thứ `within` nằm ở (within + 0.5)/10 chiều dài tính từ mép −Z.
            float offsetZ = -blockLength * 0.5f + (within + 0.5f) / BlockModel.SlicesPerBlock * blockLength;
            Vector3 local = firstBlockLocalPosition + Vector3.forward * (slot * blockSpacing + offsetZ);
            return transform.TransformPoint(local);
        }

        /// <summary>
        /// Một khối nhỏ đã bay tới khay: cập nhật model rồi hiển thị — dày thêm khối trên cùng đang dở, hoặc tạo khối lớn mới (mỏng 1/10).
        /// </summary>
        public void OnSliceArrived(int colorId, Vector3 landingPosition)
        {
            if (_flyingCount > 0)
            {
                _flyingCount--;
            }

            if (_burst != null)
            {
                _burst.Play(landingPosition, _materials != null ? _materials.GetColor(colorId) : Color.white);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(SfxType.SliceToTray);
            }

            Model.ApplyArrival(colorId);

            if (Model.IsLocked)
            {
                ShowFinishBlock(colorId);
                return;
            }

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

        /// <summary>Khay vừa đủ 4 khối cùng màu: bỏ các khối lớn cũ, thay bằng một khối hoàn thành (từ pool) ở giữa 4 ô.</summary>
        private void ShowFinishBlock(int colorId)
        {
            if (_finishBlock != null || _pool == null)
            {
                return;
            }

            BigBlockView finish = _pool.GetFinish();
            if (finish == null)
            {
                return;
            }

            foreach (BigBlockView block in _blocks)
            {
                Destroy(block.gameObject);
            }

            _blocks.Clear();

            Transform t = finish.transform;
            t.SetParent(transform, false);
            t.localPosition = firstBlockLocalPosition + Vector3.forward * (1.5f * blockSpacing) + finishLocalOffset;
            t.localRotation = Quaternion.identity;

            finish.ColorId = colorId;
            finish.SetMaterial(_materials != null ? _materials.Get(colorId) : null);
            _finishBlock = finish;

            // Không gắn làm con của khay/khối: hiệu ứng không xoay theo khay; stopAction = Destroy trên prefab tự dọn sau khi chạy xong.
            if (_effects != null)
            {
                _effects.Play(VfxType.TrayFinish, t.position);
            }
        }

        public void Initialize(TrayModel model, BlockMaterialSet materials, GameObject cubePrefab, SlicePool pool, EffectPlayer effects, BurstPool burst)
        {
            Model = model;
            _pool = pool;
            _effects = effects;
            _burst = burst;
            transform.SetPositionAndRotation(model.Position, Quaternion.Euler(model.EulerAngles));
            _arrow = FindChildRecursive(transform, "BoxArrow");
            _materials = materials;
            _cubePrefab = cubePrefab;

            // Box gốc có sẵn 1 con "Cube" đang tắt dùng làm mẫu vị trí; ta dựng khối từ cubePrefab nên giữ nguyên tắt.
            for (int i = 0; i < model.Stack.Count; i++)
            {
                CreateBlock(i, model.Stack[i].colorId, model.Stack[i].slices, model.Stack[i].isHidden);
            }
        }

        /// <summary>
        /// Khối trên cùng (view) đang ẩn mà model đã lộ màu → đổi sang material/icon thật và phát hiệu ứng.
        /// Gọi sau khi khối phía trên đã bay đi hết.
        /// </summary>
        public void SyncReveal()
        {
            BigBlockView top = _blocks.Count > 0 ? _blocks[_blocks.Count - 1] : null;
            if (top == null || !top.IsHidden)
            {
                return;
            }

            top.IsHidden = false;
            top.SetMaterial(_materials != null ? _materials.Get(top.ColorId) : null);
            top.SetIcon(_materials != null ? _materials.GetIcon(top.ColorId) : null);

            if (_effects != null)
            {
                _effects.Play(VfxType.SecretReveal, top.transform.position);
            }
        }

        /// <summary>Dựng 1 khối lớn ở ô <paramref name="slotIndex"/> với <paramref name="slices"/>/10 độ dày.</summary>
        private void CreateBlock(int slotIndex, int colorId, int slices, bool hidden = false)
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
            view.IsHidden = hidden;
            if (hidden && _materials != null)
            {
                view.SetMaterial(_materials.SecretMaterial);
                view.SetIcon(_materials.SecretIcon);
            }
            else
            {
                view.SetMaterial(_materials != null ? _materials.Get(colorId) : null);
                view.SetIcon(_materials != null ? _materials.GetIcon(colorId) : null);
            }

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
