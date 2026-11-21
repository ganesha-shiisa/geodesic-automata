# Geodesic Automata

<!--
<p align="center">
  <img src=".github/images/geodesic-automata.gif" width="800">
</p>
-->

`Geodesic Automata` は、正20面体の細分化とその双対（Dual）によって生成される**ジオデシック球（サッカーボール状の五角形・六角形タイル構造）**の上で展開される、3Dセル・オートマトンシミュレーション作品です。

平面のグリッドではなく球体表面を離散空間として扱い、隣接するトポロジー関係に基づいたパターンの遷移や、Pythonのカラーマップ（Matplotlib風）を用いたダイナミックな色彩の変化をリアルタイムに楽しむことができます。

## Play Now

👉 https://ganesha-shiisa.github.io/geodesic-automata/ _(※WebGL公開後にリンク予定)_

## 主な機能 (Features)

- **ジオデシック・デュアルグリッド生成 (Geodesic Grid)**:
  - 正20面体（Icosahedron）の細分化と球面投影、その双対（Dual）幾何から、12個の五角形と多数の六角形からなる滑らかな球面パネルメッシュをプロシージャル生成。
  - 三角形エッジ構造からパネル同士の正確なトポロジー的隣接関係（周りのパネルID）を自動抽出。
- **球面セル・オートマトン (Cellular Automata on Sphere)**:
  - 球面上の各パネルを1つのセルとして扱い、周囲のパネル状態（State）を読み取って周期的に次状態へ遷移。
  - 平面グリッドとは異なり端が存在しない閉じた球面境界ならではの循環パターン。
- **Matplotlib風 カラーマップ機能 (ColorMap System)**:
  - Pythonの `plt.get_cmap(name)(np.linspace(0, 1, mod))` と同等の色離散化システムを Unity C# 上で再現。
  - `Viridis`, `Plasma`, `Inferno`, `Magma`, `Coolwarm`, `Turbo` 等の人気プリセットを標準搭載。
  - セルの状態数に応じたカラーパレット（`ColorMap(totalStates, preset)`）から色をリアルタイム反映。
- **最適化・メモリ管理 (Performance & Memory)**:
  - `MaterialPropertyBlock` の活用により、マテリアルインスタンスを増やさずに単一マテリアルで全パネルの個別カラー描画を実現。
  - 状態更新バッファの再利用による GC Allocation の極小化。
  - 状態不変セルの描画スキップによるレンダリング負荷軽減。

---

## 開発ロードマップ & 実装状況 (Roadmap)

現在実装済みの機能と、今後予定されている機能（未対応項目）の一覧です。

### 実装済み (Implemented)

- [x] プロシージャルなジオデシック・デュアルメッシュ生成（五角形・六角形パネル）
- [x] 三角形エッジグラフに基づく各パネルの隣接ID自動検出
- [x] 各パネルの情報保持・色変更インターフェース (`IGeodesicPanel`, `GeodesicPanel`)
- [x] 独立したカラーマップ生成クラス (`ColorMap`, `ColorMapPreset`)
- [x] パネルコントローラーによるオートマトン更新ループ (`GeodesicAutomataController`)
- [x] Gitコミット・シーン軽量化用のパネル一括消去コンテキストメニュー (`Clear All Panels`)

### 🚧 今後の実装予定・未対応 (Planned / Work in Progress)

- [ ] **ユーザーインタラクション (マウス/タッチ操作)**: _(未対応)_
  - マウスレイキャストやクリックで特定のパネルの状態を反転・刺激する機能。
- [ ] **カメラ操作 (Orbit Camera)**: _(未対応)_
  - ドラッグ操作で球体を自由な角度から回転・観察・ズームできるオービットカメラ。
- [ ] **多彩なオートマトンルールの追加**: _(未対応)_
  - 球面ライフゲーム（Conway's Game of Life on Sphere）。
  - 熱拡散・波動伝播（Wave Simulation）。
  - 励起媒質・BZ反応風シミュレーション（Excitable Media）。
- [ ] **UIコントロールパネル (Runtime Settings)**: _(未対応)_
  - 実行中にカラーマッププリセットや状態数、シミュレーション速度、リセットを行えるGUI。
- [ ] **Compute Shader / GPU Instancing による超多面体最適化**: _(未対応)_
  - 細分化レベル（Subdivision Level 4以上、数万セル）でも60fps以上を維持するGPU駆動パイプライン。

---

## 操作方法 (Controls)

| 操作                  | 割り当て                            | 説明                                                                          |
| :-------------------- | :---------------------------------- | :---------------------------------------------------------------------------- |
| **観察 (カメラ操作)** | マウスドラッグ _(※今後実装予定)_    | 球体を回転させてあらゆる角度から観察します。                                  |
| **セル刺激 (入力)**   | クリック / タップ _(※今後実装予定)_ | 任意のパネルをクリックして刺激や状態変化を与えます。                          |
| **パラメータ調整**    | インスペクター                      | `updateInterval`, `totalStates`, `colorPreset` をインスペクターから調整可能。 |

---

## プロジェクトのセットアップ (Getting Started)

1. **リポジトリのクローン**:
   ```bash
   git clone https://github.com/ganesha-shiisa/geodesic-automata.git
   ```
2. **Unity で開く**:
   - Unity Hub を起動し、`Unity 6` (Universal Render Pipeline / URP) でプロジェクトを追加して開きます。
3. **メインシーンを開いて再生**:
   - `Assets/Scenes/GeodesicAutomataScene.unity` を開きます。
   - **Play（再生）ボタンを押す**と、スクリプトが自動的にジオデシック球を生成し、セル・オートマトンが開始されます。

> [!NOTE]
> シーンファイル保存時やGitコミット時は、シーン肥大化を防ぐため、`GeodesicGrid` のインスペクターメニューから **`Clear All Panels (For Git Commit / Clean Scene)`** を実行して保存することを推奨します（再生時に自動生成されます）。

---

## ライセンス (License)

[MIT License](LICENSE)

## 作者 (Author)

- **GaneshaShiisa** - [GitHub Profile](https://github.com/ganesha-shiisa)
