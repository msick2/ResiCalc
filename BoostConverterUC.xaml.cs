using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace _260328_ResiCalc
{
    public partial class BoostConverterUC : UserControl
    {
        public BoostConverterUC()
        {
            InitializeComponent();
        }

        private void Calc_Click(object sender, RoutedEventArgs e) => Calculate();
        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Calculate();
        }

        private void Calculate()
        {
            if (!Helpers.TryParse(txtVin.Text,    out double vin)      || vin <= 0)
            { Helpers.ShowError("올바른 입력 전압을 입력하세요."); return; }

            if (!Helpers.TryParse(txtVout.Text,   out double vout)     || vout <= vin)
            { Helpers.ShowError("출력 전압은 입력 전압보다 커야 합니다 (Vout > Vin)."); return; }

            if (!Helpers.TryParse(txtIout.Text,   out double iout)     || iout <= 0)
            { Helpers.ShowError("올바른 출력 전류를 입력하세요."); return; }

            if (!Helpers.TryParse(txtFreq.Text,   out double fKhz)     || fKhz <= 0)
            { Helpers.ShowError("올바른 스위칭 주파수를 입력하세요."); return; }

            if (!Helpers.TryParse(txtRipple.Text, out double ripplePct) || ripplePct <= 0 || ripplePct >= 100)
            { Helpers.ShowError("리플 비율은 0 ~ 100 % 사이 값을 입력하세요."); return; }

            double f = fKhz * 1000.0;
            double r = ripplePct / 100.0;

            // ── 핵심 계산 ────────────────────────────────────────────────
            // Boost CCM:
            //   D    = 1 - Vin/Vout
            //   Iin  = Iout * Vout/Vin  (이상적, 100% 효율 가정)
            //   ΔIL  = r * Iin
            //   Lmin = Vin * D / (f * ΔIL)

            double D      = 1.0 - vin / vout;
            double Iin    = iout * vout / vin;          // 평균 입력 전류 = 평균 인덕터 전류
            double deltaIL = r * Iin;                   // 인덕터 리플 전류
            double Lmin   = vin * D / (f * deltaIL);   // 최소 인덕턴스
            double ILpeak = Iin + deltaIL / 2.0;        // 피크 전류 (스위치/다이오드 정격 기준)
            double ILrms  = Math.Sqrt(Iin * Iin + deltaIL * deltaIL / 12.0); // RMS 전류
            double Pout   = vout * iout;                // 출력 전력

            // ── 결과 표시 ────────────────────────────────────────────────
            txtRD.Text       = $"{D:F4}";
            txtRLmin.Text    = Helpers.FormatL(Lmin);
            txtRIin.Text     = $"{Iin:F3} A";
            txtRDeltaIL.Text = $"{deltaIL:F3} A";
            txtRILpeak.Text  = $"{ILpeak:F3} A";
            txtRILrms.Text   = $"{ILrms:F3} A";
            txtRPout.Text    = $"{Pout:F2} W";
            lblLmin.Text     = $"Lmin = {Helpers.FormatL(Lmin)}";

            // ── E12 표준 인덕터 추천 ──────────────────────────────────────
            double[] e12Series = Helpers.ExpandDecades(Helpers.E12Base, -9, -2); // 1nH ~ 100mH
            double[] sorted    = e12Series.OrderBy(v => v).ToArray();

            int idx   = Array.FindLastIndex(sorted, v => v < Lmin);
            int start = Math.Max(0, idx - 1);
            int end   = Math.Min(sorted.Length - 1, idx + 4);

            double firstCcm = sorted.FirstOrDefault(v => v >= Lmin);

            var rows = new List<InductorItem>();
            for (int i = start; i <= end; i++)
            {
                double Lstd  = sorted[i];
                // 해당 L에서의 실제 리플 및 피크 전류 재계산
                double dIL   = vin * D / (f * Lstd);
                double ILpk2 = Iin + dIL / 2.0;
                bool   ccm   = Lstd >= Lmin;

                rows.Add(new InductorItem
                {
                    L       = Helpers.FormatL(Lstd),
                    Mode    = ccm ? "CCM" : "DCM",
                    DeltaIL = $"{dIL:F3} A",
                    ILpeak  = $"{ILpk2:F3} A",
                    Note    = (ccm && Lstd == firstCcm) ? "★ 최소 추천" : ""
                });
            }

            dgInductor.ItemsSource = rows;
        }
    }
}
