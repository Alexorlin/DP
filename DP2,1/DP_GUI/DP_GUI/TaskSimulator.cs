// File: TaskSimulator.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;

namespace DP_GUI
{
    public static class TaskSimulator
    {

        // =====================================================================================
        // Обчислення факторіалу N як BigInteger
        // =====================================================================================
        public static BigInteger ComputeFactorialBig(int N)
        {
            BigInteger r = 1;
            for (int i = 1; i <= N; i++)
                r *= i;
            return r;
        }

        // =====================================================================================
        // 1) CPU-bound: обчислення факторіалів від 1 до N для count ітерацій
        // =====================================================================================
        public static void ComputeFactorials(int count)
        {
            for (int n = 0; n < count; n++)
            {
                double r = 1;
                for (int i = 1; i <= 10_000; i++)
                    r *= i;
            }
        }
        public static Task ComputeFactorialsAsync(int count) =>  
            Task.Run(() => ComputeFactorials(count));

        // =====================================================================================
        // 2) HTTP JSON-запит: отримати поточний курс BTC/USD з CoinDesk API
        // =====================================================================================
        public record BtcInfo(decimal Usd, decimal Eur, decimal Uah, DateTime Time);

        public static async Task<BtcInfo?> GetBitcoinInfoAsync()
        {
            const string url =
              "https://api.coingecko.com/api/v3/simple/price?" +
              "ids=bitcoin&vs_currencies=usd,eur,uah&include_last_updated_at=true";

            try
            {
                using var client = new HttpClient();    
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DP_GUI");
                string json = await client.GetStringAsync(url);

                using var doc = JsonDocument.Parse(json);
                var btc = doc.RootElement.GetProperty("bitcoin");
                decimal usd = btc.GetProperty("usd").GetDecimal();
                decimal eur = btc.GetProperty("eur").GetDecimal();
                decimal uah = btc.GetProperty("uah").GetDecimal();
                long ts = btc.GetProperty("last_updated_at").GetInt64();
                return new BtcInfo(usd, eur, uah,
                                   DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime);
            }
            catch (Exception ex)
            {
                // вертаємо null – далі в WorkRunner покажемо «ERR»
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        // =====================================================================================
        // 3) Асинхронне читання усього файлу з диску
        // =====================================================================================
        public static Task<string> ReadFileAsync(string path) =>
            File.ReadAllTextAsync(path);

        // =====================================================================================
        // 4) Асинхронне поблочне читання файлу з підрахунком байтів
        // =====================================================================================
        public static async Task<int> ReadFileWithProgressAsync(string path)
        {
            const int bufferSize = 4096;
            int totalBytes = 0;
            using var fs = File.OpenRead(path);
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            while ((bytesRead = await fs.ReadAsync(buffer, 0, bufferSize)) > 0)
                totalBytes += bytesRead;
            return totalBytes;
        }

        // =====================================================================================
        // 5) CPU-bound: пошук простих чисел у діапазоні [a..b]
        // =====================================================================================
        public static List<int> FindPrimes(int a, int b)
        {
            var primes = new List<int>();
            for (int i = Math.Max(2, a); i <= b; i++)
            {
                bool isPrime = true;
                int limit = (int)Math.Sqrt(i);
                for (int j = 2; j <= limit; j++)
                    if (i % j == 0)
                    {
                        isPrime = false;
                        break;
                    }
                if (isPrime)
                    primes.Add(i);
            }
            return primes;
        }
    }
}
