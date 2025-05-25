using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScottPlot;       // новий API ScottPlot 5.x

namespace DP_GUI        // має співпадати з RootNamespace у вашому .csproj
{
    /// <summary>
    /// Модуль для побудови та збереження графіків результатів моделювання.
    /// Використовує ScottPlot 5.x.
    /// </summary>
    public static class ChartModule
    {
        // =====================================================================================
        // 1) Зберігає графік часу виконання задач у PNG-файл
        // =====================================================================================
        public static void SaveExecutionTimePlot(List<double> timesMs, string filePath)
        {
            var plt = new Plot();
            double[] ys = timesMs.ToArray();
            double[] xs = Enumerable.Range(0, ys.Length).Select(i => (double)i).ToArray();
            plt.Add.Scatter(xs, ys);
            plt.Title("Час виконання задач (мс)");
            plt.XLabel("Номер задачі");
            plt.YLabel("Час (ms)");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            plt.SavePng(filePath, 800, 600);
        }

        // =====================================================================================
        // 2) Зберігає графік завантаженості потоків у PNG-файл
        // =====================================================================================
        public static void SaveThreadLoadPlot(List<double> threadLoadsPercent, string filePath)
        {
            var plt = new Plot();
            double[] ys = threadLoadsPercent.ToArray();
            double[] xs = Enumerable.Range(0, ys.Length).Select(i => (double)i).ToArray();
            plt.Add.Bars(xs, ys);
            plt.Title("Завантаженість потоків (%)");
            plt.XLabel("Потоки");
            plt.YLabel("Завантаженість (%)");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            plt.SavePng(filePath, 800, 600);
        }
    }
}