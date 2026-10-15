using DG.Tweening;
using UnityEngine;

namespace LoopMaster.View
{
    /// <summary>
    /// Khối nhỏ (slice) hiển thị trên băng chuyền. Được tái sử dụng qua <see cref="SlicePool"/>,
    /// nên mọi trạng thái phải được đưa về mặc định trong <see cref="ResetState"/> khi lấy ra khỏi pool.
    /// </summary>
    public class SliceView : MonoBehaviour
    {
        private Renderer[] _renderers;
        private Vector3 _baseScale;
        private bool _cached;

        private void Cache()
        {
            if (_cached)
            {
                return;
            }

            var meshRenderers = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                {
                    meshRenderers.Add(r);
                }
            }

            _renderers = meshRenderers.ToArray();
            _baseScale = transform.localScale;
            _cached = true;
        }

        /// <summary>Đặt màu bằng cách gán material (từ BlockMaterialSet) cho renderer mesh.</summary>
        public void SetMaterial(Material material)
        {
            Cache();
            if (material == null)
            {
                return;
            }

            foreach (Renderer r in _renderers)
            {
                r.sharedMaterial = material;
            }
        }

        /// <summary>Gọi khi lấy ra khỏi pool: dọn tween còn sót, trả scale về mặc định.</summary>
        public void ResetState()
        {
            Cache();
            DOTween.Kill(transform);
            transform.localScale = _baseScale;
        }
    }
}
