// File: WorkRunner.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static DP_GUI.TaskSimulator;

namespace DP_GUI
{
    public static class WorkRunner
    {
        // Кількість ітерацій або кількість паралельних задач
        public static int TaskCount { get; set; } = 5;

        // Папка з файлами для I/O сценаріїв
        public const string ReadFolder = @"D:\University4\DP\DP2,1\DP_GUI\DP_GUI\Fileread";

        // Розмір пулу потоків
        private static readonly int _poolSize;
        public static int PoolSize => _poolSize;

        // Метрики: час, навантаження, результати
        private static readonly List<double> _times = new();
        private static readonly List<double> _loads = new();
        private static readonly List<string> _results = new();

        // список отриманих курсів BTC/USD
        private static readonly List<decimal> _prices = new();

        public static List<decimal> GetPrices() => _prices.ToList();

        // Лок для безпечного доступу з кількох потоків
        private static readonly object _lock = new();
        public static object Lock => _lock;

        static WorkRunner()
        {
            ThreadPool.GetMinThreads(out _, out int ioThreads);
            int cpuCount = Environment.ProcessorCount;
            ThreadPool.SetMinThreads(cpuCount, ioThreads);
            ThreadPool.SetMaxThreads(cpuCount, ioThreads);
            _poolSize = cpuCount;
        }

        /// <summary>Очищення усіх метрик та результатів</summary>
        public static void ClearMetrics()
        {
            lock (_lock)
            {
                _times.Clear();
                _loads.Clear();
                _results.Clear();
                _prices.Clear();
            }
        }

        /// <summary>Отримати список часів виконання</summary>
        public static List<double> GetExecutionTimes() => _times.ToList();

        /// <summary>Отримати список навантажень потоків</summary>
        public static List<double> GetThreadLoads() => _loads.ToList();

        /// <summary>Отримати список результатів</summary>
        public static List<string> GetResults() => _results.ToList();

        // ======== допоміжні методи для безпечного додавання =========
        public static void AddTime(double t) { lock (_lock) _times.Add(t); }
        public static void AddLoad(double l) { lock (_lock) _loads.Add(l); }
        public static void AddResult(string r) { lock (_lock) _results.Add(r); }

        // =====================================================================================
        // 1) CPU-bound синхронний
        //    обчислення факторіалів та збір BigInteger-результату
        // =====================================================================================
        public static void RunSynchronous(int workSize)
        {
            ClearMetrics();
            for (int i = 0; i < TaskCount; i++)
            {
                var sw = Stopwatch.StartNew();
                TaskSimulator.ComputeFactorials(workSize);
                sw.Stop();

                BigInteger fact = TaskSimulator.ComputeFactorialBig(workSize);

                ThreadPool.GetAvailableThreads(out int avail, out _);
                double load = (_poolSize - avail) * 100.0 / _poolSize;

                AddTime(sw.Elapsed.TotalMilliseconds);
                AddLoad(load);
                AddResult(fact.ToString());
            }
        }

        // =====================================================================================
        // 2) CPU-bound асинхронний
        //    async/await факторіали + BigInteger-результат
        // =====================================================================================
        public static async Task RunAsynchronous(int workSize)
        {
            ClearMetrics();
            for (int i = 0; i < TaskCount; i++)
            {
                var sw = Stopwatch.StartNew();
                await TaskSimulator.ComputeFactorialsAsync(workSize);
                sw.Stop();

                BigInteger fact = TaskSimulator.ComputeFactorialBig(workSize);

                ThreadPool.GetAvailableThreads(out int avail, out _);
                double load = (_poolSize - avail) * 100.0 / _poolSize;

                AddTime(sw.Elapsed.TotalMilliseconds);
                AddLoad(load);
                AddResult(fact.ToString());
            }
        }

