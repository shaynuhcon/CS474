﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CS474_JSort
{
    class Program
    {
        // Global processor count set to 4 for now to match example in Jie's paper
        private static int _processorCount = 2;
        private static int[] _array = new[] { 5, 17, 42, 3, 9, 22, 15, 26, 51, 19, 99, 32 };

        static void Main()
        {
            // Get starting values 
            int startIndex = 0;
            int endIndex = _array.Length;
            Console.WriteLine("Original array:");
            PrintArray();


            DoSort(startIndex, endIndex);
        }
        
        /*
         * Recursive method that checks when sorting/partitioning will be done 
         */
        private static void DoSort(int startIndex, int endIndex)
        {
            if (startIndex != 1)
            {
                int partitionIndex = DoPartition(startIndex, endIndex);

                if (partitionIndex > 1)
                    DoSort(startIndex, partitionIndex);

                if (partitionIndex + 1 < endIndex)
                    DoSort(partitionIndex, endIndex);
            }
        }

        /*
         * Method that partitions and sorts
         */
        private static int DoPartition(int start, int end)
        {
            var size = end - start;
            if (size == 0 || end < start) return 1;

            Mutex mLock = new Mutex();
            var subArray = new int[size];
            Array.Copy(_array, start, subArray, 0, size);

            // Initialize temporary arrays
            int[] temp = new int[subArray.Length];

            for (int i = 0; i < size; i++)
            {
                temp[i] = subArray[i];
            }

            // Get pivot
            int middle = (0 + size) / 2;
            int median = (0 + size + middle) / 3;
            int pivot = temp[median];

            if (median <= 1)
            {
                return 1;
            }
            Console.WriteLine("Sorting indexes {0} through {1} on pivot {2}", start, end, pivot);

            int[] nSmallerEqual = new int[_processorCount];
            int[] nGreaterThan = new int[_processorCount];

            // Set chunk size
            var chunk = size / _processorCount;

            // Set thread safe variables for unique ID 
            var queue = new ConcurrentQueue<int>();
            for (int i = 0; i < _processorCount; i++)
            {
                queue.Enqueue(i);
            }

            // Divide array into chunks
            Parallel.For(0, _processorCount, j =>
            {
                // Unique ID variable used to identify each processor
                int uniqueId;
                queue.TryDequeue(out uniqueId);

                // Starting and ending point of chunk
                int startIndex = chunk * uniqueId; 
                int endIndex = ((uniqueId + 1) * chunk) -1;

                if (endIndex > size) endIndex = size;

                // Counter variables initialized with above start/end variables
                int lesser = startIndex;
                int greater = endIndex;

                // Check for less than or equal to and greater than pivot 
                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (_array[i] <= pivot) lesser++;
                    else greater--;
                }

                mLock.WaitOne();
                nSmallerEqual[uniqueId] = lesser - startIndex; // # elements smaller than pivot 
                nGreaterThan[uniqueId] = endIndex - greater; // # elements greater than pivot 
                mLock.ReleaseMutex();

                // These values hold total count for lesser than or greater than values
                // Will be used for inserting values back to original array 
                int smallerThanCount = 0;
                int greaterThanCount = 0;

                mLock.WaitOne();
                for (int i = 0; i < uniqueId; i++)
                {

                    smallerThanCount += nSmallerEqual[i];      // calculate the offset of 1st smaller than pivot element 
                    greaterThanCount += nGreaterThan[i];      // calculate offset of first greater than pivot element 
                }
                mLock.ReleaseMutex();

                Console.WriteLine("ID: {0}. Start: {1}. End: {2}. SmallerEqual: {3}. GreaterThan {4}", uniqueId, startIndex, endIndex, smallerThanCount, greaterThanCount);

                // Using count variables, copy from temp array back to original array
                for (int i = startIndex; i <= endIndex; i++)
                {
                    // Add from left side of array if smaller
                    if (temp[i] <= pivot)
                    {
                        mLock.WaitOne();
                        _array[smallerThanCount] = temp[i];
                        mLock.ReleaseMutex();

                        smallerThanCount = smallerThanCount + 1;
                    }
                    // Add from right side of array if greater
                    else
                    {
                        mLock.WaitOne();
                        _array[(size - 1) - greaterThanCount] = temp[i];
                        mLock.ReleaseMutex();

                        greaterThanCount = greaterThanCount + 1;
                    }
                }

                queue.Enqueue(uniqueId);
            });

            Console.WriteLine("Sorted:");
            PrintArray();
            CheckForDuplicates();

            int smallerThanTotal = 0;
            for (int i = 0; i < _processorCount; i++)
            {
                smallerThanTotal += nSmallerEqual[i];      // calculate the offset of 1st smaller than pivot element 
            }

            if (smallerThanTotal == 0) return 1;

            return smallerThanTotal;
        }

        private static void CheckForDuplicates()
        {
            if (_array.Length != _array.Distinct().Count())
            {
                Console.WriteLine("Contains duplicates");
            }
        }

        private static void PrintArray()
        {
            foreach (var item in _array)
            {
                Console.Write(item + " ");
            }

            Console.WriteLine();
        }

        // Question 5: Fill array sequentially with random values
        private static void FillArrayWithRandomValues()
        {
            _array = Enumerable.Range(0, 20).ToArray();
            var random = new Random();
            for (var i = 0; i < _array.Length; i++)
            {
                int randomIndex = random.Next(_array.Length);
                int temp = _array[randomIndex];
                _array[randomIndex] = _array[i];
                _array[i] = temp;
            }
        }
    }
}
