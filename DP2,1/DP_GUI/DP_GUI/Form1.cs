// File: Form1.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScottPlot.WinForms;

namespace DP_GUI
{
    public partial class Form1 : Form
    {
        // ключ → делегат запуску
        private readonly Dictionary<string, Func<Task>> _scenarios = new();

        // прогрес-бар і таймер опитування
        private readonly ProgressBar _progress = new ProgressBar();
        private readonly Timer _poller = new Timer { Interval = 200 };
        private int _expectedTotal;
        private readonly Stopwatch _sw = new Stopwatch();

        public Form1()
        {
            InitializeComponent();
            SetupGrid();
            InitScenarios();

            // мінімальні розміри і роздільники
            splitMain.Panel1MinSize = 150;
            splitMain.Panel2MinSize = 150;
            splitMain.SplitterDistance = 300;
            splitBottom.Panel1MinSize = 150;
            splitBottom.Panel2MinSize = 150;
            splitBottom.SplitterDistance = splitBottom.Width / 2;

            // прогрес-бар у верхню панель
            _progress.Width = 150;
            _progress.Minimum = 0;
            _progress.Maximum = 100;
            flowTop.Controls.Add(_progress);

            // лейбл для загального часу
            lblTime.AutoSize = true;
            flowTop.Controls.Add(lblTime);

            // початковий показ кількості логічних процесорів
            labelInfo.Text = $"Logical processors: {Environment.ProcessorCount}";

            // підписка на події
            _poller.Tick += Poller_Tick;
            gridMetrics.CellClick += GridMetrics_CellClick;
        }

        // ────────────────────────────────────────────────────────────────────────────────
        //  Налаштування таблиці результатів
        // ────────────────────────────────────────────────────────────────────────────────
        private readonly Label lblTime = new Label { Text = "Час: ‒" };

        private void SetupGrid()
        {
            gridMetrics.Columns.Clear();
            gridMetrics.Columns.Add("TaskIndex", "№");
            gridMetrics.Columns.Add("ExecutionTimeMs", "Час (мс)");
            gridMetrics.Columns.Add("ThreadLoadPercent", "CPU (%)");
            gridMetrics.Columns.Add("Result", "Результат");
            gridMetrics.AllowUserToAddRows = false;
            gridMetrics.RowHeadersVisible = false;
            gridMetrics.ReadOnly = true;
            gridMetrics.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridMetrics.MultiSelect = false;
        }

        // ────────────────────────────────────────────────────────────────────────────────
        //  Ініціалізація доступних сценаріїв
        // ────────────────────────────────────────────────────────────────────────────────
        private void InitScenarios()
        {
            _scenarios.Clear();
            _scenarios.Add("Sync", () => Task.Run(() => WorkRunner.RunSynchronous((int)numericSize.Value)));
            _scenarios.Add("Async", () => WorkRunner.RunAsynchronous((int)numericSize.Value));
            _scenarios.Add("Par", () => WorkRunner.RunAsynchronousParallel((int)numericSize.Value));
            _scenarios.Add("Read", () => WorkRunner.RunFileReadParallelAsync((int)numericIter.Value));
            _scenarios.Add("Read+Prog", () => WorkRunner.RunFileReadWithProgressParallelAsync((int)numericIter.Value));
            _scenarios.Add("Bitcoin", () => WorkRunner.RunBitcoinParallelAsync((int)numericIter.Value));
            _scenarios.Add("Primes", () => Task.Run(() => WorkRunner.RunCPUBound((int)numericSize.Value)));
            _scenarios.Add("PrimesPar", () => WorkRunner.RunCPUBoundParallel((int)numericSize.Value));

            comboScenarios.Items.Clear();
            comboScenarios.Items.AddRange(_scenarios.Keys.ToArray());
            comboScenarios.SelectedIndex = 0;
        }

