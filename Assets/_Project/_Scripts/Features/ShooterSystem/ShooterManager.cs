using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PrimeTween;

public class ShooterManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────
    [Header("Capacity")]
    [SerializeField] private int splineCapacity = 5;
    [SerializeField] private int slotCapacity = 5;

    [Header("Slot Positions")]
    [SerializeField] private Transform[] slotTransforms;
    [SerializeField] private Transform[] trayParentTransforms;
    
    [Header("Shooter Settings")]
    [SerializeField]private float shooterMovementSpeed=3;

    // ── Runtime ───────────────────────────────────────────────────────
    private List<Shooter> _shootersOnSpline = new List<Shooter>();
    private Dictionary<Shooter,Transform> _trayOnShooters = new ();
    private List<Shooter> _shootersInSlot = new List<Shooter>();

    // ── Events ────────────────────────────────────────────────────────
    public System.Action OnLose;

    // ── Singleton ─────────────────────────────────────────────────────
    public static ShooterManager Instance { get; private set; }

    // ── Properties ────────────────────────────────────────────────────
    public int SplineCount => _shootersOnSpline.Count;
    public int SlotCount => _shootersInSlot.Count;
    public bool IsSplineFull => _shootersOnSpline.Count >= splineCapacity;
    public bool IsSlotFull => _shootersInSlot.Count >= slotCapacity;
    public float ShooterMovementSpeed => shooterMovementSpeed;

    // ─────────────────────────────────────────────────────────────────
     private List<Transform> trayQueue = new List<Transform>();
     private List<Transform> emptyTrayContainerQueue = new List<Transform>();
    

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        foreach (Transform trayParentTransform in trayParentTransforms)
        {
            trayQueue.Add(trayParentTransform);
        }
    }

    // ═════════════════════════════════════════════════════════════════
    // Spline
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by a Shooter when it wants to enter the spline.
    /// Returns true if there is capacity.
    /// </summary>
    public bool TryEnterSpline(Shooter shooter)
    {
        if (IsSplineFull|| trayQueue.Count == 0) return false;

        Transform trayCarrier = trayQueue.OrderBy(c => c.position.x).Last();
        trayQueue.Remove(trayCarrier);
        emptyTrayContainerQueue.Add(trayCarrier);
        Transform tray=trayCarrier.GetChild(0);
        tray.parent = null;
        Sequence.Create()
            .Group(Tween.EulerAngles(tray, tray.eulerAngles, Vector3.zero, 0.4001f))
            .Group(tray.JumpTo( shooter.GetSpline().EvaluatePosition(0), 1,.4001f))
            .OnComplete(() =>
            {
                tray.parent = shooter.transform;
            });
        _trayOnShooters.Add(shooter, tray);
        _shootersOnSpline.Add(shooter);
        return true;
    }

    public void ExitSpline(Shooter shooter)
    {
        Transform emptySlot = emptyTrayContainerQueue.OrderBy(c => c.position.x).First();
        emptyTrayContainerQueue.Remove(emptySlot);
        Transform tray=_trayOnShooters[shooter];
        
        tray.parent = null;
        Sequence.Create()
            .Group(tray.transform.JumpTo(emptySlot.position,1, .5f))
            .Group(Tween.Rotation(tray, emptySlot.rotation, .5f))
            .OnComplete(() =>
            {
                 tray.parent = emptySlot;
                 trayQueue.Add(emptySlot);
            });
        _trayOnShooters.Remove(shooter);
        _shootersOnSpline.Remove(shooter);
    }

    // ═════════════════════════════════════════════════════════════════
    // Slot
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by a Shooter when it finishes the spline and wants to enter a slot.
    /// Returns the slot position if available, triggers lose if slot is full.
    /// </summary>
    public bool TryEnterSlot(Shooter shooter, out Transform slotTransform)
    {
        slotTransform = transform;

        if (IsSlotFull)
        {
            OnLose?.Invoke();
            return false;
        }

        _shootersInSlot.Add(shooter);
        slotTransform = GetNextSlotTransform();
        return true;
    }

    public void ExitSlot(Shooter shooter)
    {
        _shootersInSlot.Remove(shooter);
    }

    private Transform GetNextSlotTransform()
    {
        int index = _shootersInSlot.Count - 1;

        if (slotTransforms != null && index < slotTransforms.Length && slotTransforms[index] != null)
            return slotTransforms[index];

        // Fallback: stack them in a line if no transforms assigned
        return transform;
    }

    public void ClearShooters()
    {
        _shootersInSlot.Clear();
        _shootersOnSpline.Clear();
    }
}
