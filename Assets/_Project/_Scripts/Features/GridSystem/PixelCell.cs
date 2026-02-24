using UnityEngine;
using PrimeTween;

public class PixelCell : MonoBehaviour
{
    // ── Properties ────────────────────────────────────────────────────
    public int Column { get; private set; }
    public int Row { get; private set; }
    public int ColorIndex { get; private set; }
    public bool IsEmpty => ColorIndex < 0;
    public bool IsAlive { get; private set; }
    [SerializeField]private MeshRenderer _meshRenderer;

    // ── Private ───────────────────────────────────────────────────────
    private MaterialPropertyBlock _mpb;
    private Color _baseColor;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    // Eğer built-in kullanıyorsan yukarıyı "_Color" yap

    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
    }

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
        _meshRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(BaseColorID, color);
        _meshRenderer.SetPropertyBlock(_mpb);
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
        IsAlive = true;
        transform.localScale = Vector3.one;
        ApplyColor(_baseColor);
        gameObject.SetActive(!IsEmpty);
    }

    private void DestroyCell()
    {
        IsAlive = false;

        Vector3 baseScale = transform.localScale;

        Vector3 squashScale = new Vector3(
            baseScale.x * 1.08f,
            baseScale.y * 0.95f,
            baseScale.z
        );

        Vector3 popScale = new Vector3(
            baseScale.x * 1.3f,
            baseScale.y * 1.05f,
            baseScale.z
        );

        Sequence.Create()
            .Chain(Tween.Scale(transform, squashScale, 0.08f, ease: Ease.OutQuad))
            .Chain(Tween.Scale(transform, popScale, 0.06f, ease: Ease.OutCubic))
            .Chain(Tween.Scale(transform, Vector3.zero, 0.18f, ease: Ease.InBack))
            .OnComplete(() => gameObject.SetActive(false));
    }
}