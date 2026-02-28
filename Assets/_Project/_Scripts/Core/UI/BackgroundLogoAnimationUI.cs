
    using UnityEngine;

    public class BackgroundLogoAnimationUI : MonoBehaviour
    {
        public Vector2 speed = new Vector2(0.1f, -0.05f);

        private Material mat;
        private Vector2 offset;

        void Start()
        {
            // Renderer varsa (World Space)
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
                mat = renderer.material; // instance oluşturur, orijinali bozmaz

            // UI Image varsa
            var image = GetComponent<UnityEngine.UI.Image>();
            if (image != null)
                mat = image.material;
        }

        void Update()
        {
            offset += speed * Time.deltaTime;
            mat.SetVector("_MainTex_ST", new Vector4(1, 1, offset.x, offset.y));
            // veya daha kısa:
            // mat.mainTextureOffset = offset;
        }
    }