        // ────────────────────────────────────────────────────────────────────────────────
        //  Обробник кнопки «Виконати»
        // ────────────────────────────────────────────────────────────────────────────────
        private async void btnRun_Click(object sender, EventArgs e)
        {
            btnRun.Enabled = false;
            _progress.Value = 0;
            lblTime.Text = "Час: …";

            WorkRunner.TaskCount = (int)numericIter.Value;
            WorkRunner.ClearMetrics();
            

            string key = comboScenarios.SelectedItem as string ?? "";
            if (!_scenarios.TryGetValue(key, out var run))
            {
                MessageBox.Show("Оберіть сценарій", "Увага",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnRun.Enabled = true;
                return;
            }

            // оцінюємо кількість під‐задач для прогрес‐бару
            _expectedTotal = EstimateTotalSubtasks(key);
            _progress.Maximum = _expectedTotal > 0 ? _expectedTotal : 1;

            _sw.Restart();
            _poller.Start();

            await run();  // запускаємо обраний сценарій

            _poller.Stop();
            _sw.Stop();
            _progress.Value = _progress.Maximum;
            lblTime.Text = $"Час: {_sw.Elapsed.TotalMilliseconds:F0} ms";

            ShowResults(key);
            FillTxtOutputDefault(key);
            btnRun.Enabled = true;
        }

        private int EstimateTotalSubtasks(string key)
        {
            int iter = (int)numericIter.Value;
            return key switch
            {
                "Read" or "Read+Prog" =>
                    iter * Directory.GetFiles(WorkRunner.ReadFolder, "*.txt").Length,
                "Sync" or "Async" or "Par" or "Bitcoin" or
                "Primes" or "PrimesPar" => iter,
                _ => 1
            };
        }

        // ────────────────────────────────────────────────────────────────────────────────
        //  Таймер‐опитувач прогресу
        // ────────────────────────────────────────────────────────────────────────────────
        private void Poller_Tick(object? sender, EventArgs e)
        {
            int done = Math.Min(WorkRunner.GetResults().Count, _progress.Maximum);
            _progress.Value = done;
        }

        // ────────────────────────────────────────────────────────────────────────────────
        //  Відображення таблиці + графіка
        // ────────────────────────────────────────────────────────────────────────────────
        private void ShowResults(string key)
        {
            // 1) Таблиця
            gridMetrics.Rows.Clear();
            var times = WorkRunner.GetExecutionTimes();
            var loads = WorkRunner.GetThreadLoads();
            var results = WorkRunner.GetResults();
            int rows = Math.Max(Math.Max(times.Count, loads.Count), results.Count);

            for (int i = 0; i < rows; i++)
            {
                string t = i < times.Count ? times[i].ToString("F2") : "";
                string l = i < loads.Count ? loads[i].ToString("F1") : "";
                string r = i < results.Count ? results[i] : "";
                gridMetrics.Rows.Add(i + 1, t, l, r);
            }

            // 2) Графік
            var plt = formsPlot1.Plot;
            plt.Clear();

            if (key == "Bitcoin")
            {
                // малюємо зміни курсу USD
                var priceList = WorkRunner.GetPrices();
                if (priceList.Any())
                {
                    double[] xs = priceList.Select((_, i) => i + 1d).ToArray();
                    double[] ys = priceList.Select(p => (double)p).ToArray();
                    var sc = plt.Add.Scatter(xs, ys);
                    sc.MarkerSize = 4;
                }
                plt.YLabel("USD");
            }
            else
            {
                // малюємо час виконання
                if (times.Any())
                {
                    double[] xs = times.Select((_, i) => i + 1d).ToArray();
                    double[] ys = times.ToArray();
                    plt.Add.Scatter(xs, ys);
                }
                plt.YLabel("Час (ms)");
            }

            plt.Title($"Scenario: {key}");
            plt.XLabel("Ітерація");
            formsPlot1.Refresh();

            // 3) Обчислюємо реальну кількість логічних потоків, які були задіяні
            int poolSize = Environment.ProcessorCount;
            int peakUsed = loads.Any()
                ? (int)Math.Ceiling(loads.Max() * poolSize / 100.0)
                : 0;
            labelInfo.Text = $"Потоків задіяно (макс): {peakUsed} / {poolSize}";
        }

        // ────────────────────────────────────────────────────────────────────────────────
        //  txtOutput для Read/HTTP/Bitcoin
        // ────────────────────────────────────────────────────────────────────────────────
        private async void FillTxtOutputDefault(string key)
        {
            switch (key)
            {
                case "Bitcoin":
                    // ... (залишаємо  код для середніх курсів)
                    break;

                case "Read":
                case "Read+Prog":
                    txtOutput.Text = "Клікніть рядок, щоб побачити текст файла.";
                    break;

                default:
                    txtOutput.Clear();
                    break;
            }
        }

        // ────────────────────────────────────────────────────────────────────────────────
        //  Клік по таблиці → показуємо вміст файлу
        // ────────────────────────────────────────────────────────────────────────────────
        private void GridMetrics_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string key = comboScenarios.SelectedItem as string ?? "";
            if (key != "Read" && key != "Read+Prog") return;

            var results = WorkRunner.GetResults();
            if (e.RowIndex >= results.Count) return;

            string row = results[e.RowIndex];
            string fileName = row.Split(':')[0].Trim();
            string path = Path.Combine(WorkRunner.ReadFolder, fileName);
            if (File.Exists(path))
                txtOutput.Text = File.ReadAllText(path);
        }

        // ────────────────────────────────────────────────────────────────────────────────
        //  Експорт CSV + PNG
        // ────────────────────────────────────────────────────────────────────────────────
        private async void btnExport_Click(object sender, EventArgs e)
        {
            btnExport.Enabled = false;
            WorkRunner.TaskCount = (int)numericIter.Value;

            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results");
            Directory.CreateDirectory(baseDir);

            foreach (var kv in _scenarios)
            {
                WorkRunner.ClearMetrics();
                await kv.Value();

                var times = WorkRunner.GetExecutionTimes();
                var loads = WorkRunner.GetThreadLoads();
                var results = WorkRunner.GetResults();

                string dir = Path.Combine(baseDir, kv.Key);
                Directory.CreateDirectory(dir);

                using var w = new StreamWriter(Path.Combine(dir, $"{kv.Key}.csv"));
                w.WriteLine("TaskIndex,TimeMs,Load%,Result");
                int rows = Math.Max(Math.Max(times.Count, loads.Count), results.Count);
                for (int i = 0; i < rows; i++)
                {
                    string t = i < times.Count ? times[i].ToString("F2") : "";
                    string l = i < loads.Count ? loads[i].ToString("F1") : "";
                    string r = i < results.Count ? results[i] : "";
                    w.WriteLine($"{i + 1},{t},{l},{r}");
                }

                ChartModule.SaveExecutionTimePlot(times, Path.Combine(dir, $"{kv.Key}_times.png"));
                ChartModule.SaveThreadLoadPlot(loads, Path.Combine(dir, $"{kv.Key}_loads.png"));
            }

            MessageBox.Show("Готово! Результати у 'results'.",
                            "Експорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnExport.Enabled = true;
        }
    }
}
