using UnityEngine;
using System.Collections.Generic;

public class ShooterManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────
    [Header("Capacity")]
    [SerializeField] private int splineCapacity = 5;
    [SerializeField] private int slotCapacity = 5;

    [Header("Slot Positions")]
    [SerializeField] private Transform[] slotTransforms;
    
    [Header("Shooter Settings")]
    [SerializeField]private float shooterMovementSpeed=3;

    // ── Runtime ───────────────────────────────────────────────────────
    private List<Shooter> _shootersOnSpline = new List<Shooter>();
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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
        if (IsSplineFull) return false;

        _shootersOnSpline.Add(shooter);
        return true;
    }

    public void ExitSpline(Shooter shooter)
    {
        _shootersOnSpline.Remove(shooter);
    }

    // ═════════════════════════════════════════════════════════════════
    // Slot
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by a Shooter when it finishes the spline and wants to enter a slot.
    /// Returns the slot position if available, triggers lose if slot is full.
    /// </summary>
    public bool TryEnterSlot(Shooter shooter, out Vector3 slotPosition)
    {
        slotPosition = Vector3.zero;

        if (IsSlotFull)
        {
            OnLose?.Invoke();
            return false;
        }

        _shootersInSlot.Add(shooter);
        slotPosition = GetNextSlotPosition();
        return true;
    }

    public void ExitSlot(Shooter shooter)
    {
        _shootersInSlot.Remove(shooter);
    }

    private Vector3 GetNextSlotPosition()
    {
        int index = _shootersInSlot.Count - 1;

        if (slotTransforms != null && index < slotTransforms.Length && slotTransforms[index] != null)
            return slotTransforms[index].position;

        // Fallback: stack them in a line if no transforms assigned
        return new Vector3(index * 1.5f, 0f, -5f);
    }
}
