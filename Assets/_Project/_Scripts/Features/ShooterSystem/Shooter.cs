using UnityEngine;
using UnityEngine.Splines;
using TMPro;
using PrimeTween;
using System;

public class Shooter : MonoBehaviour
{
    // ── State ─────────────────────────────────────────────────────────

    public enum State { Waiting, OnSpline, Slotted }

    private State _state;
    public State CurrentState => _state;

    // ── Inspector ─────────────────────────────────────────────────────

    [SerializeField] private MeshRenderer _meshRenderer;

    [Header("Spline")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("UI")]
    [SerializeField] private TextMeshPro pixelCountLabel;

    // ── Runtime ───────────────────────────────────────────────────────

    public int ColorIndex { get; private set; }

    private int _remainingPixels;
    private Transform _slotTargetTransform;

    private Sequence _currentSequence;

    private int _prevLineIndex = -1;
    private GridEdge _prevEdge;

    public Action<Shooter> OnRequestRelease;

    private float splineMovementSpeed;
    private readonly float moveToSlotSpeed = 8f;

    private MaterialPropertyBlock _mpb;
    private Color _baseColor;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    // ═════════════════════════════════════════════════════════════════
    // Lifecycle
    // ═════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
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

        _prevLineIndex = -1;

        _currentSequence = Sequence.Create()
            .Group(transform.JumpTo(splineContainer.EvaluatePosition(0), 1, .5f))
            .Group(Tween.Rotation(transform,(Vector3)splineContainer.EvaluateTangent(0)+Vector3.up*90,.5f)
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
                        transform.rotation = Quaternion.LookRotation(tangent, Vector3.up)* Quaternion.Euler(0f, 90f, 0f);
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

        GridEdge currentEdge = GetCurrentEdge();
        int currentLineIndex = GetCurrentLineIndex(currentEdge);

        if (currentEdge != _prevEdge)
        {
            _prevEdge = currentEdge;
            _prevLineIndex = currentLineIndex;
            TryFireAtLine(currentEdge, currentLineIndex);
            return;
        }

        int step = currentLineIndex >= _prevLineIndex ? 1 : -1;

        for (int line = _prevLineIndex + step; line != currentLineIndex + step; line += step)
            TryFireAtLine(currentEdge, line);

        _prevLineIndex = currentLineIndex;
    }

    private void TryFireAtLine(GridEdge edge, int lineIndex)
    {
        PixelCell target = PixelGrid.Instance.GetFrontCell(edge, lineIndex);
        if (target == null) return;

        if (target.ColorIndex==ColorIndex)
        {
            RegisterHit();
            BulletPool.Instance.Fire(transform.position, target,ColorIndex,10);
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
            .Group(transform.JumpTo(_slotTargetTransform.position, 1, duration)
                .OnComplete(() =>
                {
                    transform.position = _slotTargetTransform.position;
                })
                .Group(Tween.Rotation(transform, _slotTargetTransform.rotation.eulerAngles, duration)));
    }

    // ═════════════════════════════════════════════════════════════════
    // Pool
    // ═════════════════════════════════════════════════════════════════

    private void ReturnToPool()
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
                        Ease.Linear,cycleMode:CycleMode.Incremental,cycles:8)
           .Group(
                Tween.Position(transform,transform.position+Vector3.forward, .5f,ease:Ease.InBack)))
            .Group(
                Tween.Scale(transform,Vector3.zero,.5f,ease:Ease.InBack).OnComplete(() =>
                {
                    OnRequestRelease?.Invoke(this);
                })
            );
    }

    // ═════════════════════════════════════════════════════════════════
    // Edge / Line Detection
    // ═════════════════════════════════════════════════════════════════

    private GridEdge GetCurrentEdge()
    {
        Vector3 toShooter =
            transform.position - PixelGrid.Instance.GridBounds.center;

        float normX =
            Mathf.Abs(toShooter.x) / PixelGrid.Instance.GridBounds.extents.x;

        float normZ =
            Mathf.Abs(toShooter.z) / PixelGrid.Instance.GridBounds.extents.z;

        if (normZ >= normX)
            return toShooter.z > 0 ? GridEdge.Top : GridEdge.Bottom;
        else
            return toShooter.x > 0 ? GridEdge.Right : GridEdge.Left;
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
}