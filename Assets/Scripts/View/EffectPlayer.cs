using UnityEngine;
using LoopMaster.Data;

namespace LoopMaster.View
{
    /// <summary>
    /// Dịch vụ phát hiệu ứng particle: view chỉ cần gọi <see cref="Play"/> với <see cref="VfxType"/>, không giữ prefab.
    /// Hiện chỉ Instantiate (particle dùng stopAction = Destroy, số lần phát ít); loại nào phát dày thì thêm pool ở đây.
    /// </summary>
    public class EffectPlayer : MonoBehaviour
    {
        [SerializeField] private VfxSet vfxSet;

        /// <summary>Phát hiệu ứng tại <paramref name="position"/> (cộng vị trí gốc của prefab để giữ độ nâng đã chỉnh). Trả null nếu thiếu prefab.</summary>
        public GameObject Play(VfxType type, Vector3 position)
        {
            if (vfxSet == null)
            {
                return null;
            }

            GameObject prefab = vfxSet.Get(type);
            if (prefab == null)
            {
                return null;
            }

            return Instantiate(prefab, position + prefab.transform.position, Quaternion.identity);
        }
    }
}
