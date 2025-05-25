using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DP_GUI
{
    public static class MetricsLogger
    {
        // =====================================================================
        // Списки для зберігання виміряних значень
        // =====================================================================
        private static readonly List<double> _executionTimes = new();
        private static readonly List<double> _threadLoads = new();

        // =====================================================================
        // Очищення зібраних метрик
        // =====================================================================
        public static void ClearMetrics()
        {
            _executionTimes.Clear();
            _threadLoads.Clear();
        }

        // =====================================================================
        // Логування часу виконання однієї задачі (мс)
        // =====================================================================
        public static void LogExecutionTime(double timeMs) =>
            _executionTimes.Add(timeMs);

        // =====================================================================
        // Логування списку часів виконання
        // =====================================================================
        public static void LogExecutionTimes(IEnumerable<double> timesMs) =>
            _executionTimes.AddRange(timesMs);

        // =====================================================================
        // Логування завантаженості потоку (%)
        // =====================================================================
        public static void LogThreadLoad(double percent) =>
            _threadLoads.Add(percent);

        // =====================================================================
        // Логування списку завантаженостей
        // =====================================================================
        public static void LogThreadLoads(IEnumerable<double> loadsPercent) =>
            _threadLoads.AddRange(loadsPercent);

        // =====================================================================
        // Отримати копію списку часів виконання
        // =====================================================================
        public static List<double> GetExecutionTimes() =>
            _executionTimes.ToList();

        // =====================================================================
        // Отримати копію списку завантаженостей потоків
        // =====================================================================
        public static List<double> GetThreadLoads() =>
            _threadLoads.ToList();

        // =====================================================================
        // Збереження метрик у CSV-файл
        // - Колонки: TaskIndex,ExecutionTimeMs,ThreadLoadPercent
        // - Створює директорію, якщо потрібно
        // =====================================================================
        public static void SaveMetricsCsv(string filePath)
        {
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var w = new StreamWriter(filePath);
            w.WriteLine("TaskIndex,ExecutionTimeMs,ThreadLoadPercent");
            for (int i = 0; i < _executionTimes.Count; i++)
            {
                double t = _executionTimes[i];
                double l = i < _threadLoads.Count ? _threadLoads[i] : 0;
                w.WriteLine($"{i},{t},{l}");
            }
        }
    }
}
