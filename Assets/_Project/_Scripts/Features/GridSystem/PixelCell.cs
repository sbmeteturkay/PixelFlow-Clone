using Game.Core;
using PrimeTween;
using UnityEngine;

public class PixelCell : MonoBehaviour
{
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    [SerializeField] private MeshRenderer _meshRenderer;
    private Color _baseColor;

    // ── Private ───────────────────────────────────────────────────────

    private Sequence sequence;

    // ── Properties ────────────────────────────────────────────────────
    public int Column { get; private set; }
    public int Row { get; private set; }
    public int ColorIndex { get; private set; }
    public bool IsEmpty => ColorIndex < 0;
    public bool IsAlive { get; private set; }
    public bool IsShooted { get; private set; }

    // ─────────────────────────────────────────────────────────────────


    public void Initialize(int col, int row, int colorIndex, Color color)
    {
        Column = col;
        Row = row;
        ColorIndex = colorIndex;
        IsAlive = true;

        _baseColor = color;
        ApplyColor(_baseColor);

        gameObject.SetActive(!IsEmpty);
    }

    private void ApplyColor(Color color)
    {
        _meshRenderer.material.SetColor(BaseColorID, color);
    }

    public bool TryHit(int shooterColorIndex)
    {
        if (!IsAlive || IsEmpty) return false;

        if (shooterColorIndex == ColorIndex)
        {
            DestroyCell();
            return true;
        }

        return false;
    }

    public void ResetCell()
    {
        sequence.Stop();
        IsShooted = false;
        IsAlive = true;
        transform.localScale = Vector3.one;
        ApplyColor(_baseColor);
        gameObject.SetActive(!IsEmpty);
    }

    private void DestroyCell()
    {
        IsAlive = false;

        Vector3 baseScale = transform.localScale;

        Vector3 squashScale = new(
            baseScale.x * 1.08f,
            baseScale.y * 0.95f,
            baseScale.z
        );

        Vector3 popScale = new(
            baseScale.x * 1.3f,
            baseScale.y * 1.05f,
            baseScale.z
        );

        AudioManager.Instance.PlayCellDestroyed();

        sequence.Stop();
        sequence = Sequence.Create()
            .Chain(Tween.Scale(transform, squashScale, 0.08f, Ease.OutQuad))
            .Chain(Tween.Scale(transform, popScale, 0.06f, Ease.OutCubic))
            .Chain(Tween.Scale(transform, Vector3.zero, 0.18f, Ease.InBack))
            .OnComplete(() => gameObject.SetActive(false));
    }

    public void SetShooted()
    {
        IsShooted = true;
    }
}