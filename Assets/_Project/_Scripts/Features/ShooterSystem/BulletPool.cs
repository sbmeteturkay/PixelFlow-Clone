using UnityEngine;
using UnityEngine.Pool;
using PrimeTween;
using System;

public class BulletPool : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int defaultCapacity = 30;
    [SerializeField] private int maxSize = 100;

    // ── Singleton ─────────────────────────────────────────────────────

    public static BulletPool Instance { get; private set; }

    // ── Runtime ───────────────────────────────────────────────────────

    private ObjectPool<GameObject> _pool;

    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _pool = new ObjectPool<GameObject>(
            createFunc:      () => Instantiate(bulletPrefab, transform),
            actionOnGet:     go => go.SetActive(true),
            actionOnRelease: go => go.SetActive(false),
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize:         maxSize
        );
    }

    // ═════════════════════════════════════════════════════════════════
    // Public API
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Spawns a bullet from origin toward target.
    /// On arrival: fires hit, calls onHit, returns to pool.
    /// If target dies mid-flight, bullet is cancelled and returned.
    /// </summary>
    public void Fire(Vector3 origin,Vector3 forward, PixelCell target, int colorIndex, float speed, Action onHit = null)
    {
        if (target == null || !target.IsAlive) return;

        target.SetShooted();
        
        GameObject bullet = _pool.Get();
        bullet.transform.position = origin;
        bullet.transform.forward = forward;

        float duration = Vector3.Distance(origin, target.transform.position) / speed;

        Tween.Position(bullet.transform, target.transform.position, duration, Ease.Linear)
            .OnComplete(() =>
            {
                // Target may have been destroyed by another shooter mid-flight
                if (target.IsAlive)
                {
                    PixelGrid.Instance.HandleHit(target, colorIndex);
                    onHit?.Invoke();
                }

                Release(bullet);
            });
    }

    // ─────────────────────────────────────────────────────────────────

    private void Release(GameObject bullet)
    {
        Tween.StopAll(bullet.transform);
        _pool.Release(bullet);
    }
}