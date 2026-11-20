using System.Collections.Generic;
using UnityEngine;

namespace GeodesicAutomata
{
    /// <summary>
    /// GeodesicGrid 上のパネル情報を取得・更新するコントローラー。
    /// 配列の毎フレーム確保を排除し、GCアロケーションをゼロに抑えて動作します。
    /// </summary>
    public class GeodesicAutomataController : MonoBehaviour
    {
        [SerializeField]
        private GeodesicGrid grid;

        [Header("Simulation Settings")]
        public float updateInterval = 0.5f;
        public Color activeColor = Color.cyan;
        public Color inactiveColor = Color.gray;

        private float _timer;
        private int[] _nextStatesBuffer; // GCアロケーション回避用のバッファ

        private void Awake()
        {
            if (grid == null)
            {
                grid = GetComponent<GeodesicGrid>();
            }
        }

        private void Start()
        {
            if (grid != null && (grid.Panels == null || grid.Panels.Count == 0))
            {
                grid.BuildGeodesicGrid();
            }

            InitializeAutomata();
        }

        private void InitializeAutomata()
        {
            if (grid == null || grid.Panels == null) return;

            int count = grid.Panels.Count;
            _nextStatesBuffer = new int[count];

            // ランダムに初期状態（0 または 1）を割り当て
            foreach (var panel in grid.Panels)
            {
                panel.State = Random.value > 0.7f ? 1 : 0;
                panel.SetColor(panel.State == 1 ? activeColor : inactiveColor);
            }
        }

        private void Update()
        {
            if (grid == null || grid.Panels == null || grid.Panels.Count == 0) return;

            _timer += Time.deltaTime;
            if (_timer >= updateInterval)
            {
                _timer = 0f;
                StepAutomata();
            }
        }

        /// <summary>
        /// 1ステップのセル・オートマトン更新処理（毎回の new を無くしGCを発生させない）
        /// </summary>
        private void StepAutomata()
        {
            var panels = grid.Panels;
            int count = panels.Count;

            // バッファサイズが不足している場合のみリサイズ（通常は Start 時の1回のみ）
            if (_nextStatesBuffer == null || _nextStatesBuffer.Length != count)
            {
                _nextStatesBuffer = new int[count];
            }

            // 1. 各パネルの周囲のパネルIDから現在の情報を取得し、次の状態を計算
            for (int i = 0; i < count; i++)
            {
                GeodesicPanel panel = panels[i];
                IReadOnlyList<int> neighborIds = panel.NeighborIds;

                int activeNeighbors = 0;
                int neighborCount = neighborIds.Count;
                for (int j = 0; j < neighborCount; j++)
                {
                    int neighborId = neighborIds[j];
                    if (neighborId >= 0 && neighborId < count)
                    {
                        if (panels[neighborId].State == 1)
                        {
                            activeNeighbors++;
                        }
                    }
                }

                // セル・オートマトン ルール
                if (panel.State == 1)
                {
                    _nextStatesBuffer[i] = (activeNeighbors == 2 || activeNeighbors == 3) ? 1 : 0;
                }
                else
                {
                    _nextStatesBuffer[i] = (activeNeighbors == 2) ? 1 : 0;
                }
            }

            // 2. 計算結果を各パネルへ指示して適用
            for (int i = 0; i < count; i++)
            {
                GeodesicPanel panel = panels[i];
                int nextState = _nextStatesBuffer[i];
                panel.State = nextState;
                panel.SetColor(nextState == 1 ? activeColor : inactiveColor);
            }
        }
    }
}
