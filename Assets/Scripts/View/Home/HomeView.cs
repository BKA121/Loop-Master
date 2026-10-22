using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using LoopMaster.Data;

namespace LoopMaster.View
{
    /// <summary>
    /// Màn hình chính: một nút hiển thị level hiện tại ("Level N"); bấm để vào chơi level đó.
    /// Level hiện tại lấy từ <see cref="ProgressStore"/> (thắng thì tăng sang level kế).
    ///
    /// Nút được nối qua Inspector (Button → On Click → HomeView.OnPlayClicked) chứ không tham chiếu kiểu Button trong code,
    /// vì thư mục ASSET RIPPER/Scripts/UnityEngine.UI có class Button rỗng trùng tên che mất UnityEngine.UI.Button thật.
    /// </summary>
    public class HomeView : MonoBehaviour
    {
        [SerializeField] private TMP_Text levelLabel;

        private void Start()
        {
            if (levelLabel != null)
            {
                levelLabel.text = "Level " + ProgressStore.CurrentLevel;
            }
        }

        /// <summary>Gán vào On Click của nút Play trong Inspector.</summary>
        public void OnPlayClicked()
        {
            SceneManager.LoadScene(SceneNames.Game);
        }
    }
}
