using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace CS474_Lab2
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var sizes = new List<long>();
            sizes.Add(1000);
            sizes.Add(1000000);
            sizes.Add(2000000);

            foreach (var size in sizes)
            {
                SequentialSieve(size);
                ParallelSieve(size);
                Console.WriteLine();
            }


            Console.ReadLine();
        }

        private static void SequentialSieve(long size)
        {
            // Initialize array
            var primeArray = new bool[size];

            // Get max 
            var limit = (int)Math.Ceiling(Math.Sqrt(size));

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Set all to true
            for (int i = 2; i < size; i++) if (i == 2 || i % 2 != 0) primeArray[i] = true;

            for (int i = 2; i < limit; i++)
            {
                // If index is prime
                if (primeArray[i])
                {
                    // Knock out multiples of index
                    for (int j = i * 2; j < size; j += i)
                    {
                        primeArray[j] = false;
                    }
                }
            }

            // Count number of primes
            var primeCount = 0;
            foreach (var isPrime in primeArray)
            {
                if (isPrime) primeCount++;
            }

            stopwatch.Stop();
            var elapsedTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();

            Console.WriteLine("Sequential: Found {0} prime numbers for array size {1}. Time: {2}ms", primeCount, size, elapsedTime);
        }

        private static void ParallelSieve(long size)
        {
            // Initialize array
            var primeArray = new bool[size];

            // Get max 
            var limit = (int) Math.Ceiling(Math.Sqrt(size));

            // Set all exception 0, 1, and any multiples of 2 to true 
            Parallel.For(2, primeArray.Length, i =>
            {
                if (i == 2 || i % 2 != 0) primeArray[i] = true;
            });

            // Initialize lock and get processor count
            Mutex mLock = new Mutex();

            var processorCount = Environment.ProcessorCount;

            // Start timer
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Sequential loop that tracks latest prime
            for (int latestPrime = 3; latestPrime < limit; latestPrime++)
            {
                if (primeArray[latestPrime])
                {
                    // Split up array between processor count / 2
                    Parallel.For(3, processorCount / 2, j =>
                    {
                        mLock.WaitOne();
                        // Knock out multiples of prime i
                        for (int k = latestPrime * latestPrime; k < size; k += latestPrime)
                        {
                            primeArray[k] = false;
                        }
                        mLock.ReleaseMutex();
                    });

                }

            }

            // End timer
            stopwatch.Stop();
            var elapsedTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();

            // Count number of primes
            var primeCount = 0;

            Parallel.For(0, primeArray.Length, i =>
            {
                mLock.WaitOne();
                if (primeArray[i]) primeCount++;
                mLock.ReleaseMutex();
            });


            Console.WriteLine("Parallel: Found {0} prime numbers for array size {1}. Time: {2}ms", primeCount, size, elapsedTime);
        }
    }
}