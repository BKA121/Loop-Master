using System.Collections.Generic;
using UnityEngine;
using LoopMaster.Data;

namespace LoopMaster.View
{
    /// <summary>
    /// Dựng decor của level từ dữ liệu <see cref="DecorDTO"/> (trường <c>m</c> trong level JSON) và bảng
    /// <see cref="DecorSet"/> (tên → sprite/prefab).
    ///
    /// KHÔNG tự load level — chỉ nhận dữ liệu qua <see cref="Initialize"/>, do <see cref="GameViewManager"/> gọi,
    /// cùng cách với các view khác.
    /// </summary>
    public class DecorSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("Cha của decor được dựng; để trống = chính object này")]
        private Transform decorRoot;

        [SerializeField, Tooltip("Góc xoay của sprite so với root decor (90,0,0 = nằm phẳng nhìn từ trên xuống, như prefab gốc)")]
        private Vector3 spriteEuler = new Vector3(90f, 0f, 0f);

        [SerializeField, Tooltip("Độ cao cục bộ của sprite so với root decor (prefab gốc dùng 0.1)")]
        private float spriteHeight = 0.1f;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>
        /// Dựng toàn bộ decor của level. Gọi lại sẽ xoá decor cũ rồi dựng lại.
        /// </summary>
        public void Initialize(IReadOnlyList<DecorDTO> decors, DecorSet set)
        {
            Clear();

            if (decors == null || decors.Count == 0)
            {
                return;
            }

            if (set == null)
            {
                Debug.LogWarning("[DecorSpawner] Chưa có DecorSet nên không dựng decor.");
                return;
            }

            Transform parent = decorRoot != null ? decorRoot : transform;
            var missing = new HashSet<string>();

            foreach (DecorDTO decor in decors)
            {
                if (decor == null)
                {
                    continue;
                }

                if (!set.TryGet(decor.n, out DecorSet.Entry entry) || (entry.prefab == null && entry.sprite == null))
                {
                    // Gộp theo tên để không spam log khi 1 decor xuất hiện nhiều lần.
                    if (missing.Add(decor.n ?? "<null>"))
                    {
                        Debug.LogWarning($"[DecorSpawner] DecorSet chưa có prefab/sprite cho decor '{decor.n}'.");
                    }

                    continue;
                }

                GameObject go = CreateDecor(decor.n, entry, parent);
                ApplyMarkerTransform(go.transform, decor);
                _spawned.Add(go);
            }
        }

        private GameObject CreateDecor(string decorName, DecorSet.Entry entry, Transform parent)
        {
            GameObject go;
            if (entry.prefab != null)
            {
                go = Instantiate(entry.prefab, parent);
                go.name = "Decor_" + decorName;
                return go;
            }

            go = new GameObject("Decor_" + decorName);
            go.transform.SetParent(parent, false);

            // Giống prefab gốc: root đặt theo marker, con "Square" mang SpriteRenderer nằm phẳng trên nền.
            var square = new GameObject("Square");
            square.transform.SetParent(go.transform, false);
            square.transform.localPosition = new Vector3(0f, spriteHeight, 0f);
            square.transform.localRotation = Quaternion.Euler(spriteEuler);
            square.AddComponent<SpriteRenderer>().sprite = entry.sprite;

            return go;
        }

        private static void ApplyMarkerTransform(Transform target, DecorDTO decor)
        {
            Vector3 position = ReadVector(decor.p, Vector3.zero);
            Vector3 euler = ReadVector(decor.r, Vector3.zero);
            Vector3 scale = ReadVector(decor.s, Vector3.one);

            // Vị trí/góc xoay trong level JSON là toạ độ thế giới. Scale âm = lật gương, giữ nguyên.
            target.SetPositionAndRotation(position, Quaternion.Euler(euler));
            target.localScale = scale;
        }

        // DTO lưu vector dạng mảng thô float[]; thiếu dữ liệu thì dùng giá trị mặc định.
        private static Vector3 ReadVector(float[] v, Vector3 fallback)
        {
            return v != null && v.Length >= 3 ? new Vector3(v[0], v[1], v[2]) : fallback;
        }

        private void Clear()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }

            _spawned.Clear();
        }
    }
}
