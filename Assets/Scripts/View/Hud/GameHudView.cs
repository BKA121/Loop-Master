using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LoopMaster.Core;

namespace LoopMaster.View
{
    /// <summary>
    /// HUD đầu màn: ô capacity (số khối lớn trên băng chuyền / tối đa + thanh fill) và ô cấp độ.
    /// Đơn vị chỉ số là khối lớn (<see cref="BlockModel.SlicesPerBlock"/> khối nhỏ): chỉ số chỉ tăng khi đủ 10 khối nhỏ
    /// đã hạ cánh trên băng chuyền, còn thanh fill = Count/Capacity nên tăng mịn theo từng khối nhỏ.
    /// Thanh chuyển đỏ khi chỉ số hiện tại ≥ tối đa − 1. ConveyorModel không có event nên đọc mỗi frame, chỉ cập nhật khi Count đổi.
    /// Nhận model/level qua <see cref="Initialize"/> từ <see cref="GameViewManager"/>.
    /// </summary>
    public class GameHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text capacityText;
        [SerializeField, Tooltip("Image kiểu Filled/Horizontal của thanh capacity")] private Image fillImage;
        [SerializeField, Tooltip("Màu thanh bình thường (sprite fill màu trắng, đổi màu qua Image.color)")]
        private Color normalColor = new Color(0.25f, 0.82f, 0.25f);
        [SerializeField, Tooltip("Màu thanh khi chỉ số hiện tại ≥ tối đa − 1")]
        private Color warningColor = new Color(0.9f, 0.19f, 0.19f);
        [SerializeField] private TMP_Text levelText;

        private ConveyorModel _model;
        private int _lastCount = -1;

        public void Initialize(ConveyorModel model, int level)
        {
            _model = model;
            _lastCount = -1;

            if (levelText != null)
            {
                levelText.text = "CẤP ĐỘ\n" + level;
            }

            Refresh();
        }

        private void Update()
        {
            if (_model != null && _model.Count != _lastCount)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            _lastCount = _model.Count;

            int perBlock = BlockModel.SlicesPerBlock;
            int current = _lastCount / perBlock;
            int max = _model.Capacity / perBlock;

            if (capacityText != null)
            {
                capacityText.text = current + "/" + max;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = _model.Capacity > 0 ? Mathf.Clamp01((float)_lastCount / _model.Capacity) : 0f;

                fillImage.color = current >= max - 1 ? warningColor : normalColor;
            }
        }
    }
}
