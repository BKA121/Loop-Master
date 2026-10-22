using UnityEngine;

namespace LoopMaster.Data
{
    /// <summary>
    /// Chịu trách nhiệm đọc file dữ liệu level (JSON) và trả về <see cref="LevelDataDTO"/>.
    ///
    /// Level nằm ở "Assets/Resources/Levels/Level{n}.json" (n bắt đầu từ 1) vì Resources.Load chỉ đọc được
    /// asset nằm trong một thư mục tên "Resources", và không nên phụ thuộc runtime vào thư mục tham khảo ASSET RIPPER.
    /// </summary>
    public static class LevelLoader
    {
        // Thư mục tính từ bên trong Resources; tên file là LevelPrefix + số level, không có phần mở rộng ".json".
        private const string LevelFolder = "Levels";
        private const string LevelPrefix = "Level";

        /// <summary>
        /// Load level số <paramref name="levelNumber"/> (từ 1). Trả về null và log lỗi nếu không đọc/parse được.
        /// </summary>
        public static LevelDataDTO LoadLevel(int levelNumber)
        {
            return LoadFromResources($"{LevelFolder}/{LevelPrefix}{levelNumber}");
        }

        /// <summary>Số file level hiện có trong Resources/Levels.</summary>
        public static int CountLevels()
        {
            return Resources.LoadAll<TextAsset>(LevelFolder).Length;
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
