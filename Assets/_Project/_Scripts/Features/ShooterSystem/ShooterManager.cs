using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PrimeTween;

namespace Game.Feature.Shooting
{
    public class ShooterManager : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────

        public const int SplineCapacity = 5;
        public const int SlotCapacity = 5;
        public bool test = false;

        // ── Inspector ─────────────────────────────────────────────────────

        [Header("Slots")]
        [SerializeField] private Transform[] slotTransforms;

        [SerializeField] private Transform[] trayParentTransforms;

        [Header("Settings")]
        [SerializeField] private float shooterMovementSpeed = 3f;

        // ── Singleton ─────────────────────────────────────────────────────

        public static ShooterManager Instance { get; private set; }

        // ── Properties ────────────────────────────────────────────────────

        public float ShooterMovementSpeed => shooterMovementSpeed;
        public bool IsSplineFull => _shootersOnSpline.Count >= SplineCapacity;
        public bool IsSlotFull => _shootersInSlot.Count >= SlotCapacity;
        public int SplineCount => _shootersOnSpline.Count;
        public int SlotCount => _shootersInSlot.Count;

        // ── Events ────────────────────────────────────────────────────────

        public System.Action OnLose;
        public System.Action<int> OnTrayListCountChanged;

        // ── Runtime ───────────────────────────────────────────────────────

        private readonly List<Shooter> _shootersOnSpline = new();
        private readonly List<Shooter> _shootersInSlot = new();
        private readonly Dictionary<Shooter, Transform> _trayOnShooters = new();

        private readonly List<Transform> _trayList = new();
        private readonly List<Transform> _emptyTrayContainerList = new();

        // ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (trayParentTransforms != null)
                foreach (Transform t in trayParentTransforms)
                    if (t != null)
                        _trayList.Add(t);
        }

        // ═════════════════════════════════════════════════════════════════
        // Spline
        // ═════════════════════════════════════════════════════════════════

        public bool TryEnterSpline(Shooter shooter)
        {
            if (shooter == null) return false;
            if (IsSplineFull || _trayList.Count == 0) return false;

            Transform trayCarrier = _trayList.OrderBy(c => c.position.x).Last();
            _trayList.Remove(trayCarrier);
            _emptyTrayContainerList.Add(trayCarrier);

            if (trayCarrier.childCount == 0)
            {
                Debug.LogWarning("[ShooterManager] TryEnterSpline: trayCarrier has no children.");
                _trayList.Add(trayCarrier);
                _emptyTrayContainerList.Remove(trayCarrier);
                return false;
            }

            Transform tray = trayCarrier.GetChild(0);
            tray.parent = null;

            Vector3 splineStart = shooter.GetSpline() != null
                ? (Vector3)shooter.GetSpline().EvaluatePosition(0)
                : shooter.transform.position;

            Sequence.Create()
                .Group(Tween.EulerAngles(tray, tray.eulerAngles, Vector3.zero, 0.4001f))
                .Group(tray.JumpTo(splineStart, 1, 0.4001f))
                .OnComplete(() => tray.parent = shooter.transform);

            _trayOnShooters.Add(shooter, tray);
            _shootersOnSpline.Add(shooter);

            OnTrayListCountChanged?.Invoke(SplineCapacity - _trayOnShooters.Count);
            if (test)
            {
                OnLose?.Invoke();
            }
            return true;
        }

        public void ExitSpline(Shooter shooter)
        {
            if (shooter == null) return;
            if (!_shootersOnSpline.Contains(shooter)) return;

            if (_emptyTrayContainerList.Count == 0)
            {
                Debug.LogWarning("[ShooterManager] ExitSpline: no empty tray containers available.");
                _shootersOnSpline.Remove(shooter);
                _trayOnShooters.Remove(shooter);
                return;
            }

            if (!_trayOnShooters.TryGetValue(shooter, out Transform tray))
            {
                Debug.LogWarning("[ShooterManager] ExitSpline: no tray found for shooter.");
                _shootersOnSpline.Remove(shooter);
                return;
            }

            Transform emptySlot = _emptyTrayContainerList.OrderBy(c => c.position.x).First();
            _emptyTrayContainerList.Remove(emptySlot);

            tray.parent = null;

            Sequence.Create()
                .Group(tray.transform.JumpTo(emptySlot.position, 1, 0.5f))
                .Group(Tween.Rotation(tray, emptySlot.rotation, 0.5f))
                .OnComplete(() =>
                {
                    tray.parent = emptySlot;
                    _trayList.Add(emptySlot);
                });

            _trayOnShooters.Remove(shooter);
            _shootersOnSpline.Remove(shooter);

            OnTrayListCountChanged?.Invoke(SplineCapacity - _trayOnShooters.Count);
        }

        // ═════════════════════════════════════════════════════════════════
        // Slot
        // ═════════════════════════════════════════════════════════════════

        public bool TryEnterSlot(Shooter shooter, out Transform slotTransform)
        {
            slotTransform = null;

            if (shooter == null) return false;

            if (IsSlotFull)
            {
                OnLose?.Invoke();
                return false;
            }

            _shootersInSlot.Add(shooter);
            slotTransform = GetNextSlotTransform();

            if (slotTransform == null)
            {
                Debug.LogWarning("[ShooterManager] TryEnterSlot: no valid slot transform found.");
                _shootersInSlot.Remove(shooter);
                return false;
            }

            return true;
        }

        public void ExitSlot(Shooter shooter)
        {
            if (shooter == null) return;
            _shootersInSlot.Remove(shooter);
        }

        private Transform GetNextSlotTransform()
        {
            int index = _shootersInSlot.Count - 1;

            if (index >= 0 && index < slotTransforms.Length)
                return slotTransforms[index];

            return null;
        }

        // ═════════════════════════════════════════════════════════════════
        // Reset
        // ═════════════════════════════════════════════════════════════════

        public void ClearShooters()
        {
            _shootersInSlot.Clear();
            _shootersOnSpline.Clear();
            _trayOnShooters.Clear();
        }
    }
}