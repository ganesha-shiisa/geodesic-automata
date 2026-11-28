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

        void SetColor(Color color);
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

        public void SetColor(Color color)
        {
            _currentColor = color;
            _isColorDirty = true;
        }

        public Color GetColor() => _currentColor;

        private void EnsureRenderer()
        {
            _renderer ??= GetComponent<MeshRenderer>();
            _sharedPropBlock ??= new MaterialPropertyBlock();
        }

        public void FlushColorIfDirty(Plane[] frustumPlanes, Vector3 cameraPos)
        {
            if (_isColorDirty && IsVisibleToCamera(frustumPlanes, cameraPos))
            {
                SetRendererEnabled(true);
                ApplyColorToRenderer();
            }
        }

        public bool IsVisibleToCamera(Plane[] frustumPlanes, Vector3 cameraPos, Vector3 sphereCenter = default)
        {
            if (frustumPlanes == null) return true;

            EnsureRenderer();
            if (_renderer == null) return false;

            Vector3 center = _renderer.bounds.center;

            // 裏向き判定（球体の外向き法線と、パネル→カメラの向きの内積）
            Vector3 outwardNormal = (center - sphereCenter).normalized;
            Vector3 toCamera = cameraPos - center;
            if (Vector3.Dot(outwardNormal, toCamera) <= -0.3f)
            {
                return false;
            }

            // 視錐台判定
            Bounds bounds = _renderer.bounds;
            bounds.Expand(Vector3.one * (1.0f * 2f));
            return GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
        }

        public void SetRendererEnabled(bool enabled)
        {
            EnsureRenderer();
            if (_renderer == null) return;

            if (_renderer.enabled != enabled)
            {
                _renderer.enabled = enabled;
            }
        }

        public void ApplyColorToRenderer()
        {
            EnsureRenderer();
            if (_renderer == null) return;

            _sharedPropBlock.Clear();
            _sharedPropBlock.SetColor(BaseColorId, _currentColor);
            _renderer.SetPropertyBlock(_sharedPropBlock);

            _isColorDirty = false;
        }
    }
}
