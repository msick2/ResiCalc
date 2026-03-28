using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace _260328_ResiCalc
{
    public class InductorItem
    {
        public string L       { get; set; } = "";
        public string Mode    { get; set; } = "";
        public string DeltaIL { get; set; } = "";
        public string ILpeak  { get; set; } = "";
        public string Note    { get; set; } = "";
    }

    public partial class BuckConverterUC : UserControl
    {
        public BuckConverterUC()
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

            if (!Helpers.TryParse(txtVout.Text,   out double vout)     || vout <= 0 || vout >= vin)
            { Helpers.ShowError("출력 전압은 0 V 보다 크고 입력 전압보다 작아야 합니다."); return; }

            if (!Helpers.TryParse(txtIout.Text,   out double iout)     || iout <= 0)
            { Helpers.ShowError("올바른 출력 전류를 입력하세요."); return; }

            if (!Helpers.TryParse(txtFreq.Text,   out double fKhz)     || fKhz <= 0)
            { Helpers.ShowError("올바른 스위칭 주파수를 입력하세요."); return; }

            if (!Helpers.TryParse(txtRipple.Text, out double ripplePct) || ripplePct <= 0 || ripplePct >= 100)
            { Helpers.ShowError("리플 비율은 0 ~ 100 % 사이 값을 입력하세요."); return; }

            double f = fKhz * 1000.0;
            double r = ripplePct / 100.0;

            // ── 핵심 계산 ────────────────────────────────────────────────
            double D      = vout / vin;
            double deltaIL = r * iout;
            double Lmin   = (vin - vout) * D / (f * deltaIL);
            double ILpeak = iout + deltaIL / 2.0;
            double ILrms  = Math.Sqrt(iout * iout + deltaIL * deltaIL / 12.0);
            double Iin    = D * iout;

            // ── 결과 표시 ────────────────────────────────────────────────
            txtRD.Text       = $"{D:F4}";
            txtRLmin.Text    = Helpers.FormatL(Lmin);
            txtRDeltaIL.Text = $"{deltaIL:F3} A";
            txtRILpeak.Text  = $"{ILpeak:F3} A";
            txtRILrms.Text   = $"{ILrms:F3} A";
            txtRIin.Text     = $"{Iin:F3} A";
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
                double dIL   = (vin - vout) * D / (f * Lstd);
                double ILpk2 = iout + dIL / 2.0;
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
