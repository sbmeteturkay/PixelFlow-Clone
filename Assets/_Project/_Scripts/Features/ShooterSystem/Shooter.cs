using UnityEngine;
using UnityEngine.Splines;
using TMPro;
using UniRx;
using PrimeTween;
using System;

public class Shooter : MonoBehaviour
{
    // ── State ─────────────────────────────────────────────────────────

    public enum State { Waiting, OnSpline, Slotted }

    private readonly ReactiveProperty<State> _state = new(State.Waiting);
    public IReadOnlyReactiveProperty<State> StateStream => _state;

    // ── Inspector ─────────────────────────────────────────────────────

    [SerializeField]private MeshRenderer _meshRenderer;
    
    [Header("Spline")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("UI")]
    [SerializeField] private TextMeshPro pixelCountLabel;

    // ── Runtime ───────────────────────────────────────────────────────

    public int ColorIndex { get; private set; }

    private int _remainingPixels;
    private Vector3 _slotTargetPosition;

    private Sequence _currentSequence;
    private CompositeDisposable _disposables = new();

    // Line sweep
    private int _prevLineIndex = -1;
    private GridEdge _prevEdge;

    // ── Events ────────────────────────────────────────────────────────

    public Action<Shooter> OnRequestRelease;

    private float splineMovementSpeed;
    private readonly float moveToSlotSpeed = 8f;
    // ── Private ───────────────────────────────────────────────────────
    private MaterialPropertyBlock _mpb;
    private Color _baseColor;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    // ═════════════════════════════════════════════════════════════════
    // Lifecycle
    // ═════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _state
            .DistinctUntilChanged()
            .Subscribe(OnStateChanged)
            .AddTo(_disposables);
        _mpb = new MaterialPropertyBlock();
    }

    private void OnDisable()
    {
        StopAllProcesses();
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }

    // ═════════════════════════════════════════════════════════════════
    // Initialization (Pool)
    // ═════════════════════════════════════════════════════════════════

    public void Initialize(ShooterData data)
    {
        ColorIndex = data.colorIndex;
        _remainingPixels = data.pixelCount;
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

    // ═════════════════════════════════════════════════════════════════
    // Input
    // ═════════════════════════════════════════════════════════════════

    private void OnMouseDown()
    {
        if (_state.Value == State.Waiting || _state.Value == State.Slotted)
            TryLaunchToSpline();
    }

    // ═════════════════════════════════════════════════════════════════
    // State Management
    // ═════════════════════════════════════════════════════════════════

    private void SetState(State newState) => _state.Value = newState;

    private void OnStateChanged(State newState)
    {
        StopAllProcesses();

        switch (newState)
        {
            case State.OnSpline: EnterSpline(); break;
            case State.Slotted:  EnterSlotMove(); break;
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
        if (!ShooterManager.Instance.TryEnterSpline(this)) return;

        if (_state.Value == State.Slotted)
            ShooterManager.Instance.ExitSlot(this);

        SetState(State.OnSpline);
    }

    private void FinishSpline()
    {
        ShooterManager.Instance.ExitSpline(this);

        if (!ShooterManager.Instance.TryEnterSlot(this, out Vector3 slotPos))
        {
            SetState(State.Waiting);
            return;
        }

        _slotTargetPosition = slotPos;
        SetState(State.Slotted);
    }

    // ═════════════════════════════════════════════════════════════════
    // Spline Movement (PrimeTween) + Line Sweep Firing
    // ═════════════════════════════════════════════════════════════════

    private void EnterSpline()
    {
        if (splineContainer == null) return;

        float splineLength = splineContainer.CalculateLength();
        float duration = splineLength / splineMovementSpeed;

        // Reset sweep state
        _prevLineIndex = -1;

        _currentSequence = Sequence.Create()
            .Group(transform.JumpTo(splineContainer.EvaluatePosition(0), 1, .5f))
            .Chain(Tween.Custom(
                0f,
                splineLength,
                duration,
                onValueChange: distance =>
                {
                    float normalized = distance / splineLength;

                    // Movement
                    Vector3 pos = splineContainer.EvaluatePosition(normalized);
                    transform.position = pos;

                    Vector3 tangent = splineContainer.EvaluateTangent(normalized);
                    if (tangent != Vector3.zero)
                        transform.forward = tangent.normalized;

                    // Line sweep firing
                    SweepAndFire();
                },
                ease: Ease.Linear))
            .OnComplete(FinishSpline);
    }

    // ═════════════════════════════════════════════════════════════════
    // Line Sweep
    // ═════════════════════════════════════════════════════════════════

    private void SweepAndFire()
    {
        if (PixelGrid.Instance == null) return;

        GridEdge currentEdge = GetCurrentEdge();
        int currentLineIndex = GetCurrentLineIndex(currentEdge);

        // Corner transition: edge changed, reset sweep
        if (currentEdge != _prevEdge)
        {
            _prevEdge = currentEdge;
            _prevLineIndex = currentLineIndex;
            TryFireAtLine(currentEdge, currentLineIndex);
            return;
        }

        // Same edge: fire at every line between prev and current
        // Direction can be either increasing or decreasing
        int step = currentLineIndex >= _prevLineIndex ? 1 : -1;

        for (int line = _prevLineIndex + step; line != currentLineIndex + step; line += step)
            TryFireAtLine(currentEdge, line);

        _prevLineIndex = currentLineIndex;
    }

    private void TryFireAtLine(GridEdge edge, int lineIndex)
    {
        PixelCell target = PixelGrid.Instance.GetFrontCell(edge, lineIndex);
        if (target == null) return;

        bool hit = PixelGrid.Instance.HandleHit(target, ColorIndex);
        if (hit) RegisterHit();
    }

    private void RegisterHit()
    {
        _remainingPixels--;
        UpdateLabel();

        if (_remainingPixels <= 0)
            ReturnToPool();
    }

    // ═════════════════════════════════════════════════════════════════
    // Slot Movement (PrimeTween)
    // ═════════════════════════════════════════════════════════════════

    private void EnterSlotMove()
    {
        float duration = Vector3.Distance(transform.position, _slotTargetPosition) / moveToSlotSpeed;

        transform.JumpTo(_slotTargetPosition, 1, duration).OnComplete(() =>
        {
            transform.position = _slotTargetPosition;
        });
    }

    // ═════════════════════════════════════════════════════════════════
    // Pool
    // ═════════════════════════════════════════════════════════════════

    private void ReturnToPool()
    {
        StopAllProcesses();

        if (_state.Value == State.OnSpline)
            ShooterManager.Instance.ExitSpline(this);
        else if (_state.Value == State.Slotted)
            ShooterManager.Instance.ExitSlot(this);

        OnRequestRelease?.Invoke(this);
    }

    // ═════════════════════════════════════════════════════════════════
    // Edge / Line Detection
    // ═════════════════════════════════════════════════════════════════

    private GridEdge GetCurrentEdge()
    {
        Vector3 toShooter = transform.position - PixelGrid.Instance.GridBounds.center;
        float normX = Mathf.Abs(toShooter.x) / PixelGrid.Instance.GridBounds.extents.x;
        float normZ = Mathf.Abs(toShooter.z) / PixelGrid.Instance.GridBounds.extents.z;

        if (normZ >= normX)
            return toShooter.z > 0 ? GridEdge.Top : GridEdge.Bottom;
        else
            return toShooter.x > 0 ? GridEdge.Right : GridEdge.Left;
    }

    private int GetCurrentLineIndex(GridEdge edge)
    {
        return edge switch
        {
            GridEdge.Top    or GridEdge.Bottom => PixelGrid.Instance.WorldXToCol(transform.position.x),
            GridEdge.Left   or GridEdge.Right  => PixelGrid.Instance.WorldZToRow(transform.position.z),
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
}