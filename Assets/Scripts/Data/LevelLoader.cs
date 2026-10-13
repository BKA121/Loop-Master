using UnityEngine;

namespace LoopMaster.Data
{
    /// <summary>
    /// Chịu trách nhiệm đọc file dữ liệu level (JSON) và trả về <see cref="LevelDataDTO"/>.
    ///
    /// Hiện tại HARDCODE cứng level 1 (file gốc "p537803.json") để dựng phần core trước;
    /// chưa có hệ thống chọn level theo id — sẽ thay thế khi có nhiều level.
    /// File JSON được copy từ "Assets/ASSET RIPPER/copiedassets/decoded_json/p537803.json" sang
    /// "Assets/Resources/Levels/p537803.json" vì Resources.Load chỉ đọc được asset nằm trong
    /// một thư mục tên "Resources", và không nên phụ thuộc runtime vào thư mục tham khảo ASSET RIPPER.
    /// </summary>
    public static class LevelLoader
    {
        // Đường dẫn tính từ bên trong thư mục Resources, không có phần mở rộng ".json".
        private const string Level1ResourcePath = "Levels/p537804";

        /// <summary>
        /// Load level 1 (hardcode). Trả về null và log lỗi nếu không đọc/parse được.
        /// </summary>
        public static LevelDataDTO LoadLevel1()
        {
            return LoadFromResources(Level1ResourcePath);
        }

        private static LevelDataDTO LoadFromResources(string resourcePath)
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);
            if (jsonAsset == null)
            {
                Debug.LogError($"[LevelLoader] Không tìm thấy file level tại Resources/{resourcePath}.json");
                return null;
            }

            return ParseJson(jsonAsset.text);
        }

        private static LevelDataDTO ParseJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<LevelDataDTO>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LevelLoader] Parse JSON thất bại: {ex.Message}");
                return null;
            }
        }
    }
}
