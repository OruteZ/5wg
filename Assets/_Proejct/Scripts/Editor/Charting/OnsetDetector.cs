using System;
using System.Collections.Generic;
using UnityEngine;

namespace FiveWG.Charting.EditorTools
{
    public enum OnsetMethod
    {
        /// <summary>진폭이 갑자기 커지는 순간. FFT를 쓰지 않아 싸다.</summary>
        EnergyEnvelope,

        /// <summary>주파수 분포가 갑자기 바뀌는 순간. 크기가 안 변하는 레가토를 잡는다.</summary>
        SpectralFlux,
    }

    /// <summary>
    /// 두 검출기가 공유하는 설정. 원리 설명은 `.claude/docs/charting.md`에 있다.
    /// </summary>
    [Serializable]
    public struct OnsetSettings
    {
        /// <summary>분석 창 크기(샘플). 2의 거듭제곱이어야 한다.</summary>
        public int WindowSize;

        /// <summary>창을 얼마나 밀며 볼 것인가(샘플). 이 값이 시간 해상도를 정한다.</summary>
        public int HopSize;

        /// <summary>주변 중앙값의 몇 배를 넘어야 온셋으로 볼 것인가.</summary>
        public float ThresholdMultiplier;

        /// <summary>중앙값을 낼 이웃 프레임 수(한쪽).</summary>
        public int MedianRadius;

        /// <summary>이보다 가까운 검출은 하나로 뭉갠다(초).</summary>
        public double MinInterval;

        /// <summary>
        /// 전체 평균의 몇 배를 넘어야 하는가. 중앙값 임계값만으로는 <b>무음 구간에서 무너진다</b> —
        /// 주변이 전부 0이면 중앙값이 0이고, 0의 몇 배는 여전히 0이라 잡음 봉우리가 전부 통과한다.
        /// </summary>
        public float NoiseFloorFactor;

        public static OnsetSettings Default => new()
        {
            WindowSize = 1024,          // 44.1kHz에서 23ms
            HopSize = 256,              // 5.8ms — 16분음표(117ms)를 스무 조각으로 본다
            ThresholdMultiplier = 1.6f,
            MedianRadius = 12,
            MinInterval = 0.05,         // 16분음표보다 짧다. 한 타격의 중복 검출만 걷어낸다
            NoiseFloorFactor = 1.0f,
        };
    }

    /// <summary>
    /// stem에서 온셋(악기가 울린 순간)을 뽑는다.
    ///
    /// 두 방식을 다 두는 이유는 어느 쪽이 이길지 실제 stem을 재보기 전에는 모르기 때문이다.
    /// stem이 악기별로 갈려 있어서 싼 쪽(에너지)이 이길 가능성이 크지만, 어택이 흐린 악기에서는
    /// 진다. 같은 음원에 둘 다 돌려 스냅 오차를 나란히 놓고 고른다.
    /// </summary>
    public static class OnsetDetector
    {
        /// <summary>검출 결과. 초 단위 시각들이다 — 박으로 바꾸는 것은 호출자가 한다.</summary>
        public static List<double> Detect(float[] mono, int sampleRate, OnsetMethod method, in OnsetSettings settings)
        {
            if (mono is not { Length: > 0 } || sampleRate <= 0) return new List<double>();

            float[] detection = method == OnsetMethod.EnergyEnvelope
                ? EnergyDetectionFunction(mono, settings)
                : FluxDetectionFunction(mono, settings);

            return PickPeaks(detection, sampleRate, settings);
        }

        /// <summary>
        /// 창마다 에너지를 재고 <b>늘어난 만큼만</b> 남긴다. 줄어드는 구간은 0이다 —
        /// 소리가 잦아드는 것은 새 노트가 아니기 때문이다.
        /// </summary>
        private static float[] EnergyDetectionFunction(float[] mono, in OnsetSettings settings)
        {
            int hop = Mathf.Max(1, settings.HopSize);
            int window = Mathf.Max(hop, settings.WindowSize);
            int frames = Mathf.Max(1, (mono.Length - window) / hop + 1);

            var energy = new float[frames];
            for (int f = 0; f < frames; f++)
            {
                int start = f * hop;
                double sum = 0.0;
                for (int i = 0; i < window && start + i < mono.Length; i++)
                {
                    float s = mono[start + i];
                    sum += s * s;
                }

                // 제곱합이 아니라 RMS를 쓴다. 셈여림 차이가 제곱으로 벌어지면
                // 여린 구간의 타격이 threshold 아래로 깔려버린다.
                energy[f] = (float)Math.Sqrt(sum / window);
            }

            var detection = new float[frames];

            // 0번 프레임은 직전이 없다. 무음에서 올라온 것으로 본다 —
            // 0초가 비트 0이므로 곡 첫 박에 타격이 있는 것이 정상이고, 이걸 빼면 그걸 통째로 놓친다.
            detection[0] = energy[0];
            for (int f = 1; f < frames; f++) detection[f] = Mathf.Max(0f, energy[f] - energy[f - 1]);
            return detection;
        }

