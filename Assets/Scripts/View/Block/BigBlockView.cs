using UnityEngine;

namespace LoopMaster.View
{
    /// <summary>
    /// Khối lớn hiển thị trong khay (gắn lên instance prefab Cube). Màu được quyết định bằng cách gán
    /// material (từ <see cref="LoopMaster.Data.BlockMaterialSet"/>) cho các renderer mesh.
    /// </summary>
    public class BigBlockView : MonoBehaviour
    {
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
    }
}
