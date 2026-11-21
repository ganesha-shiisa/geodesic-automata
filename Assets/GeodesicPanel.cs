using System.Collections.Generic;
using UnityEngine;

namespace GeodesicAutomata
{
    public interface IGeodesicPanel
    {
        int Id { get; }
        bool IsPentagon { get; }
        Vector3 Position { get; }
        int[] NeighborIds { get; }

        int State { get; set; }
        float CustomValue { get; set; }

        void SetColor(Color color, Plane[] frustumPlanes = null);
        Color GetColor();

    }

    [RequireComponent(typeof(MeshRenderer))]
    public class GeodesicPanel : MonoBehaviour, IGeodesicPanel
    {
        [SerializeField] private int _id;
        [SerializeField] private bool _isPentagon;
        [SerializeField] private int[] _neighborIds;

        private MeshRenderer _renderer;
        private static MaterialPropertyBlock _sharedPropBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP
        private static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in

        private Color _currentColor = new Color(-1f, -1f, -1f, -1f);
        private bool _isColorInitialized = false;
        private bool _isColorDirty = false;

        public int Id => _id;
        public bool IsPentagon => _isPentagon;
        public Vector3 Position => transform.position;

        /// <summary>
        /// ループの仮想呼び出しオーバーヘッドを無くすため具体型 int[] を公開
        /// </summary>
        public int[] NeighborIds => _neighborIds;

        public int State { get; set; }
        public float CustomValue { get; set; }

        public void Initialize(int id, bool isPentagon, int[] neighbors, Color initialColor)
        {
            _id = id;
            _isPentagon = isPentagon;
            _neighborIds = neighbors;

            EnsureRenderer();
            SetColor(initialColor);
        }

        public void SetColor(Color color, Plane[] frustumPlanes = null)
        {
            if (_isColorInitialized && _currentColor == color)
            {
                return;
            }

            _currentColor = color;
            _isColorInitialized = true;
            _isColorDirty = true;
            EnsureRenderer();

            if (IsVisibleToCamera(frustumPlanes))
            {
                ApplyColorToRenderer();
            }
        }

        public Color GetColor() => _currentColor;

        private void EnsureRenderer()
        {
            _renderer ??= GetComponent<MeshRenderer>();
            _sharedPropBlock ??= new MaterialPropertyBlock();
        }

        public void FlushColorIfDirty(Plane[] frustumPlanes)
        {
            if (_isColorDirty && IsVisibleToCamera(frustumPlanes))
            {
                ApplyColorToRenderer();
            }
        }

        public bool IsVisibleToCamera(Plane[] frustumPlanes)
        {
            if (frustumPlanes == null) return true;

            EnsureRenderer();
            if (_renderer == null) return false;

            // Renderer の Bounds がカメラの視錐台（6枚の平面）と交差・内包しているか判定
            return GeometryUtility.TestPlanesAABB(frustumPlanes, _renderer.bounds);
        }

        private void ApplyColorToRenderer()
        {
            EnsureRenderer();
            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_sharedPropBlock);
            _sharedPropBlock.SetColor(BaseColorId, _currentColor);
            _sharedPropBlock.SetColor(ColorId, _currentColor);
            _renderer.SetPropertyBlock(_sharedPropBlock);

            _isColorDirty = false;
        }
    }
}