        // =====================================================================================
        // 3) CPU-bound паралельний асинхронний
        //    Task.Run для кожного ітератора + BigInteger
        // =====================================================================================
        public static async Task RunAsynchronousParallel(int workSize)
        {
            ClearMetrics();
            var tasks = new List<Task>();

            for (int i = 0; i < TaskCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    // ── завантаження ДО старту
                    ThreadPool.GetAvailableThreads(out int before, out _);

                    var sw = Stopwatch.StartNew();
                    await TaskSimulator.ComputeFactorialsAsync(workSize);
                    sw.Stop();

                    // ── завантаження ПІСЛЯ завершення
                    ThreadPool.GetAvailableThreads(out int after, out _);

                    double load = ((_poolSize - before) + (_poolSize - after)) / 2.0
                                  * 100.0 / _poolSize;

                    BigInteger fact = TaskSimulator.ComputeFactorialBig(workSize);

                    // колекції → під замком
                    lock (_lock)
                    {
                        _times.Add(sw.Elapsed.TotalMilliseconds);
                        _loads.Add(load);
                        _results.Add(fact.ToString());
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        // =====================================================================================
        // 4) I/O-bound: паралельне читання всіх .txt у ReadFolder
        // =====================================================================================
        public static async Task RunFileReadParallelAsync(int calls)
        {
            ClearMetrics();
            var files = Directory.GetFiles(ReadFolder, "*.txt");
            var tasks = new List<Task>();

            for (int i = 0; i < calls; i++)
            {
                foreach (var file in files)
                {
                    ThreadPool.GetAvailableThreads(out int before, out _);
                    double loadBefore = (_poolSize - before) * 100.0 / _poolSize;

                    tasks.Add(Task.Run(async () =>
                    {
                        var sw = Stopwatch.StartNew();
                        string content = await TaskSimulator.ReadFileAsync(file);
                        sw.Stop();

                        ThreadPool.GetAvailableThreads(out int after, out _);
                        double loadAfter = (_poolSize - after) * 100.0 / _poolSize;

                        AddTime(sw.Elapsed.TotalMilliseconds);
                        AddLoad((loadBefore + loadAfter) / 2);
                        AddResult($"{Path.GetFileName(file)}: {content.Length} bytes");
                    }));
                }
            }

            await Task.WhenAll(tasks);
        }

        // =====================================================================================
        // 5) I/O-bound: паралельне читання з прогресом всіх .txt у ReadFolder
        // =====================================================================================
        public static async Task RunFileReadWithProgressParallelAsync(int calls)
        {
            ClearMetrics();
            var files = Directory.GetFiles(ReadFolder, "*.txt");
            var tasks = new List<Task>();

            for (int i = 0; i < calls; i++)
            {
                foreach (var file in files)
                {
                    ThreadPool.GetAvailableThreads(out int before, out _);
                    double loadBefore = (_poolSize - before) * 100.0 / _poolSize;

                    tasks.Add(Task.Run(async () =>
                    {
                        var sw = Stopwatch.StartNew();
                        int bytes = await TaskSimulator.ReadFileWithProgressAsync(file);
                        sw.Stop();

                        ThreadPool.GetAvailableThreads(out int after, out _);
                        double loadAfter = (_poolSize - after) * 100.0 / _poolSize;

                        AddTime(sw.Elapsed.TotalMilliseconds);
                        AddLoad((loadBefore + loadAfter) / 2);
                        AddResult($"{Path.GetFileName(file)}: {bytes} bytes");
                    }));
                }
            }

            await Task.WhenAll(tasks);
        }

        // =====================================================================================
        // 6) I/O-bound: паралельні HTTP-запити курсу Bitcoin (CoinGecko)
        //    – calls   : скільки разів запитати API
        //    – SemaphoreSlim(5) → максимум 5 запитів одночасно, щоб уникнути 429
        // =====================================================================================
        public static async Task RunBitcoinParallelAsync(int calls)
        {
            ClearMetrics();

            using var sem = new SemaphoreSlim(5);          // ≤5 паралельних HTTP
            var tasks = Enumerable.Range(0, calls).Select(async _ =>
            {
                await sem.WaitAsync();
                try
                {
                    ThreadPool.GetAvailableThreads(out int before, out _);
                    var sw = Stopwatch.StartNew();

                    BtcInfo? info = null;
                    for (int attempt = 0; attempt < 3 && info is null; attempt++)
                    {
                        info = await TaskSimulator.GetBitcoinInfoAsync();
                        if (info is null)
                            await Task.Delay(TimeSpan.FromMilliseconds(
                                             500 * Math.Pow(2, attempt))); // 0,5 → 1 → 2 c
                    }

                    sw.Stop();
                    ThreadPool.GetAvailableThreads(out int after, out _);
                    double load = ((_poolSize - before) + (_poolSize - after)) / 2.0
                                  * 100 / _poolSize;

                    AddTime(sw.Elapsed.TotalMilliseconds);
                    AddLoad(load);

                    if (info is not null)
                    {
                        // додаємо курс USD у колекцію цін
                        _prices.Add(info.Usd);

                        AddResult($"{info.Time:HH:mm:ss} | " +
                                  $"${info.Usd:F0} USD / €{info.Eur:F0} EUR / ₴{info.Uah:F0} UAH");
                    }
                    else
                    {
                        AddResult("ERR");
                    }
                }
                finally
                {
                    sem.Release();
                }
            });

            await Task.WhenAll(tasks);
        }



        // =====================================================================================
        // 7) CPU-bound: послідовний пошук простих
        // =====================================================================================
        public static void RunCPUBound(int max)
        {
            ClearMetrics();
            for (int i = 0; i < TaskCount; i++)
            {
                var sw = Stopwatch.StartNew();
                var primes = TaskSimulator.FindPrimes(2, max);
                sw.Stop();

                ThreadPool.GetAvailableThreads(out int avail, out _);
                double load = (_poolSize - avail) * 100.0 / _poolSize;

                AddTime(sw.Elapsed.TotalMilliseconds);
                AddLoad(load);
                AddResult(primes.Count.ToString());
            }
        }

        // =====================================================================================
        // 8) CPU-bound: паралельний пошук простих
        // =====================================================================================
        public static async Task RunCPUBoundParallel(int maxValue)
        {
            ClearMetrics();
            int chunk = maxValue / TaskCount;
            int start = 2;
            var tasks = new List<Task>();

            for (int i = 0; i < TaskCount; i++)
            {
                int localStart = start;
                int localEnd = (i == TaskCount - 1) ? maxValue : start + chunk - 1;

                ThreadPool.GetAvailableThreads(out int before, out _);
                double loadBefore = (_poolSize - before) * 100.0 / _poolSize;

                tasks.Add(Task.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    var primes = TaskSimulator.FindPrimes(localStart, localEnd);
                    sw.Stop();

                    ThreadPool.GetAvailableThreads(out int after, out _);
                    double loadAfter = (_poolSize - after) * 100.0 / _poolSize;

                    AddTime(sw.Elapsed.TotalMilliseconds);
                    AddLoad((loadBefore + loadAfter) / 2);
                    AddResult(primes.Count.ToString());
                }));

                start += chunk;
            }

            await Task.WhenAll(tasks);
        }
    }
}
