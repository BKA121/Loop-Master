using DG.Tweening;
using UnityEngine;
using LoopMaster.Data;

namespace LoopMaster.Audio
{
    /// <summary>
    /// Quản lý âm thanh: 1 AudioSource loop cho nhạc nền và một vòng AudioSource cho SFX (tách khối nhỏ cách nhau
    /// ~0.08s nên tiếng chồng lên nhau, mỗi tiếng cần nguồn riêng để không bị cắt). Singleton, sống xuyên scene.
    /// Âm thanh/âm lượng lấy từ <see cref="AudioSet"/>; gọi qua <c>if (AudioManager.Instance != null)</c> để scene không có manager vẫn chạy.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField, Tooltip("Bảng clip + âm lượng")] private AudioSet audioSet;
        [SerializeField, Min(1), Tooltip("Số AudioSource dùng luân phiên cho SFX")] private int sfxSourceCount = 8;
        [SerializeField, Range(0f, 0.3f), Tooltip("Biên độ ngẫu nhiên pitch (± giá trị này) để các tiếng póc liên tiếp đỡ đơn điệu")]
        private float pitchJitter = 0.05f;
        [SerializeField, Min(0f), Tooltip("Số giây tối thiểu giữa 2 lần phát cùng một loại SFX")] private float minInterval = 0.03f;
        [SerializeField, Min(0f)] private float bgmFadeTime = 0.5f;

        private AudioSource _bgmSource;
        private AudioSource[] _sfxSources;
        private int _nextSfx;
        private readonly float[] _lastPlayTime = new float[System.Enum.GetValues(typeof(SfxType)).Length];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            _sfxSources = new AudioSource[sfxSourceCount];
            for (int i = 0; i < _sfxSources.Length; i++)
            {
                _sfxSources[i] = gameObject.AddComponent<AudioSource>();
                _sfxSources[i].playOnAwake = false;
            }

            for (int i = 0; i < _lastPlayTime.Length; i++)
            {
                _lastPlayTime[i] = float.NegativeInfinity;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                DOTween.Kill(_bgmSource);
            }
        }

        /// <summary>Phát nhạc nền của màn chơi; đang phát đúng clip đó thì bỏ qua (tránh restart khi vào lại level).</summary>
        public void PlayGameBgm()
        {
            if (audioSet != null)
            {
                PlayBgm(audioSet.gameBgm, audioSet.bgmVolume);
            }
        }

        public void PlayBgm(AudioClip clip, float volume)
        {
            if (clip == null)
            {
                return;
            }

            DOTween.Kill(_bgmSource);
            if (_bgmSource.isPlaying && _bgmSource.clip == clip)
            {
                _bgmSource.volume = volume;
                return;
            }

            _bgmSource.clip = clip;
            _bgmSource.volume = 0f;
            _bgmSource.Play();
            _bgmSource.DOFade(volume, bgmFadeTime).SetTarget(_bgmSource);
        }

        public void StopBgm()
        {
            DOTween.Kill(_bgmSource);
            if (!_bgmSource.isPlaying)
            {
                return;
            }

            _bgmSource.DOFade(0f, bgmFadeTime).SetTarget(_bgmSource).OnComplete(() => _bgmSource.Stop());
        }

        public void PlaySfx(SfxType type)
        {
            if (audioSet == null)
            {
                return;
            }

            AudioClip clip = audioSet.GetSfx(type);
            if (clip == null)
            {
                return;
            }

            int key = (int)type;
            if (Time.unscaledTime - _lastPlayTime[key] < minInterval)
            {
                return;
            }

            _lastPlayTime[key] = Time.unscaledTime;

            AudioSource source = _sfxSources[_nextSfx];
            _nextSfx = (_nextSfx + 1) % _sfxSources.Length;
            source.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            source.PlayOneShot(clip, audioSet.sfxVolume);
        }
    }
}
