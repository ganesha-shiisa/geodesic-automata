using System.Collections.Generic;
using UnityEngine;

namespace GeodesicAutomata
{
    public enum ColorMapPreset
    {
        Viridis,
        Plasma,
        Inferno,
        Magma,
        Turbo,
        Coolwarm,
        Jet,
        Rainbow
    }

    /// <summary>
    /// Python の cmap(np.linspace(0, 1, mod)) のように、
    /// 分割したカラーパレットをインデクサ colors[i] や関数 colors(i) で参照できるクラス。
    /// </summary>
    public class ColorMap
    {
        private readonly Color[] _colors;

        public int Length => _colors.Length;

        public ColorMap(int totalSteps, ColorMapPreset preset = ColorMapPreset.Viridis)
        {
            totalSteps = Mathf.Max(1, totalSteps);
            _colors = new Color[totalSteps];

            for (int i = 0; i < totalSteps; i++)
            {
                _colors[i] = EvaluatePreset(preset, totalSteps == 1 ? 0f : (float)i / (totalSteps - 1));
            }
        }

        public ColorMap(int totalSteps, Gradient customGradient)
        {
            totalSteps = Mathf.Max(1, totalSteps);
            _colors = new Color[totalSteps];

            for (int i = 0; i < totalSteps; i++)
            {
                float t = totalSteps == 1 ? 0f : (float)i / (totalSteps - 1);
                _colors[i] = customGradient != null ? customGradient.Evaluate(t) : Color.white;
            }
        }

        /// <summary>
        /// インデクサ colors[index]
        /// </summary>
        public Color this[int index] => Get(index);

        /// <summary>
        /// 関数形式 colors(index) で呼び出せるメソッド（Invoke）
        /// 例: colors(panel.State)
        /// </summary>
        public Color Invoke(int index) => Get(index);

        /// <summary>
        /// 範囲外インデックスも安全にクランプして色を取得
        /// </summary>
        public Color Get(int index)
        {
            if (_colors == null || _colors.Length == 0) return Color.white;
            int clampedIndex = Mathf.Clamp(index, 0, _colors.Length - 1);
            return _colors[clampedIndex];
        }

        #region Static Helpers

        private static readonly Dictionary<ColorMapPreset, Gradient> PresetGradients = new Dictionary<ColorMapPreset, Gradient>();

        public static Color EvaluatePreset(ColorMapPreset preset, float t)
        {
            t = Mathf.Clamp01(t);

            if (!PresetGradients.TryGetValue(preset, out var grad))
            {
                grad = CreatePresetGradient(preset);
                PresetGradients[preset] = grad;
            }

            return grad.Evaluate(t);
        }

        private static Gradient CreatePresetGradient(ColorMapPreset preset)
        {
            Gradient grad = new Gradient();

            switch (preset)
            {
                case ColorMapPreset.Viridis:
                    grad.SetKeys(
                        new GradientColorKey[]
                        {
                            new GradientColorKey(new Color(0.267f, 0.004f, 0.329f), 0.0f),
                            new GradientColorKey(new Color(0.282f, 0.141f, 0.459f), 0.2f),
                            new GradientColorKey(new Color(0.191f, 0.407f, 0.556f), 0.4f),
                            new GradientColorKey(new Color(0.128f, 0.567f, 0.551f), 0.6f),
                            new GradientColorKey(new Color(0.369f, 0.789f, 0.383f), 0.8f),
                            new GradientColorKey(new Color(0.993f, 0.906f, 0.144f), 1.0f)
                        },
                        new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                    );
                    break;

                case ColorMapPreset.Plasma:
                    grad.SetKeys(
                        new GradientColorKey[]
                        {
                            new GradientColorKey(new Color(0.050f, 0.030f, 0.528f), 0.0f),
                            new GradientColorKey(new Color(0.417f, 0.001f, 0.658f), 0.25f),
                            new GradientColorKey(new Color(0.793f, 0.282f, 0.470f), 0.5f),
                            new GradientColorKey(new Color(0.973f, 0.584f, 0.254f), 0.75f),
                            new GradientColorKey(new Color(0.940f, 0.975f, 0.131f), 1.0f)
                        },
                        new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                    );
                    break;

                case ColorMapPreset.Inferno:
                    grad.SetKeys(
                        new GradientColorKey[]
                        {
                            new GradientColorKey(new Color(0.001f, 0.000f, 0.015f), 0.0f),
                            new GradientColorKey(new Color(0.341f, 0.062f, 0.429f), 0.3f),
                            new GradientColorKey(new Color(0.733f, 0.224f, 0.347f), 0.6f),
                            new GradientColorKey(new Color(0.988f, 0.652f, 0.211f), 0.85f),
                            new GradientColorKey(new Color(0.988f, 0.998f, 0.645f), 1.0f)
                        },
                        new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                    );
                    break;

                case ColorMapPreset.Magma:
                    grad.SetKeys(
                        new GradientColorKey[]
                        {
                            new GradientColorKey(new Color(0.001f, 0.000f, 0.015f), 0.0f),
                            new GradientColorKey(new Color(0.316f, 0.099f, 0.443f), 0.35f),
                            new GradientColorKey(new Color(0.716f, 0.215f, 0.475f), 0.65f),
                            new GradientColorKey(new Color(0.987f, 0.645f, 0.639f), 0.88f),
                            new GradientColorKey(new Color(0.987f, 0.991f, 0.750f), 1.0f)
                        },
                        new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                    );
                    break;

                case ColorMapPreset.Coolwarm:
                    grad.SetKeys(
                        new GradientColorKey[]
                        {
                            new GradientColorKey(new Color(0.230f, 0.299f, 0.754f), 0.0f),
                            new GradientColorKey(new Color(0.865f, 0.865f, 0.865f), 0.5f),
                            new GradientColorKey(new Color(0.706f, 0.016f, 0.150f), 1.0f)
                        },
                        new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                    );
                    break;

                case ColorMapPreset.Turbo:
                case ColorMapPreset.Jet:
                case ColorMapPreset.Rainbow:
                default:
                    grad.SetKeys(
                        new GradientColorKey[]
                        {
                            new GradientColorKey(new Color(0.190f, 0.072f, 0.232f), 0.0f),
                            new GradientColorKey(new Color(0.116f, 0.380f, 0.950f), 0.2f),
                            new GradientColorKey(new Color(0.134f, 0.865f, 0.548f), 0.45f),
                            new GradientColorKey(new Color(0.989f, 0.825f, 0.088f), 0.7f),
                            new GradientColorKey(new Color(0.900f, 0.140f, 0.140f), 1.0f)
                        },
                        new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                    );
                    break;
            }

            return grad;
        }

        #endregion
    }
}
