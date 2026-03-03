using PrimeTween;
using UnityEngine;

public static class PrimeTweenExtensions
{
    public static Sequence JumpTo(this Transform transform,Vector3 target, float jumpHeight, float duration)
    {
        Vector3 start = transform.position;

        return Sequence.Create()
            .Group(
                // XZ linear hareket
                Tween.Position(transform, target, duration)
            )
            .Group(
                // Y ekseninde parabol
                Tween.Custom(
                    0f,
                    1f,
                    duration,
                    t =>
                    {
                        float yOffset = 4f * jumpHeight * t * (1f - t);
                        Vector3 pos = transform.position;
                        pos.y = Mathf.Lerp(start.y, target.y, t) + yOffset;
                        transform.position = pos;
                    })
            );
    }
}
