using UnityEngine;

namespace LoopMaster.View
{
    /// <summary>
    /// Khối lớn hiển thị trong khay (gắn lên instance prefab Cube). Màu được quyết định bằng cách gán
    /// material (từ <see cref="LoopMaster.Data.BlockMaterialSet"/>) cho các renderer mesh.
    /// Khối đã bay lên một phần được vẽ mỏng đi dọc trục Z của khay theo tỷ lệ còn lại (<see cref="SetFill"/>).
    /// </summary>
    public class BigBlockView : MonoBehaviour
    {
        private Vector3 _baseLocalPosition;
        private Vector3 _baseLocalScale;
        private float _length;
        private SpriteRenderer _iconRenderer;
        private Sprite _icon;

        /// <summary>Màu của khối (đặt bởi <see cref="TrayView"/> để biết có lấp tiếp được không).</summary>
        public int ColorId { get; set; }

        /// <summary>Khối bí ẩn đang hiển thị ẩn màu (material xám + dấu "?"); <see cref="TrayView"/> lộ màu khi nó lên trên cùng.</summary>
        public bool IsHidden { get; set; }

        /// <summary>Số khối nhỏ hiện hiển thị (độ dày = Slices/10); đồng bộ với <see cref="SetFill"/>.</summary>
        public int Slices { get; private set; } = LoopMaster.Core.BlockModel.SlicesPerBlock;

        /// <summary>Đặt độ dày theo số khối nhỏ (0..10).</summary>
        public void SetSlices(int slices)
        {
            Slices = Mathf.Clamp(slices, 0, LoopMaster.Core.BlockModel.SlicesPerBlock);
            SetFill(Slices / (float)LoopMaster.Core.BlockModel.SlicesPerBlock);
            RefreshIcon();
        }

        public void SetMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                {
                    r.sharedMaterial = material;
                }
            }
        }

        /// <summary>Ghi nhớ icon của màu; chỉ hiển thị khi khối đã hoàn chỉnh (đủ 10 khối nhỏ), xem <see cref="RefreshIcon"/>.</summary>
        public void SetIcon(Sprite icon)
        {
            _icon = icon;
            RefreshIcon();
        }

        /// <summary>Gán icon lên SpriteRenderer của child "Square" (mặt trên khối) nếu khối đủ 10 khối nhỏ, ngược lại ẩn.</summary>
        private void RefreshIcon()
        {
            if (_iconRenderer == null)
            {
                foreach (SpriteRenderer r in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (r.gameObject.name == "Square")
                    {
                        _iconRenderer = r;
                        break;
                    }
                }
            }

            if (_iconRenderer == null)
            {
                return;
            }

            bool complete = Slices >= LoopMaster.Core.BlockModel.SlicesPerBlock;
            _iconRenderer.sprite = _icon;
            _iconRenderer.enabled = complete && _icon != null;
        }

        /// <summary>Ghi nhớ pose đầy đủ (gọi sau khi đã đặt vị trí trong khay) và chiều dài khối đầy theo Z local của khay.</summary>
        public void Initialize(float length)
        {
            _baseLocalPosition = transform.localPosition;
            _baseLocalScale = transform.localScale;
            _length = length;
        }

        /// <summary>
        /// Vị trí thế giới của lát ngoài cùng (phía +Z) đang còn trong khối — chỗ khối nhỏ tách ra. Gọi trước khi giảm <see cref="Slices"/>.
        /// Khối neo ở −Z (xem <see cref="SetFill"/>) nên lát thứ k (từ 1) có tâm cách mép −Z (k − 0.5)/10 chiều dài.
        /// </summary>
        public Vector3 GetOuterSliceWorldPosition()
        {
            float offsetZ = -_length * 0.5f + (Slices - 0.5f) / LoopMaster.Core.BlockModel.SlicesPerBlock * _length;
            Vector3 local = _baseLocalPosition + Vector3.forward * offsetZ;
            return transform.parent != null ? transform.parent.TransformPoint(local) : local;
        }

        /// <summary>Mỏng khối còn <paramref name="ratio"/> (0..1) dọc Z, neo ở phía −Z (xa mũi tên) để vẫn sát khối bên dưới.</summary>
        public void SetFill(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            Vector3 scale = _baseLocalScale;
            scale.z *= ratio;
            transform.localScale = scale;

            Vector3 position = _baseLocalPosition;
            position.z -= (1f - ratio) * _length * 0.5f;
            transform.localPosition = position;
        }
    }
}
