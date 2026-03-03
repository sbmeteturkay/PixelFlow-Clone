using System;
using Game.Feature.Level;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Splines;

namespace Game.Feature.Shooting
{
    public class Shooter : MonoBehaviour
    {
        // ── State ─────────────────────────────────────────────────────────

        public enum State
        {
            Waiting,
            OnSpline,
            Slotted
        }

        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

        // ── Inspector ─────────────────────────────────────────────────────

        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private Vector3 _shootOffset;


        [Header("UI")]
        [SerializeField] private TextMeshPro pixelCountLabel;

        private readonly float moveToSlotSpeed = 16f;
        private Color _baseColor;

        private Sequence _currentSequence;

        private bool _interactable;

        private MaterialPropertyBlock _mpb;
        private GridEdge _prevEdge;

        private int _prevLineIndex = -1;

        private int _remainingPixels;
        private Transform _slotTargetTransform;

        private Vector3 defaultScale;

        private Transform mainCamTransform;

        public Action<Shooter> OnRequestRelease;

        // ── Runtime ───────────────────────────────────────────────────────

        private SplineContainer splineContainer;

        private float splineMovementSpeed;
        public State CurrentState { get; private set; }

        private int ColorIndex { get; set; }

        // ═════════════════════════════════════════════════════════════════
        // Lifecycle
        // ═════════════════════════════════════════════════════════════════

        private void Awake()
        {
            _mpb = new();
            defaultScale = transform.localScale;
        }

        private void OnDisable()
        {
            StopAllProcesses();
        }
        // ═════════════════════════════════════════════════════════════════
        // Input
        // ═════════════════════════════════════════════════════════════════

        private void OnMouseDown()
        {
            if (!_interactable)
                return;
            Tween.PunchScale(transform, Vector3.one * .5f, .1f);
            if (CurrentState == State.Waiting || CurrentState == State.Slotted)
                TryLaunchToSpline();
        }

        // ═════════════════════════════════════════════════════════════════
        // Initialization (Pool)
        // ═════════════════════════════════════════════════════════════════

        public void Initialize(ShooterData data, Transform mainCam)
        {
            mainCamTransform = mainCam;
            transform.localScale = defaultScale;
            ColorIndex = data.colorIndex;
            _remainingPixels = data.pixelCount;
            transform.rotation = Quaternion.identity;
            splineMovementSpeed = ShooterManager.Instance.ShooterMovementSpeed;

            _baseColor = data.targetColor;
            ApplyColor(_baseColor);

            UpdateLabel();
            SetState(State.Waiting);
        }

        private void ApplyColor(Color color)
        {
            _meshRenderer.material.SetColor(BaseColorID, color);
        }

        public void SetSpline(SplineContainer spline)
        {
            splineContainer = spline;
        }

        public SplineContainer GetSpline()
        {
            return splineContainer;
        }

        // ═════════════════════════════════════════════════════════════════
        // State Management
        // ═════════════════════════════════════════════════════════════════

        private void SetState(State newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            OnStateChanged(newState);
        }

        private void OnStateChanged(State newState)
        {
            StopAllProcesses();

            switch (newState)
            {
                case State.OnSpline:
                    EnterSpline();
                    break;

                case State.Slotted:
                    EnterSlotMove();
                    break;
            }
        }

        private void StopAllProcesses()
        {
            _currentSequence.Stop();
        }

        // ═════════════════════════════════════════════════════════════════
        // Launch Logic
        // ═════════════════════════════════════════════════════════════════

        private void TryLaunchToSpline()
        {
            if (!ShooterManager.Instance.TryEnterSpline(this))
                return;

            if (CurrentState == State.Slotted)
                ShooterManager.Instance.ExitSlot(this);

            LevelManager.Instance.OnShooterLeftWaiting(this);
            SetState(State.OnSpline);
        }

        private void FinishSpline()
        {
            ShooterManager.Instance.ExitSpline(this);

            if (!ShooterManager.Instance.TryEnterSlot(this, out Transform slotPos))
            {
                SetState(State.Waiting);
                return;
            }

            _slotTargetTransform = slotPos;
            SetState(State.Slotted);
        }

        // ═════════════════════════════════════════════════════════════════
        // Spline Movement + Line Sweep
        // ═════════════════════════════════════════════════════════════════

        private void EnterSpline()
        {
            if (splineContainer == null) return;

            float splineLength = splineContainer.CalculateLength();
            float duration = splineLength / splineMovementSpeed;

            _prevEdge = PixelGrid.Instance.GetEdgeForPosition(
                splineContainer.EvaluatePosition(0));
            _prevLineIndex = GetCurrentLineIndex(_prevEdge);

            _currentSequence = Sequence.Create()
                .Group(transform.JumpTo(splineContainer.EvaluatePosition(0), 2, .4f))
                .Group(Tween.Rotation(transform, (Vector3)splineContainer.EvaluateTangent(0) + Vector3.up * 90, .4f,
                        Ease.InBack).OnUpdate(this, (_, _) => pixelCountLabel.transform.LookAt(
                        transform.position + mainCamTransform.forward,
                        mainCamTransform.up))
                    .Chain(Tween.Custom(
                        0f,
                        splineLength,
                        duration,
                        distance =>
                        {
                            float normalized = distance / splineLength;

                            Vector3 pos = splineContainer.EvaluatePosition(normalized);
                            transform.position = pos;

                            Vector3 tangent = splineContainer.EvaluateTangent(normalized);

                            if (tangent != Vector3.zero)
                            {
                                transform.rotation = Quaternion.LookRotation(tangent, Vector3.up) *
                                                     Quaternion.Euler(0f, 90f, 0f);
                            }

                            pixelCountLabel.transform.LookAt(transform.position + mainCamTransform.forward,
                                mainCamTransform.up);
                            SweepAndFire();
                        },
                        Ease.Linear))
                    .OnComplete(FinishSpline));
        }

