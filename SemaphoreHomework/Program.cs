using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SemaphoreHomework
{

    internal class Program
    {
        private const int CountOfNumbers = 2000;

        private const int MinValue = 1;
        private const int MaxValue = 100000;

        private const string NumbersFile = "numbers.txt";
        private const string PrimesFile = "primes.txt";
        private const string PrimesEndingWith7File = "primes_ending_7.txt";
        private const string ReportFile = "report.txt";

        private static readonly Semaphore Sem12 = new Semaphore(0, 1);
        private static readonly Semaphore Sem23 = new Semaphore(0, 1);
        private static readonly Semaphore Sem34 = new Semaphore(0, 1); 

        private static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== Домашнє завдання: М'ютекси. Семафори ===");
            Console.WriteLine($"Робоча директорія: {Directory.GetCurrentDirectory()}");
            Console.WriteLine();

            Thread thread1 = new Thread(GenerateNumbers) { Name = "Потік-1 (генерація)" };
            Thread thread2 = new Thread(FilterPrimes) { Name = "Потік-2 (прості числа)" };
            Thread thread3 = new Thread(FilterPrimesEndingWith7) { Name = "Потік-3 (прості на 7)" };
            Thread thread4 = new Thread(BuildReport) { Name = "Потік-4 (звіт)" };

            thread1.Start();
            thread2.Start();
            thread3.Start();
            thread4.Start();

            thread1.Join();
            thread2.Join();
            thread3.Join();
            thread4.Join();

            Console.WriteLine();
            Console.WriteLine("=== Усі потоки завершили роботу. Перевірте файли: ===");
            Console.WriteLine($"  {NumbersFile}, {PrimesFile}, {PrimesEndingWith7File}, {ReportFile}");
        }

        private static void GenerateNumbers()
        {
            Log("Старт генерації випадкових чисел...");

            Random random = new Random();
            var numbers = new List<int>(CountOfNumbers);

            for (int i = 0; i < CountOfNumbers; i++)
            {
                numbers.Add(random.Next(MinValue, MaxValue + 1));
            }

            File.WriteAllLines(NumbersFile, ConvertToStrings(numbers));

            Log($"Згенеровано {numbers.Count} чисел -> файл '{NumbersFile}'.");
            Sem12.Release();
        }


        private static void FilterPrimes()
        {
            Log("Очікування завершення Потоку-1 (генерації чисел)...");
            Sem12.WaitOne(); 

            Log("Отримано сигнал. Починаю пошук простих чисел...");

            int[] numbers = ReadNumbersFromFile(NumbersFile);
            var primes = new List<int>();

            foreach (int number in numbers)
            {
                if (IsPrime(number))
                {
                    primes.Add(number);
                }
            }

            File.WriteAllLines(PrimesFile, ConvertToStrings(primes));

            Log($"Знайдено {primes.Count} простих чисел -> файл '{PrimesFile}'.");

            Sem23.Release();
        }

        private static void FilterPrimesEndingWith7()
        {
            Log("Очікування завершення Потоку-2 (пошук простих чисел)...");
            Sem23.WaitOne();

            Log("Отримано сигнал. Відбираю прості числа, що закінчуються на 7...");

            int[] primes = ReadNumbersFromFile(PrimesFile);
            var primesEndingWith7 = new List<int>();

            foreach (int prime in primes)
            {
                if (prime % 10 == 7)
                {
                    primesEndingWith7.Add(prime);
                }
            }

            File.WriteAllLines(PrimesEndingWith7File, ConvertToStrings(primesEndingWith7));

            Log($"Знайдено {primesEndingWith7.Count} простих чисел, що закінчуються на 7 " +
                $"-> файл '{PrimesEndingWith7File}'.");

            Sem34.Release();
        }

        private static void BuildReport()
        {
            Log("Очікування завершення Потоку-3 (фільтрація простих на 7)...");
            Sem34.WaitOne();

            Log("Отримано сигнал. Формую підсумковий звіт...");

            string[] files = { NumbersFile, PrimesFile, PrimesEndingWith7File };
            var reportLines = new List<string>
            {
                "===== ЗВІТ ПРО ОТРИМАНІ ФАЙЛИ =====",
                $"Дата/час формування: {DateTime.Now:dd.MM.yyyy HH:mm:ss}",
                string.Empty
            };

            foreach (string file in files)
            {
                int count = File.Exists(file) ? File.ReadAllLines(file).Length : 0;
                long sizeInBytes = File.Exists(file) ? new FileInfo(file).Length : 0;

                reportLines.Add($"Файл: {file}");
                reportLines.Add($"  - кількість чисел : {count}");
                reportLines.Add($"  - розмір файлу     : {sizeInBytes} байт");
                reportLines.Add(string.Empty);
            }

            File.WriteAllLines(ReportFile, reportLines);

            Log($"Звіт сформовано -> файл '{ReportFile}'.");
        }

   
        private static bool IsPrime(int number)
        {
            if (number < 2)
            {
                return false;
            }

            if (number == 2)
            {
                return true;
            }

            if (number % 2 == 0)
            {
                return false;
            }

            int boundary = (int)Math.Sqrt(number);
            for (int divisor = 3; divisor <= boundary; divisor += 2)
            {
                if (number % divisor == 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static int[] ReadNumbersFromFile(string path)
        {
            string[] lines = File.ReadAllLines(path);
            var result = new int[lines.Length];

            for (int i = 0; i < lines.Length; i++)
            {
                result[i] = int.Parse(lines[i]);
            }

            return result;
        }

        private static IEnumerable<string> ConvertToStrings(List<int> numbers)
        {
            foreach (int n in numbers)
            {
                yield return n.ToString();
            }
        }

        private static readonly object ConsoleLock = new object();

        private static void Log(string message)
        {
            lock (ConsoleLock)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{Thread.CurrentThread.Name}] {message}");
            }
        }
    }
}