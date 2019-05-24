﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CS474_JSort
{
    class Program
    {
        // Global processor count set to 4 for now to match example in Jie's paper
        private static int _processorCount = 4;
        private static int[] _array = new[]{5, 17, 42, 3, 32, 22, 51, 26, 15, 9, 19, 99};
        private static int _size = _array.Length;

        static void Main(string[] args)
        {
            // Get starting values 
            int startIndex = 0;
            int endIndex = _size - 1;

            Console.WriteLine("Original array:");
            PrintArray();

            DoSort(startIndex, endIndex);
        }
        
        /*
         * Recursive method that checks when sorting/partitioning will be done 
         */
        private static void DoSort(int start, int end)
        {
            // Set pivot to a[5] as stated in Jie's paper
            int pivot = _array[5];

            if (start < end)
            {
                int partitionIndex = DoPartition(start, end, pivot);

                if (partitionIndex > 1)
                    DoSort(start, partitionIndex - 1);

                if (partitionIndex + 1 < end)
                    DoSort(partitionIndex + 1, end);
            }
        }

        /*
         * Method that partitions and sorts
         */
        private static int DoPartition(int start, int end, int pivot)
        {
            Mutex mLock = new Mutex();
            
            // Set chunk size
            var chunk = _size / _processorCount;

            // Initialize temporary arrays
            int[] temp = new int[_size];
            int[] nSmallerEqual = new int[_processorCount];
            int[] nGreaterThan = new int[_processorCount];

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

                if (endIndex > _size) endIndex = _size;

                // Counter variables initialized with above start/end variables
                int lesser = startIndex;
                int greater = endIndex;

                // Check for less than or equal to and greater than pivot 
                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (_array[i] <= pivot)
                    {
                        mLock.WaitOne();
                        temp[lesser] = _array[i];
                        mLock.ReleaseMutex();

                        lesser++;
                    }
                    else
                    {
                        mLock.WaitOne();
                        temp[greater] = _array[i];
                        mLock.ReleaseMutex();

                        greater--;
                    }
                }

                mLock.WaitOne();
                nSmallerEqual[uniqueId] = lesser - startIndex; // # elements smaller than pivot 
                nGreaterThan[uniqueId] = endIndex - greater; // # elements greater than pivot 
                mLock.ReleaseMutex();

                // Calculate prefix sums
                int smallerThanCount = 0;
                int greaterThanCount = 0;

                //for (int i = 0; i <= uniqueId; i++)
                //{
                //    smallerThanCount += nSmallerEqual[i];      // calculate the offset of 1st smaller than pivot element 
                //    greaterThanCount += nGreaterThan[i];      // calculate offset of first greater than pivot element 
                //    //Console.WriteLine("ID: {0}. Prefix sum calculation. i = {1}, smallerThanCount = {2}, greaterThanCount = {3}", uniqueId, i, smallerThanCount, greaterThanCount);
                //}


                ////Calculate prefix sums on arrays
                for (int k = 0; k <= uniqueId; k++)
                {
                    int whateverThisIs = (int)(uniqueId - Math.Pow(2, k));
                    if (whateverThisIs >= 0)
                    {
                        mLock.WaitOne();
                        nSmallerEqual[uniqueId] = nSmallerEqual[whateverThisIs] + nSmallerEqual[uniqueId];
                        nGreaterThan[uniqueId] = nGreaterThan[whateverThisIs] + nGreaterThan[uniqueId];
                       // Console.WriteLine("ID: {0}. Prefix sum calculation. i = {1}, smallerThanCount = {2}, greaterThanCount = {3}", uniqueId, k, nSmallerEqual[uniqueId], nGreaterThan[uniqueId]);

                        mLock.ReleaseMutex();
                    }
                }

                ////Determine starting points for each processor to copy elements <= pivot and > pivot
                if (uniqueId != 0)
                {
                    smallerThanCount = nSmallerEqual[uniqueId - 1];
                    greaterThanCount = nGreaterThan[uniqueId - 1];
                    //Console.WriteLine("ID: {0}. New prefix sum values.smallerThanCount = {1}, greaterThanCount = {2}", uniqueId, smallerThanCount, greaterThanCount);

                }
                else
                {
                    smallerThanCount = 0;
                    greaterThanCount = 0;
                }

                Console.WriteLine("ID: {0}, Start: {1}, End: {2}, SmallerOrEqual: {3}, GreaterThan: {4}, SmallerThanCount: {5}, GreaterThanCount: {6}", uniqueId, startIndex, endIndex, nSmallerEqual[uniqueId], nGreaterThan[uniqueId], smallerThanCount, greaterThanCount);

                // Using count variables, copy from temp array back to original array
                for (int i = startIndex; i <= endIndex; i++)
                {
                    // Add from left side of array if smaller
                    if (temp[i] <= pivot)
                    {
                        mLock.WaitOne();
                        _array[smallerThanCount] = temp[i];
                        Console.WriteLine("ID: {0}, Smaller than pivot. Writing {1} to index {2} on array. i = {3}", uniqueId, temp[i], smallerThanCount, i);
                        mLock.ReleaseMutex();

                        smallerThanCount = smallerThanCount + 1;
                    }
                    // Add from right side of array if greater
                    else
                    {
                        mLock.WaitOne();
                        _array[(_size - 1) - greaterThanCount] = temp[i];
                        Console.WriteLine("ID: {0}, Greater than pivot. Writing {1} to index {2} on array. End index: {3}, GreaterThan: {4}, i = {5} ", uniqueId, temp[i], (endIndex - greaterThanCount), endIndex, greaterThanCount, i);
                        mLock.ReleaseMutex();

                        greaterThanCount = greaterThanCount - 1;
                    }
                }

                queue.Enqueue(uniqueId);
            });

            Console.WriteLine("Sorted:");
            PrintArray();

            // Get median of startIndex, endIndex, and the middle index and use that to find pivot. 
            int middle = (start + end) / 2;
            int median = (start + end + middle) / 3;
            int partitionIndex = _array[median];

            return partitionIndex;
        }

        private static void PrintArray()
        {
            foreach (var item in _array)
            {
                Console.Write(item + " ");
            }

            Console.WriteLine();
        }
    }
}
