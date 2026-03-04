using UnityEngine;

namespace Game.Core
{
    public class AudioManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────

        [Header("Music")]
        [SerializeField] private AudioClip gameplayMusic;

        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;

        [Header("SFX")]
        [SerializeField] private AudioClip shootSfx;

        [SerializeField] private AudioClip cellDestroyedSfx;
        [SerializeField] private AudioClip shooterLaunchSfx;
        [SerializeField] private AudioClip winSfx;
        [SerializeField] private AudioClip loseSfx;
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1f;

        // ── Runtime ───────────────────────────────────────────────────────

        private AudioSource _musicSource;

        private AudioSource _sfxSource;
        // ── Singleton ─────────────────────────────────────────────────────

        public static AudioManager Instance { get; private set; }

        // ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (gameplayMusic != null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
                _musicSource.loop = true;
                _musicSource.playOnAwake = false;
                _musicSource.volume = musicVolume;
            }

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            _sfxSource.volume = sfxVolume;
        }

        private void Start()
        {
            PlayMusic();
        }

        // ═════════════════════════════════════════════════════════════════
        // Music
        // ═════════════════════════════════════════════════════════════════

        public void PlayMusic()
        {
            if (gameplayMusic == null) return;
            _musicSource.clip = gameplayMusic;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            _musicSource.Stop();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            _musicSource.volume = musicVolume;
        }

        // ═════════════════════════════════════════════════════════════════
        // SFX
        // ═════════════════════════════════════════════════════════════════

        public void PlayShoot()
        {
            Play(shootSfx);
        }

        public void PlayCellDestroyed()
        {
            Play(cellDestroyedSfx);
        }

        public void PlayShooterLaunch()
        {
            Play(shooterLaunchSfx);
        }

        public void PlayWin()
        {
            Play(winSfx);
        }

        public void PlayLose()
        {
            Play(loseSfx);
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            _sfxSource.volume = sfxVolume;
        }

        // ── Internal ──────────────────────────────────────────────────────

        private void Play(AudioClip clip)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
}