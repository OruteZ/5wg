namespace FiveWG.Charting.EditorTools
{
    /// <summary>
    /// 제자리 radix-2 FFT. 스펙트럴 플럭스가 창마다 주파수 분포를 필요로 해서 있다.
    ///
    /// Unity에는 오프라인 FFT가 없다. <c>AudioSource.GetSpectrumData</c>는 재생 중에만 동작해서
    /// 채보 추출에 못 쓴다. 그래서 직접 둔다.
    ///
    /// 에너지 포락선 검출기는 이걸 쓰지 않는다 — 그게 그 방식이 싼 이유다.
    /// </summary>
    public static class Fft
    {
        /// <summary>
        /// 길이가 2의 거듭제곱인 복소 배열을 제자리 변환한다.
        /// 실수 신호를 넣을 때는 <paramref name="imaginary"/>를 0으로 채워 넣는다.
        /// </summary>
        public static void Transform(float[] real, float[] imaginary)
        {
            int n = real.Length;
            if (n <= 1) return;

            // 비트 반전 재배열. 이걸 먼저 해두면 이후 버터플라이가 제자리에서 끝난다.
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;

                if (i >= j) continue;

                (real[i], real[j]) = (real[j], real[i]);
                (imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
            }

            for (int len = 2; len <= n; len <<= 1)
            {
                double angle = -2.0 * System.Math.PI / len;
                float wStepReal = (float)System.Math.Cos(angle);
                float wStepImag = (float)System.Math.Sin(angle);

                for (int start = 0; start < n; start += len)
                {
                    float wReal = 1f, wImag = 0f;

                    for (int k = 0; k < len / 2; k++)
                    {
                        int a = start + k;
                        int b = a + len / 2;

                        float tReal = real[b] * wReal - imaginary[b] * wImag;
                        float tImag = real[b] * wImag + imaginary[b] * wReal;

                        real[b] = real[a] - tReal;
                        imaginary[b] = imaginary[a] - tImag;
                        real[a] += tReal;
                        imaginary[a] += tImag;

                        float next = wReal * wStepReal - wImag * wStepImag;
                        wImag = wReal * wStepImag + wImag * wStepReal;
                        wReal = next;
                    }
                }
            }
        }

        /// <summary>2의 거듭제곱인가. 창 크기 검사용.</summary>
        public static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;
    }
}
