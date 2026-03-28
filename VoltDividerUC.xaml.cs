using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace _260328_ResiCalc
{
    public class VdResult
    {
        public int    Rank       { get; set; }
        public string R1         { get; set; } = "";
        public string R2         { get; set; } = "";
        public string ActualVout { get; set; } = "";
        public string Error      { get; set; } = "";
        public string TotalR     { get; set; } = "";
    }

    public partial class VoltDividerUC : UserControl
    {
        public VoltDividerUC()
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
            if (!Helpers.TryParse(txtVin.Text, out double vin) || vin <= 0)
            {
                Helpers.ShowError("올바른 입력 전압을 입력하세요."); return;
            }
            if (!Helpers.TryParse(txtVout.Text, out double vout) || vout <= 0 || vout >= vin)
            {
                Helpers.ShowError("출력 전압은 0 V 보다 크고 입력 전압보다 작아야 합니다."); return;
            }

            double[] baseValues = rbE24.IsChecked  == true ? Helpers.E24Base  :
                                  rbE96.IsChecked  == true ? Helpers.E96Base  : Helpers.E192Base;
            double[] series = Helpers.ExpandDecades(baseValues, 1, 6); // 10Ω ~ 10MΩ

            double ratio = vout / vin;
            double r2r1  = ratio / (1.0 - ratio);

            lblRatio.Text  = $"비율  {ratio:F4}";
            lblFormula.Text = $"Vout / Vin = {ratio:F5}   |   R2 / R1 = {r2r1:F5}";

            var seen    = new HashSet<string>();
            var results = new List<(double r1, double r2, double av, double err)>();

            foreach (double r1 in series)
            {
                double r2 = Helpers.FindNearest(series, r1 * r2r1);
                if (!seen.Add($"{r1}|{r2}")) continue;

                double av  = vin * r2 / (r1 + r2);
                double err = (av - vout) / vout * 100.0;
                results.Add((r1, r2, av, err));
            }

            var top = results
                .OrderBy(r => System.Math.Abs(r.err))
                .Take(15)
                .Select((r, i) => new VdResult
                {
                    Rank       = i + 1,
                    R1         = Helpers.FormatR(r.r1),
                    R2         = Helpers.FormatR(r.r2),
                    ActualVout = $"{r.av:F4} V",
                    Error      = $"{r.err:+0.000;-0.000;0.000}%",
                    TotalR     = Helpers.FormatR(r.r1 + r.r2)
                }).ToList();

            dgResult.ItemsSource = top;
            lblCount.Text        = $"(오차 작은 순 {top.Count}개)";
        }
    }
}
