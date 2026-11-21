using UnityEngine;

namespace GeodesicAutomata
{
    /// <summary>
    /// GeodesicGrid 上のパネル情報を取得・更新するコントローラー。
    /// 配列のキャッシュ、具体型（int[]）の直接ループ、状態不変時の描画更新スキップ等により
    /// UpdateScene / StdRender.ApplyShader の呼び出し回数とCPU負荷を最小化します。
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
        private Color[] _cachedColorTable; // 配列インデックスで直引きして関数呼び出しをなくす
        private float _timer;
        private bool _isStepAutomata = false;

        // キャッシュ用配列
        private GeodesicPanel[] _cachedPanels;
        private int[][] _cachedNeighbors;
        private int[] _nextStatesBuffer;

        private void Awake()
        {
            if (grid == null)
            {
                grid = GetComponent<GeodesicGrid>();
            }
        }

        private void Start()
        {
            if (grid != null && (grid.Panels == null || grid.Panels.Length == 0))
            {
                grid.BuildGeodesicGrid();
            }

            CacheGridReferences();
            InitializeColorTable();
            InitializeAutomata();
        }

        private void CacheGridReferences()
        {
            if (grid == null || grid.Panels == null) return;

            _cachedPanels = grid.Panels;
            int count = _cachedPanels.Length;

            _nextStatesBuffer = new int[count];
            _cachedNeighbors = new int[count][];

            // 各パネルの NeighborIds (int[]) 参照を配列化してローカルに保持（プロパティ経由を無くす）
            for (int i = 0; i < count; i++)
            {
                _cachedNeighbors[i] = _cachedPanels[i].NeighborIds;
            }
        }

        private void InitializeColorTable()
        {
            _colors = new ColorMap(totalStates, colorPreset);
            _cachedColorTable = new Color[totalStates];
            for (int i = 0; i < totalStates; i++)
            {
                _cachedColorTable[i] = _colors[i];
            }
        }

        private void InitializeAutomata()
        {
            if (_cachedPanels == null || _cachedPanels.Length == 0) return;

            int count = _cachedPanels.Length;
            for (int i = 0; i < count; i++)
            {
                GeodesicPanel panel = _cachedPanels[i];
                panel.State = panel.NeighborIds.Length == 5 ? 1 : 0;
                panel.SetColor(_cachedColorTable[panel.State]);
            }
        }

        private void Update()
        {
            if (_cachedPanels == null || _cachedPanels.Length == 0) return;

            _timer += Time.deltaTime;
            if (_timer >= updateInterval && !_isStepAutomata)
            {
                _isStepAutomata = true;
                _timer = 0f;
                StepAutomata();
                _isStepAutomata = false;
            }
        }

        /// <summary>
        /// 1ステップのセル・オートマトン更新処理（徹底したキャッシュとスキップ最適化）
        /// </summary>
        private void StepAutomata()
        {
            GeodesicPanel[] panels = _cachedPanels;
            int[][] allNeighbors = _cachedNeighbors;
            int[] nextStates = _nextStatesBuffer;
            Color[] colorTable = _cachedColorTable;
            int count = panels.Length;
            int mod = totalStates;

            // 1. 周囲のパネル情報から次の状態（State）を計算
            for (int i = 0; i < count; i++)
            {
                int[] neighbors = allNeighbors[i];
                int neighborCount = neighbors.Length;

                int sum = 0;
                for (int j = 0; j < neighborCount; j++)
                {
                    sum += panels[neighbors[j]].State;
                }
                nextStates[i] = sum % mod;
            }

            // 2. 状態の適用と描画更新
            for (int i = 0; i < count; i++)
            {
                GeodesicPanel panel = panels[i];
                int nextState = nextStates[i];

                if (panel.State != nextState)
                {
                    panel.State = nextState;
                    panel.SetColor(colorTable[nextState]);
                }
            }
        }
    }
}