        // ═════════════════════════════════════════════════════════════════
        // Line Sweep
        // ═════════════════════════════════════════════════════════════════

        private void SweepAndFire()
        {
            if (PixelGrid.Instance == null) return;

            GridEdge currentEdge = PixelGrid.Instance.GetEdgeForPosition(transform.position);

            if (currentEdge == GridEdge.Corner)
            {
                _prevLineIndex = -1;
                _prevEdge = GridEdge.Corner;
                return;
            }

            if (currentEdge != _prevEdge)
            {
                _prevEdge = currentEdge;
                _prevLineIndex = GetCurrentLineIndex(currentEdge);
                TryFireAtLine(currentEdge, _prevLineIndex);
                return;
            }

            int currentLineIndex = GetCurrentLineIndex(currentEdge);
            if (currentLineIndex == _prevLineIndex) return;

            int step = currentLineIndex > _prevLineIndex ? 1 : -1;
            for (int line = _prevLineIndex + step; line != currentLineIndex + step; line += step)
            {
                TryFireAtLine(currentEdge, line);
            }

            _prevLineIndex = currentLineIndex;
        }

        private void TryFireAtLine(GridEdge edge, int lineIndex)
        {
            PixelCell target = PixelGrid.Instance.GetFrontCell(edge, lineIndex);
            if (target == null || !target.IsAlive || target.IsShooted) return;
            if (target.ColorIndex == ColorIndex)
            {
                RegisterHit();
                Tween.PunchScale(_meshRenderer.gameObject.transform, Vector3.one * .5f, .1f);
                BulletPool.Instance.Fire(transform.position + transform.TransformDirection(_shootOffset),
                    transform.forward, target, ColorIndex, 15);
            }
        }

        private void RegisterHit()
        {
            _remainingPixels--;
            UpdateLabel();

            if (_remainingPixels <= 0)
                ReturnToPool();
        }

        // ═════════════════════════════════════════════════════════════════
        // Slot Movement
        // ═════════════════════════════════════════════════════════════════

        private void EnterSlotMove()
        {
            float duration =
                Vector3.Distance(transform.position, _slotTargetTransform.position) / moveToSlotSpeed;

            Sequence.Create()
                .Group(transform.JumpTo(_slotTargetTransform.position, 2, duration)
                    .OnComplete(() =>
                    {
                        transform.position = _slotTargetTransform.position;
                        pixelCountLabel.transform.LookAt(transform.position + mainCamTransform.forward,
                            mainCamTransform.up);
                    })
                    .Group(Tween.Rotation(transform, _slotTargetTransform.rotation.eulerAngles, duration)));
        }

        // ═════════════════════════════════════════════════════════════════
        // Pool
        // ═════════════════════════════════════════════════════════════════

        public void ReturnToPool()
        {
            StopAllProcesses();

            if (CurrentState == State.OnSpline)
                ShooterManager.Instance.ExitSpline(this);
            else if (CurrentState == State.Slotted)
                ShooterManager.Instance.ExitSlot(this);
            Sequence.Create()
                .Group(
                    Tween.Rotation(
                            transform,
                            transform.eulerAngles + new Vector3(0f, 90f, 0f),
                            0.1f,
                            Ease.Linear, cycleMode: CycleMode.Incremental, cycles: 8)
                        .Group(
                            Tween.Position(transform, transform.position + Vector3.forward * 2, .5f,
                                Ease.InBack)))
                .Group(
                    Tween.Scale(transform, Vector3.zero, .5f, Ease.InBack).OnComplete(() =>
                    {
                        OnRequestRelease?.Invoke(this);
                    })
                );
        }

        public void ResetShooter()
        {
            StopAllProcesses();
            SetState(State.Waiting);
            if (gameObject.activeInHierarchy)
                OnRequestRelease?.Invoke(this);
        }

        // ═════════════════════════════════════════════════════════════════
        // Edge / Line Detection
        // ═════════════════════════════════════════════════════════════════

        private GridEdge GetCurrentEdge()
        {
            return PixelGrid.Instance.GetEdgeForPosition(transform.position);
        }

        private int GetCurrentLineIndex(GridEdge edge)
        {
            return edge switch
            {
                GridEdge.Top or GridEdge.Bottom =>
                    PixelGrid.Instance.WorldXToCol(transform.position.x),

                GridEdge.Left or GridEdge.Right =>
                    PixelGrid.Instance.WorldZToRow(transform.position.z),

                _ => 0
            };
        }

        // ═════════════════════════════════════════════════════════════════
        // UI
        // ═════════════════════════════════════════════════════════════════

        private void UpdateLabel()
        {
            if (pixelCountLabel != null)
                pixelCountLabel.text = _remainingPixels.ToString();
            pixelCountLabel.transform.LookAt(transform.position + mainCamTransform.forward,
                mainCamTransform.up);
        }

        public void MoveTo(Vector3 getWaitingPosition)
        {
            transform.JumpTo(getWaitingPosition, 1, .4f);
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            pixelCountLabel.alpha = interactable ? 1 : .6f;
        }
    }
}