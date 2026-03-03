using PrimeTween;
using UnityEngine;

public class ConveyAnimation : MonoBehaviour
{
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private float speed;
    void Start()
    {
        Tween.MaterialMainTextureOffset(
            _renderer.material,
            startValue: Vector2.zero,
            endValue: new (0f, -1f),
            duration: 1f/speed ,
            ease: Ease.Linear,
            cycles: -1,                      
            cycleMode: CycleMode.Restart     
        );
    }
}
