using UnityEngine;

namespace LoopMaster.Data
{
    /// <summary>Các âm thanh hiệu ứng (SFX) trong game; thêm giá trị mới khi có âm mới rồi gán clip trong <see cref="AudioSet"/>.</summary>
    public enum SfxType
    {
        SliceSplit,
        SliceToTray,
    }

    /// <summary>
    /// Bảng âm thanh (clip + âm lượng). Tạo asset qua Create → LoopMaster → Audio Set, gán vào AudioManager.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioSet", menuName = "LoopMaster/Audio Set")]
    public class AudioSet : ScriptableObject
    {
        [Header("Nhạc nền")]
        public AudioClip gameBgm;
        [Range(0f, 1f)] public float bgmVolume = 0.5f;

        [Header("SFX")]
        [Tooltip("Tiếng póc khi khối nhỏ tách khỏi khối lớn")] public AudioClip sliceSplit;
        [Tooltip("Tiếng póc khi khối nhỏ vào khay")] public AudioClip sliceToTray;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;

        public AudioClip GetSfx(SfxType type)
        {
            switch (type)
            {
                case SfxType.SliceSplit: return sliceSplit;
                case SfxType.SliceToTray: return sliceToTray;
                default: return null;
            }
        }
    }
}
