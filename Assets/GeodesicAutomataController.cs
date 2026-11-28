using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections;

namespace GeodesicAutomata
{
    /// <summary>
    /// GeodesicGrid 上のパネル情報を取得・更新するコントローラー。
    /// シンプルに grid.Panels から直接情報を取得し、ColorMap で色を設定します。
    /// </summary>
    public class GeodesicAutomataController : MonoBehaviour
    {
        [SerializeField]
        private GeodesicGrid grid;

        [Header("Simulation Settings")]
        public float updateInterval = 0.5f;

        [Header("Color Map Settings")]
        public ColorMapPreset colorPreset = ColorMapPreset.Viridis;
        [Range(2, 64)]
        public int totalStates = 7;

        private ColorMap _colors;
        private float _timer;

        private int[] _currentStatesBuffer;
        private int[] _nextStatesBuffer;
        private int[][] _neighborIdsCache;

        private volatile bool _isCalculating;
        private volatile bool _hasPendingResult;
        private volatile bool _isRendering;
        private CancellationTokenSource _cts;

        private Plane[] _frustumPlanes = new Plane[6];
        private Vector3 _cameraPos;
        private Camera cam;

        int[] _visibleIndices;
        private int _visibleCount;

        private Vector3 _lastCamPos;
        private Quaternion _lastCamRot;
        private bool _hasLastCamState;

        private void Awake()
        {
            if (grid == null)
            {
                grid = GetComponent<GeodesicGrid>();
            }
            _cts = new CancellationTokenSource();
        }

        private void Start()
        {
            if (grid != null && (grid.Panels == null || grid.Panels.Length == 0))
            {
                grid.BuildGeodesicGrid();
            }

            // カラーパレットの生成
            _colors = new ColorMap(totalStates, colorPreset);

            cam = Camera.main;
            InitializeAutomata();
        }

        private void InitializeAutomata()
        {
            if (grid == null || grid.Panels == null) return;

            var panels = grid.Panels;
            int count = panels.Length;

            // EnsureBuffers(panels, count);
            _currentStatesBuffer = new int[count];
            _nextStatesBuffer = new int[count];

            // 初期状態の設定（五角形なら1、六角形なら0）
            for (int i = 0; i < count; i++)
            {
                var panel = panels[i];
                int initialState = panel.NeighborIds.Length == 5 ? 1 : 0;
                panel.State = initialState;
                _currentStatesBuffer[i] = initialState;
                panel.SetColor(_colors[initialState]);
            }
            _visibleIndices = new int[panels.Length];
        }

        private void Update()
        {

            if (grid == null || grid.Panels == null || grid.Panels.Length == 0) return;

            if (_isRendering) return;

            // // 別スレッドの計算が終わっていたらメインスレッドで描画反映
            if (_hasPendingResult)
            {
                _hasPendingResult = false;
                _isRendering = true;
                StartCoroutine(ApplyRender());
            }

            _timer += Time.deltaTime;
            if (_timer >= updateInterval && !_isCalculating)
            {
                _timer = 0f;

                CalculateCellularAutomata();

                _hasPendingResult = true;
                _isCalculating = false;
            }
        }

        private void LateUpdate()
        {
            cam ??= Camera.main;
            if (cam == null) return;

            UpdateCameraCullInfo();

            if (IsCameraChanged())
            {
                FlushVisibleDirtyPanels(cam);
            }
        }

        private void UpdateCameraCullInfo()
        {
            if (cam == null) return;

            GeometryUtility.CalculateFrustumPlanes(cam, _frustumPlanes);
            _cameraPos = cam.transform.position;
        }

        private bool IsCameraChanged()
        {
            Transform t = cam.transform;
            Vector3 pos = t.position;
            Quaternion rot = t.rotation;

            if (!_hasLastCamState)
            {
                _lastCamPos = pos;
                _lastCamRot = rot;
                _hasLastCamState = true;
                return true;
            }

            bool posSame = (_lastCamPos - pos).sqrMagnitude <= 1e-8f;
            bool rotSame = Quaternion.Angle(_lastCamRot, rot) <= 0.001f;

            if (posSame && rotSame)
            {
                return false;
            }

            _lastCamPos = pos;
            _lastCamRot = rot;
            _cameraPos = pos;
            return true;
        }

        private void FlushVisibleDirtyPanels(Camera cam)
        {
            if (cam == null) return;

            GeometryUtility.CalculateFrustumPlanes(cam, _frustumPlanes);

            var panels = grid.Panels;
            for (int i = 0; i < panels.Length; i++)
            {
                panels[i].FlushColorIfDirty(_frustumPlanes, _cameraPos);
            }
        }

        private void CalculateCellularAutomata()
        {

            var panels = grid.Panels;
            int count = panels.Length;
            int mod = totalStates;
            int[] nextStates = _nextStatesBuffer;

            for (int i = 0; i < count; i++)
            {
                int[] neighborIds = panels[i].NeighborIds;
                int sum = 0;
                for (int j = 0; j < neighborIds.Length; j++)
                {
                    sum += panels[neighborIds[j]].State;
                }

                nextStates[i] = (panels[i].State + sum) % mod;
            }


        }

        /// <summary>
        /// 計算結果をパネルの State と色に反映する（メインスレッド実行）
        /// </summary>
        private IEnumerator ApplyRender()
        {
            if (grid == null || grid.Panels == null || _colors == null) yield break;

            float sliceStart = Time.realtimeSinceStartup;

            var panels = grid.Panels;
            int count = Mathf.Min(panels.Length, _nextStatesBuffer.Length);
            _visibleCount = 0;

            for (int i = 0; i < count; i++)
            {
                int nextState = _nextStatesBuffer[i];
                GeodesicPanel panel = panels[i];

                bool visible = panel.IsVisibleToCamera(_frustumPlanes, _cameraPos);
                if (visible)
                {
                    _visibleIndices[_visibleCount++] = i;
                }

                int prevState = panel.State;
                if (prevState == nextState) continue;

                panel.State = nextState;
                panel.SetColor(_colors[nextState]);

                if (Time.realtimeSinceStartup - sliceStart >= 0.005)
                {
                    yield return null;
                    sliceStart = Time.realtimeSinceStartup;
                }
            }

            yield return null;

            for (int idx = 0; idx < _visibleCount; idx++)
            {
                var panel = panels[_visibleIndices[idx]];
                panel.ApplyColorToRenderer();
            }

            _isRendering = false;
        }
    }
}