        /// <summary>
        /// 창마다 주파수 분포를 구해 직전 창과 비교하고, 각 대역에서 <b>늘어난 양만</b> 합산한다.
        /// 소리의 크기가 아니라 성분이 바뀐 정도를 본다.
        /// </summary>
        private static float[] FluxDetectionFunction(float[] mono, in OnsetSettings settings)
        {
            int hop = Mathf.Max(1, settings.HopSize);
            int window = settings.WindowSize;
            if (!Fft.IsPowerOfTwo(window))
            {
                Debug.LogError($"[{nameof(OnsetDetector)}] 창 크기 {window}는 2의 거듭제곱이 아니다. FFT를 돌릴 수 없다.");
                return Array.Empty<float>();
            }

            int frames = Mathf.Max(1, (mono.Length - window) / hop + 1);
            int bins = window / 2;

            // 해닝 창. 안 씌우면 창 경계의 불연속이 전 대역에 퍼져 없던 플럭스가 생긴다.
            var hann = new float[window];
            for (int i = 0; i < window; i++) hann[i] = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (window - 1)));

            var detection = new float[frames];
            var previous = new float[bins];
            var real = new float[window];
            var imaginary = new float[window];

            for (int f = 0; f < frames; f++)
            {
                int start = f * hop;
                for (int i = 0; i < window; i++)
                {
                    real[i] = start + i < mono.Length ? mono[start + i] * hann[i] : 0f;
                    imaginary[i] = 0f;
                }

                Fft.Transform(real, imaginary);

                float flux = 0f;
                for (int k = 0; k < bins; k++)
                {
                    float magnitude = Mathf.Sqrt(real[k] * real[k] + imaginary[k] * imaginary[k]);
                    flux += Mathf.Max(0f, magnitude - previous[k]);
                    previous[k] = magnitude;
                }

                // 0번 프레임에서는 previous가 전부 0이라 flux가 곧 "무음에서 올라온 양"이 된다.
                // 에너지 쪽과 같은 이유로 그대로 쓴다.
                detection[f] = flux;
            }

            return detection;
        }

        /// <summary>
        /// 봉우리 고르기. 두 방식이 같은 후처리를 쓴다.
        ///
        /// 임계값을 절대값이 아니라 <b>주변 중앙값의 배수</b>로 잡는다. 곡은 셈여림이 변하는데
        /// 절대값으로 자르면 여린 구간이 통째로 사라지거나 센 구간이 온통 검출된다.
        /// 평균이 아니라 중앙값인 이유는, 봉우리 자신이 평균을 끌어올려 자기 임계값을 높여버리기 때문이다.
        /// </summary>
        private static List<double> PickPeaks(float[] detection, int sampleRate, in OnsetSettings settings)
        {
            var result = new List<double>();
            if (detection.Length == 0) return result;

            int radius = Mathf.Max(1, settings.MedianRadius);
            double secondsPerFrame = settings.HopSize / (double)sampleRate;
            var neighbourhood = new List<float>(radius * 2 + 1);
            double lastAccepted = double.NegativeInfinity;

            double total = 0.0;
            for (int i = 0; i < detection.Length; i++) total += detection[i];
            float floor = (float)(total / detection.Length) * Mathf.Max(0f, settings.NoiseFloorFactor);

            // 창의 시작이 아니라 중심을 그 프레임의 시각으로 본다.
            // 프레임 f는 [f*hop, f*hop+window)를 보므로, 시작으로 잡으면 검출이 창 길이만큼 이르게 나온다.
            // 실제로 클릭 트랙에서 에너지 -18.9ms / 플럭스 -13.5ms로 쏠렸다. 중심이 MIR의 관례이기도 하다.
            double centerOffset = settings.WindowSize * 0.5 / sampleRate;

            for (int f = 0; f < detection.Length - 1; f++)
            {
                // 국소 최대가 아니면 같은 타격의 어깨다. 0번은 직전이 없으므로 뒤만 본다.
                if (f > 0 && detection[f] < detection[f - 1]) continue;
                if (detection[f] < detection[f + 1]) continue;
                if (detection[f] <= 0f) continue;

                neighbourhood.Clear();
                int from = Mathf.Max(0, f - radius);
                int to = Mathf.Min(detection.Length - 1, f + radius);
                for (int i = from; i <= to; i++) neighbourhood.Add(detection[i]);
                neighbourhood.Sort();

                float median = neighbourhood[neighbourhood.Count / 2];
                if (detection[f] < Mathf.Max(median * settings.ThresholdMultiplier, floor)) continue;

                double time = f * secondsPerFrame + centerOffset;
                if (time - lastAccepted < settings.MinInterval) continue;

                result.Add(time);
                lastAccepted = time;
            }

            return result;
        }
    }
}
