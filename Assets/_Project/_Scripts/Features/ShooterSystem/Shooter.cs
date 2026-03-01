using UnityEngine;
using UnityEngine.Splines;
using TMPro;
using PrimeTween;
using System;
using Game.Feature.Level;

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

        private State _state;
        public State CurrentState => _state;

        // ── Inspector ─────────────────────────────────────────────────────

        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private Vector3 _shootOffset;


        [Header("UI")]
        [SerializeField] private TextMeshPro pixelCountLabel;

        // ── Runtime ───────────────────────────────────────────────────────

        private SplineContainer splineContainer;
        private int ColorIndex { get; set; }

        private bool _interactable;

        private int _remainingPixels;
        private Transform _slotTargetTransform;

        private Sequence _currentSequence;

        private int _prevLineIndex = -1;
        private GridEdge _prevEdge;

        public Action<Shooter> OnRequestRelease;

        private float splineMovementSpeed;
        private readonly float moveToSlotSpeed = 16f;

        private MaterialPropertyBlock _mpb;
        private Color _baseColor;
        private Vector3 defaultScale;

        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

        // ═════════════════════════════════════════════════════════════════
        // Lifecycle
        // ═════════════════════════════════════════════════════════════════

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            defaultScale = transform.localScale;
        }

        private void OnDisable()
        {
            StopAllProcesses();
        }

        // ═════════════════════════════════════════════════════════════════
        // Initialization (Pool)
        // ═════════════════════════════════════════════════════════════════

        public void Initialize(ShooterData data)
        {
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
            _meshRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorID, color);
            _meshRenderer.SetPropertyBlock(_mpb);
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
        // Input
        // ═════════════════════════════════════════════════════════════════

        private void OnMouseDown()
        {
            if (!_interactable)
                return;
            Tween.PunchScale(transform, Vector3.one * .5f, .1f);
            if (_state == State.Waiting || _state == State.Slotted)
                TryLaunchToSpline();
        }

        // ═════════════════════════════════════════════════════════════════
        // State Management
        // ═════════════════════════════════════════════════════════════════

        private void SetState(State newState)
        {
            if (_state == newState) return;

            _state = newState;
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

            if (_state == State.Slotted)
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
                (Vector3)splineContainer.EvaluatePosition(0));
            _prevLineIndex = GetCurrentLineIndex(_prevEdge);
            
            _currentSequence = Sequence.Create()
                .Group(transform.JumpTo(splineContainer.EvaluatePosition(0), 2, .4f))
                .Group(Tween.Rotation(transform, (Vector3)splineContainer.EvaluateTangent(0) + Vector3.up * 90, .4f,
                        ease: Ease.InBack).OnComplete(()=>Debug.Log("finish"))
                    .Chain(Tween.Custom(
                        0f,
                        splineLength,
                        duration,
                        onValueChange: distance =>
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

                            SweepAndFire();
                        },
                        ease: Ease.Linear))
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
                TryFireAtLine(currentEdge, line);

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
                    transform.forward, target, ColorIndex, 10);
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
                    .OnComplete(() => { transform.position = _slotTargetTransform.position; })
                    .Group(Tween.Rotation(transform, _slotTargetTransform.rotation.eulerAngles, duration)));
        }

        // ═════════════════════════════════════════════════════════════════
        // Pool
        // ═════════════════════════════════════════════════════════════════

        public void ReturnToPool()
        {
            StopAllProcesses();

            if (_state == State.OnSpline)
                ShooterManager.Instance.ExitSpline(this);
            else if (_state == State.Slotted)
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
                                ease: Ease.InBack)))
                .Group(
                    Tween.Scale(transform, Vector3.zero, .5f, ease: Ease.InBack).OnComplete(() =>
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