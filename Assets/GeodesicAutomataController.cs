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

        private Plane[] _frustumPlanes;
        private Vector3 _cameraPos;
        private Camera cam;

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
        }

        private void Update()
        {
            if (grid == null || grid.Panels == null || grid.Panels.Length == 0) return;

            UpdateCameraCullInfo();

            if (_isRendering) return;

            // 別スレッドの計算が終わっていたらメインスレッドで描画反映
            if (_hasPendingResult)
            {
                _hasPendingResult = false;
                _isRendering = true;
                StartCoroutine(ApplyRender());
            }

            // カメラ視界に入った「未反映（Dirty）カラー」のパネルを吐き出す
            FlushVisibleDirtyPanels();

            // インターバル経過＆計算中でなければ、次の計算を別スレッドへ投げる
            _timer += Time.deltaTime;

            if (_timer >= updateInterval && !_isCalculating)
            {
                _timer = 0f;

                CalculateCellularAutomata();

                _hasPendingResult = true;
                _isCalculating = false;
            }
        }

        private void UpdateCameraCullInfo()
        {

            if (cam == null) return;

            if (_frustumPlanes == null)
            {
                _frustumPlanes = new Plane[6];
            }

            GeometryUtility.CalculateFrustumPlanes(cam, _frustumPlanes);
            _cameraPos = cam.transform.position;
        }

        private void FlushVisibleDirtyPanels()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            if (_frustumPlanes == null)
            {
                _frustumPlanes = new Plane[6];
            }

            // カメラが動いていなければスキップするならここでも判定できるぜ
            GeometryUtility.CalculateFrustumPlanes(cam, _frustumPlanes);

            var panels = grid.Panels;
            for (int i = 0; i < panels.Length; i++)
            {
                panels[i].FlushColorIfDirty(_frustumPlanes);
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
            float sliceStart = Time.realtimeSinceStartup;

            // if (grid == null || grid.Panels == null || _colors == null) return;

            var panels = grid.Panels;
            int count = Mathf.Min(panels.Length, _nextStatesBuffer.Length);

            int[] _visibleIndices = new int[panels.Length];
            int _visibleCount = 0;
            for (int i = 0; i < count; i++)
            {
                int nextState = _nextStatesBuffer[i];
                GeodesicPanel panel = panels[i];

                int prevState = panel.State;
                if (prevState == nextState) continue;

                panel.State = nextState;
                panel.SetColor(_colors[nextState], _frustumPlanes);

                if (panel.IsVisibleToCamera(_frustumPlanes))
                {
                    _visibleIndices[_visibleCount++] = i;
                }

                if (Time.realtimeSinceStartup - sliceStart >= 0.01)
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
