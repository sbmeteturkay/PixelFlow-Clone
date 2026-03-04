using System;
using System.Collections.Generic;
using System.Linq;
using PrimeTween;
using UnityEngine;

namespace Game.Feature.Shooting
{
    public class TrayContainer
    {
        public Vector3 position;
        public Quaternion rotation;
        public Tray tray;

        public TrayContainer(Vector3 transformPosition, Quaternion transformRotation, Tray tray)
        {
            position = transformPosition;
            rotation = transformRotation;
            this.tray = tray;
        }

        public bool occupied => tray != null;
    }

    public class ShooterManager : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────

        public const int SplineCapacity = 5;
        public const int SlotCapacity = 5;
        public bool test;

        // ── Inspector ─────────────────────────────────────────────────────

        [Header("Slots")]
        [SerializeField] private Transform[] slotTransforms;

        [SerializeField] private List<Tray> _trays = new();

        [Header("Settings")]
        [SerializeField] private float shooterMovementSpeed = 3f;

        private readonly List<Shooter> _shootersInSlot = new();

        // ── Runtime ───────────────────────────────────────────────────────

        private readonly List<Shooter> _shootersOnSpline = new();
        private readonly List<TrayContainer> _trayContainers = new();
        private Shooter[] _slotOccupants;

        // ── Events ────────────────────────────────────────────────────────

        public Action OnLose;
        public Action<int> OnTrayListCountChanged;

        // ── Singleton ─────────────────────────────────────────────────────

        public static ShooterManager Instance { get; private set; }

        // ── Properties ────────────────────────────────────────────────────

        public float ShooterMovementSpeed => shooterMovementSpeed;
        private bool IsSplineFull => _shootersOnSpline.Count >= SplineCapacity;
        private bool IsSlotFull => Array.TrueForAll(_slotOccupants, s => s != null);

        // ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            foreach (Tray tray in _trays)
            {
                _trayContainers.Add(new(tray.transform.position, tray.transform.rotation, tray));
            }

            _slotOccupants = new Shooter[slotTransforms.Length];
        }


        // ═════════════════════════════════════════════════════════════════
        // Spline
        // ═════════════════════════════════════════════════════════════════

        public bool TryEnterSpline(Shooter shooter)
        {
            if (shooter == null) return false;

            List<TrayContainer> freeTrays = _trayContainers.FindAll(x => x.occupied);
            if (IsSplineFull || freeTrays.Count == 0) return false;

            TrayContainer container = freeTrays.OrderBy(c => c.tray.transform.position.x).Last();
            Tray tray = container.tray;
            container.tray = null;

            Vector3 splineStart = shooter.GetSpline() != null
                ? shooter.GetSpline().EvaluatePosition(0)
                : shooter.transform.position;

            Vector3 trayLocalOffset = tray.transform.position - shooter.transform.position;

            if (tray.sequence.isAlive)
                tray.sequence.Stop();

            tray.sequence = Sequence.Create()
                .Group(Tween.Rotation(tray.transform, tray.transform.eulerAngles, Vector3.right, 0.4f))
                .Group(tray.transform.JumpTo(splineStart, 1, 0.4f))
                .OnComplete(() => trayLocalOffset = tray.transform.position - shooter.transform.position);

            _shootersOnSpline.Add(shooter);

            shooter.OnSplineMove.RemoveAllListeners();
            shooter.OnSplineMove.AddListener(() =>
            {
                tray.transform.position = shooter.transform.position + trayLocalOffset;
            });

            shooter.OnSplineExit.RemoveAllListeners();
            shooter.OnSplineExit.AddListener(() =>
            {
                TrayContainer targetContainer = _trayContainers
                    .FindAll(x => !x.occupied)
                    .OrderBy(x => x.position.x)
                    .First();

                if (tray.sequence.isAlive)
                    tray.sequence.Stop();

                tray.sequence = Sequence.Create()
                    .Group(tray.transform.JumpTo(targetContainer.position, 1.5f, 0.6f))
                    .Group(Tween.Rotation(tray.transform, targetContainer.rotation, 0.6f));

                shooter.OnSplineExit.RemoveAllListeners();
                targetContainer.tray = tray;
                OnTrayListCountChanged?.Invoke(_trayContainers.FindAll(x => x.occupied).Count);
            });

            OnTrayListCountChanged?.Invoke(_trayContainers.FindAll(x => x.occupied).Count);

            if (test) OnLose?.Invoke();

            return true;
        }

        public void ExitSpline(Shooter shooter)
        {
            if (shooter == null) return;
            if (!_shootersOnSpline.Contains(shooter)) return;

            _shootersOnSpline.Remove(shooter);
            OnTrayListCountChanged?.Invoke(_trayContainers.FindAll(x => x.occupied).Count);
        }

        // ═════════════════════════════════════════════════════════════════
        // Slot
        // ═════════════════════════════════════════════════════════════════

        public bool TryEnterSlot(Shooter shooter, out Transform slotTransform)
        {
            slotTransform = null;
            if (shooter == null) return false;

            // Find first free slot
            int freeIndex = -1;
            for (int i = 0; i < _slotOccupants.Length; i++)
            {
                if (_slotOccupants[i] == null)
                {
                    freeIndex = i;
                    break;
                }
            }

            if (freeIndex == -1)
            {
                OnLose?.Invoke();
                return false;
            }

            _slotOccupants[freeIndex] = shooter;
            _shootersInSlot.Add(shooter);
            slotTransform = slotTransforms[freeIndex];
            return true;
        }

        public void ExitSlot(Shooter shooter)
        {
            if (shooter == null) return;
            _shootersInSlot.Remove(shooter);

            for (int i = 0; i < _slotOccupants.Length; i++)
            {
                if (_slotOccupants[i] == shooter)
                {
                    _slotOccupants[i] = null;
                    break;
                }
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // Reset
        // ═════════════════════════════════════════════════════════════════

        public void ClearShooters()
        {
            foreach (Tray tray in _trays)
            {
                if (tray.sequence.isAlive)
                    tray.sequence.Stop();
            }

            for (int i = 0; i < _trayContainers.Count; i++)
            {
                TrayContainer container = _trayContainers[i];
                Tray tray = _trays[i];

                container.tray = tray;
                tray.transform.position = container.position;
                tray.transform.rotation = container.rotation;
            }

            Array.Clear(_slotOccupants, 0, _slotOccupants.Length);
            _shootersInSlot.Clear();
            _shootersOnSpline.Clear();
            OnTrayListCountChanged?.Invoke(_trayContainers.FindAll(x => x.occupied).Count);
        }
    }
}