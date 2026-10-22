using UnityEngine;

namespace LoopMaster.Data
{
    /// <summary>
    /// Tiến trình người chơi: level hiện tại (bắt đầu từ 1), lưu bằng PlayerPrefs.
    /// Thắng một level thì sang level kế; qua hết level cuối thì giữ ở level cuối.
    /// </summary>
    public static class ProgressStore
    {
        private const string CurrentLevelKey = "CurrentLevel";

#if UNITY_EDITOR
        // Chỉ để test: mỗi lần bấm Play trong Editor đều bắt đầu lại từ level 1 (vẫn tăng level bình thường trong cùng một lần Play).
        // Đặt false khi muốn giữ tiến trình giữa các lần Play.
        private const bool ResetOnEditorPlay = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetForEditorTest()
        {
            if (ResetOnEditorPlay)
            {
                PlayerPrefs.DeleteKey(CurrentLevelKey);
            }
        }
#endif

        public static int CurrentLevel
        {
            get
            {
                int level = PlayerPrefs.GetInt(CurrentLevelKey, 1);
                return Mathf.Max(1, level);
            }
        }

        /// <summary>Gọi khi thắng level hiện tại: tăng sang level kế, không vượt quá số level hiện có.</summary>
        public static void CompleteCurrentLevel()
        {
            int next = Mathf.Min(CurrentLevel + 1, Mathf.Max(1, LevelLoader.CountLevels()));
            PlayerPrefs.SetInt(CurrentLevelKey, next);
            PlayerPrefs.Save();
        }
    }
}
