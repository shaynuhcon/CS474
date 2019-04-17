using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;

namespace CS474_Lab1
{
    internal class Program
    {
        private static int[] _intArray;
        private static readonly Random Random = new Random();

        private static void Main(string[] args)
        {
            // Initialize list that will set array sizes
            var arraySizes = new List<long>
            {
                20,
                1000,
                1000000,
                2000000,
                20000000
            };


            // Initialize list for chunk sizes
            // Set chunk sizes
            var chunkSizes = new List<long>
            {
                10,
                100,
                500,
                1000,
                100000,
                20000000
            };

            foreach (var arraySize in arraySizes)
            {
                _intArray = new int[arraySize];

                Console.WriteLine("-- Size {0} -- ", arraySize);

                FillArrayWithRandomValues();
                FindLargestNumberSequential();
                FindLargestNumberParallel();

                // Question 10: try partitioning/chunking for array size of 20,000,000
                if (arraySize == 20000000)
                {
                    // Question 10: Get core count 
                    var coreCount = 0;
                    foreach (var item in new ManagementObjectSearcher("Select * from Win32_Processor").Get())
                        coreCount += int.Parse(item["NumberOfCores"].ToString());

                    foreach (var chunkSize in chunkSizes)
                    {
                        FindLargestNumberChunkedParallel(coreCount, chunkSize);
                    }
                }

                Console.WriteLine();
            }

            Console.ReadLine();
        }

        // Fill array with random values
        private static void FillArrayWithRandomValues()
        {
            for (var i = 0; i < _intArray.Length; i++) _intArray[i] = Random.Next(0, 100);
        }

        // Find largest number in array sequentially 
        private static void FindLargestNumberSequential()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var largestNumber = 0;
            for (var i = 0; i < _intArray.Length; i++)
                if (_intArray[i] > largestNumber)
                    largestNumber = _intArray[i];

            stopwatch.Stop();

            var elapsedTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();

            // Print largest number and time it took to find
            Console.WriteLine("Largest number {0} found (sequential) in {1} ms", largestNumber, elapsedTime);
        }

        // Find largest number in array using parallel loop
        private static void FindLargestNumberParallel()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var largestNumber = 0;
            Parallel.For(0, _intArray.Length, i =>
            {
                if (_intArray[i] > largestNumber) largestNumber = _intArray[i];
            });

            stopwatch.Stop();

            var elapsedTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();

            // Print largest number and time it took to find
            Console.WriteLine("Largest number {0} found (parallel) in {1} ms", largestNumber, elapsedTime);
        }

        // Find largest number in array using parallel loop and partioning
        private static void FindLargestNumberChunkedParallel(int coreCount, long chunkSize)
        {
            // Question 10: Use chunk size divided by core count 
            var partitionSize = chunkSize / coreCount;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var largestNumber = 0;

            // Create partitions in array for each size 
            Parallel.ForEach(Partitioner.Create(0, _intArray.Length, partitionSize),
                range =>
                {
                    // Loop through partitioned part of array only
                    for (var i = (int) range.Item1; i < range.Item2; i++)
                    {
                        if (_intArray[i] > largestNumber) largestNumber = _intArray[i];
                    }

                });

            stopwatch.Stop();

            var elapsedTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();

            // Print largest number, time it took to find, chunk size, and core count
            Console.WriteLine("Largest number {0} found (chunked parallel) in {1} ms. Chunk size: {2}. Core Count: {3}", largestNumber, elapsedTime, chunkSize, coreCount);
        }
    }
}