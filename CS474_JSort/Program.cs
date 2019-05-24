using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CS474_JSort
{
    class Program
    {
        // Global processor count set to 4 for now to match example in Jie's paper
        private static int _processorCount = 4;
        private static int[] _array = new[]{5, 17, 42, 3, 9, 22, 15, 26, 51, 19, 99, 32};
        private static int _size = _array.Length;

        static void Main(string[] args)
        {
            // Get starting values 
            int startIndex = 0;
            int endIndex = _size - 1;

            Console.WriteLine("Original array:");
            PrintArray();

            int middle = (startIndex + endIndex) / 2;
            int median = (startIndex + endIndex + middle) / 3;
            int pivot = _array[median];
            DoSort(startIndex, endIndex, pivot);
        }
        
        /*
         * Recursive method that checks when sorting/partitioning will be done 
         */
        private static void DoSort(int startIndex, int endIndex, int pivot)
        {
            if (startIndex < endIndex)
            {
                int partitionIndex = DoPartition(startIndex, endIndex, pivot);

                if (partitionIndex > 1)
                    DoSort(startIndex, partitionIndex, pivot);

                if (partitionIndex + 1 < endIndex)
                    DoSort(partitionIndex + 1, endIndex, pivot);
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

            for (int i = 0; i < _size; i++)
            {
                temp[i] = _array[i];
            }
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
                    if (_array[i] <= pivot) lesser++;
                    else greater--;
                }

                mLock.WaitOne();
                nSmallerEqual[uniqueId] = lesser - startIndex; // # elements smaller than pivot 
                nGreaterThan[uniqueId] = endIndex - greater; // # elements greater than pivot 
                mLock.ReleaseMutex();

                // Calculate prefix sums on arrays
                //for (int k = 0; k <= uniqueId; k++)
                //{
                //    int whateverThisIs = (int)(uniqueId - Math.Pow(2, k));
                //    if (whateverThisIs >= 0)
                //    {
                //        mLock.WaitOne();
                //        nSmallerEqual[uniqueId] = nSmallerEqual[whateverThisIs] + nSmallerEqual[uniqueId];
                //        mLock.ReleaseMutex();

                //        mLock.WaitOne();
                //        nGreaterThan[uniqueId] = nGreaterThan[whateverThisIs] + nGreaterThan[uniqueId];
                //        mLock.ReleaseMutex();
                //    }
                //}

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

                Console.WriteLine("ID: {0}. SmallerEqual: {1}. GreaterThan {2}", uniqueId, smallerThanCount, greaterThanCount);


                //Determine starting points for each processor to copy elements <= pivot and > pivot
                //if (uniqueId != 0)
                //{
                //    smallerThanCount = nSmallerEqual[uniqueId - 1];
                //    greaterThanCount = nGreaterThan[uniqueId - 1];
                //}
                //else
                //{
                //    smallerThanCount = 0;
                //    greaterThanCount = 0;
                //}


                // Using count variables, copy from temp array back to original array
                for (int i = startIndex; i <= endIndex; i++)
                {
                    // Add from left side of array if smaller
                    if (temp[i] <= pivot)
                    {
                        mLock.WaitOne();
                        _array[smallerThanCount] = temp[i];
                        mLock.ReleaseMutex();
                        Console.WriteLine("ID: {0}. SMALLER THAN. i = {1}. end = {2}. smallerThanCount = {3}. greaterThanCount = {4}. Writing {5} to index {6}", uniqueId, i, endIndex, smallerThanCount, greaterThanCount, temp[i], smallerThanCount);

                        smallerThanCount = smallerThanCount + 1;
                    }
                    // Add from right side of array if greater
                    else
                    {
                        mLock.WaitOne();
                        _array[(_size - 1) - greaterThanCount] = temp[i];
                        Console.WriteLine("ID: {0}. GREATER THAN. i = {1}. end = {2}. smallerThanCount = {3}. greaterThanCount = {4}. Writing {5} to index {6}", uniqueId, i, endIndex, smallerThanCount, greaterThanCount, temp[i], (_size - 1) - greaterThanCount);

                        greaterThanCount = greaterThanCount + 1;
                        mLock.ReleaseMutex();

                    }
                }

                queue.Enqueue(uniqueId);
            });

            Console.WriteLine("Sorted:");
            PrintArray();
            CheckForDuplicates();

            return nSmallerEqual[_processorCount - 1];
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
    }
}
