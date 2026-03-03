using UnityEngine;
using UnityEngine.Pool;

namespace Game.Feature.Shooting
{
    public class ShooterPool : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────
        [SerializeField] private Shooter shooterPrefab;
        [SerializeField] private int defaultCapacity = 20;
        [SerializeField] private int maxSize = 50;

        // ── Runtime ───────────────────────────────────────────────────────
        private ObjectPool<Shooter> _pool;

        // ── Singleton ─────────────────────────────────────────────────────
        public static ShooterPool Instance { get; private set; }
        Transform mainCamTransform;

        // ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _pool = new ObjectPool<Shooter>(
                createFunc: CreateShooter,
                actionOnGet: OnGetShooter,
                actionOnRelease: OnReleaseShooter,
                actionOnDestroy: OnDestroyShooter,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
            mainCamTransform = Camera.main.transform;
        }

        // ═════════════════════════════════════════════════════════════════
        // Public API
        // ═════════════════════════════════════════════════════════════════

        /// <summary>Get a shooter from the pool and initialize it with data.</summary>
        public Shooter Get(ShooterData data, Vector3 position)
        {
            Shooter shooter = _pool.Get();
            shooter.transform.position = position;
            shooter.Initialize(data,mainCamTransform);
            return shooter;
        }

        /// <summary>Return a shooter to the pool.</summary>
        private void Release(Shooter shooter)
        {
            _pool.Release(shooter);
        }

        // ═════════════════════════════════════════════════════════════════
        // Pool Callbacks
        // ═════════════════════════════════════════════════════════════════

        private Shooter CreateShooter()
        {
            Shooter s = Instantiate(shooterPrefab, transform);
            s.OnRequestRelease += Release;
            return s;
        }

        private void OnGetShooter(Shooter shooter)
        {
            shooter.gameObject.SetActive(true);
        }

        private void OnReleaseShooter(Shooter shooter)
        {
            shooter.gameObject.SetActive(false);
        }

        private void OnDestroyShooter(Shooter shooter)
        {
            shooter.OnRequestRelease -= Release;
        }
    }
}