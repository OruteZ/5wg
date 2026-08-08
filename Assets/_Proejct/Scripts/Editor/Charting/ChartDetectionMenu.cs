using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FiveWG.Charting.EditorTools
{
    /// <summary>
    /// 고른 <see cref="ChartAsset"/>의 stem에서 온셋을 뽑아 노트로 넣고 리포트를 찍는다.
    ///
    /// 전용 창을 만들지 않는다(→ `.claude/docs/charting.md`). 검출이 잘 맞으면 손댈 일이 거의
    /// 없어서 파형 에디터는 헛수고가 되고, 정확도를 먼저 재보는 것이 정해진 순서다.
    /// Alchemy가 인스펙터를 그리고 있어서 커스텀 에디터로 버튼을 얹으면 그쪽과 부딪힌다.
    /// </summary>
    public static class ChartDetectionMenu
    {
        private const string EnergyPath = "Tools/Charting/온셋 검출 — 에너지 포락선";
        private const string FluxPath = "Tools/Charting/온셋 검출 — 스펙트럴 플럭스";
        private const string ComparePath = "Tools/Charting/두 방식 비교 (노트는 그대로)";

        [MenuItem(EnergyPath)]
        private static void DetectEnergy() => Run(OnsetMethod.EnergyEnvelope, writeNotes: true);

        [MenuItem(FluxPath)]
        private static void DetectFlux() => Run(OnsetMethod.SpectralFlux, writeNotes: true);

        [MenuItem(ComparePath)]
        private static void Compare() => Run(null, writeNotes: false);

        [MenuItem(EnergyPath, true)]
        [MenuItem(FluxPath, true)]
        [MenuItem(ComparePath, true)]
        private static bool Validate() => Selection.activeObject is ChartAsset;

        private static void Run(OnsetMethod? method, bool writeNotes)
        {
            if (Selection.activeObject is not ChartAsset chart)
            {
                Debug.LogError($"[{nameof(ChartDetectionMenu)}] 프로젝트 창에서 {nameof(ChartAsset)}을 골라야 한다.");
                return;
            }

            ChartTrack[] tracks = chart.Tracks;
            if (tracks is not { Length: > 0 })
            {
                Debug.LogWarning($"[{nameof(ChartDetectionMenu)}] '{chart.name}'에 트랙이 없다.", chart);
                return;
            }

            var settings = OnsetSettings.Default;
            var report = new StringBuilder();
            report.Append("[채보 검출] ").Append(chart.name)
                .Append("  BPM ").Append(chart.Bpm.ToString("0.###"))
                .Append("  서브박 ").Append((chart.SecondsPerSub * 1000.0).ToString("F1")).AppendLine("ms");

            for (int i = 0; i < tracks.Length; i++)
            {
                ChartTrack track = tracks[i];
                if (track.Stem == null)
                {
                    report.Append("  ").Append(track.Instrument).AppendLine(" : stem이 없다 (커밋되지 않으므로 정상일 수 있다)");
                    continue;
                }

                if (!TryReadMono(track.Stem, out float[] mono, out int rate))
                {
                    report.Append("  ").Append(track.Instrument).AppendLine(" : 샘플을 읽지 못했다");
                    continue;
                }

                report.Append("  ").Append(track.Instrument).Append(" (").Append(track.Stem.name).AppendLine(")");

                int[] truth = track.Notes;   // 이미 노트가 있으면 정답으로 놓고 비교한다

                foreach (OnsetMethod candidate in MethodsToRun(method))
                {
                    List<double> onsets = OnsetDetector.Detect(mono, rate, candidate, settings);
                    int[] snapped = Snap(onsets, chart, out double meanError, out double maxError, out int beyondHalf);

                    report.Append("    ").Append(Label(candidate)).Append(" : 검출 ").Append(onsets.Count)
                        .Append("개 → 노트 ").Append(snapped.Length).Append("개")
                        .Append(" | 스냅 오차 평균 ").Append(meanError.ToString("F1")).Append("ms")
                        .Append(" 최대 ").Append(maxError.ToString("F1")).Append("ms")
                        .Append(" | 반칸 밖 ").Append(beyondHalf).Append("개");

                    if (truth is { Length: > 0 })
                    {
                        CompareToTruth(snapped, truth, out int hit, out int missed, out int extra);
                        report.Append(" || 정답 대비 맞음 ").Append(hit).Append('/').Append(truth.Length)
                            .Append(" 놓침 ").Append(missed).Append(" 헛검출 ").Append(extra);
                    }

                    report.AppendLine();

                    if (!writeNotes) continue;

                    Undo.RecordObject(chart, "Detect Onsets");
                    chart.SetNotes(i, snapped);
                    EditorUtility.SetDirty(chart);
                }
            }

            if (writeNotes) AssetDatabase.SaveAssets();
            Debug.Log(report.ToString(), chart);
        }

        private static IEnumerable<OnsetMethod> MethodsToRun(OnsetMethod? method)
        {
            if (method.HasValue)
            {
                yield return method.Value;
                yield break;
            }

            yield return OnsetMethod.EnergyEnvelope;
            yield return OnsetMethod.SpectralFlux;
        }

        private static string Label(OnsetMethod method) =>
            method == OnsetMethod.EnergyEnvelope ? "에너지  " : "플럭스  ";

        /// <summary>
        /// 검출 시각을 서브박 격자에 붙인다. 오차는 <b>붙이기 전에</b> 재야 의미가 있다 —
        /// 붙인 뒤에는 전부 0이다.
        /// </summary>
        private static int[] Snap(List<double> onsets, ChartAsset chart,
            out double meanErrorMs, out double maxErrorMs, out int beyondHalfSub)
        {
            meanErrorMs = 0.0;
            maxErrorMs = 0.0;
            beyondHalfSub = 0;

            if (onsets.Count == 0) return Array.Empty<int>();

            double perSub = chart.SecondsPerSub;
            double halfSub = perSub * 0.5;
            var notes = new List<int>(onsets.Count);
            double total = 0.0;

            foreach (double time in onsets)
            {
                int index = (int)Math.Round(time / perSub);
                if (index < 0) index = 0;

                double error = Math.Abs(time - index * perSub);
                total += error;
                if (error > maxErrorMs / 1000.0) maxErrorMs = error * 1000.0;

                // 반칸을 넘게 밀렸다는 건 이 격자에 안 맞는 연주이거나 BPM이 틀렸다는 뜻이다.
                if (error > halfSub * 0.999) beyondHalfSub++;

                if (notes.Count == 0 || notes[^1] != index) notes.Add(index);
            }

            meanErrorMs = total / onsets.Count * 1000.0;
            return notes.ToArray();
        }

        /// <summary>이미 들어 있던 노트를 정답으로 놓고 맞은 수를 센다. 클릭 트랙처럼 답을 아는 경우에 쓴다.</summary>
        private static void CompareToTruth(int[] detected, int[] truth, out int hit, out int missed, out int extra)
        {
            var truthSet = new HashSet<int>(truth);
            var detectedSet = new HashSet<int>(detected);

            hit = 0;
            foreach (int note in truthSet)
            {
                if (detectedSet.Contains(note)) hit++;
            }

            missed = truthSet.Count - hit;
            extra = detectedSet.Count - hit;
        }

        /// <summary>
        /// 클립에서 모노 샘플을 꺼낸다. 임포트 설정이 <c>preloadAudioData=false</c>면
        /// 데이터가 안 올라와 있어서 먼저 불러야 한다 — 안 그러면 조용히 0만 읽힌다.
        /// </summary>
        private static bool TryReadMono(AudioClip clip, out float[] mono, out int sampleRate)
        {
            mono = Array.Empty<float>();
            sampleRate = clip.frequency;

            if (!clip.preloadAudioData && !clip.LoadAudioData())
            {
                Debug.LogError($"[{nameof(ChartDetectionMenu)}] '{clip.name}'의 오디오 데이터를 불러오지 못했다.", clip);
                return false;
            }

            var interleaved = new float[clip.samples * clip.channels];
            if (!clip.GetData(interleaved, 0))
            {
                Debug.LogError(
                    $"[{nameof(ChartDetectionMenu)}] '{clip.name}'에서 샘플을 읽지 못했다. " +
                    "임포트 설정의 Load Type을 Decompress On Load로 두면 된다.", clip);
                return false;
            }

            if (clip.channels == 1)
            {
                mono = interleaved;
                return true;
            }

            mono = new float[clip.samples];
            for (int i = 0; i < clip.samples; i++)
            {
                float sum = 0f;
                for (int c = 0; c < clip.channels; c++) sum += interleaved[i * clip.channels + c];
                mono[i] = sum / clip.channels;
            }

            return true;
        }
    }
}
