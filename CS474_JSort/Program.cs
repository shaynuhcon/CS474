using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;

namespace CS474_JSort
{
    class Program
    {
        // Global processor count set to 4 for now to match example in Jie's paper
        private static int _processorCount = 4;

        static void Main()
        {
            int[] array = new[] { 5, 17, 42, 3, 9, 22, 15, 26, 51, 19, 99, 32 };


            // Get starting values 
            int startIndex = 0;
            int endIndex = array.Length;
            Console.WriteLine("Original array:");
            PrintArray(array);

            DoSort(array, startIndex, endIndex);
            Console.WriteLine("Done");
            Console.ReadLine();
        }
        
        /*
         * Recursive method that checks when sorting/partitioning will be done 
         */
        private static void DoSort(int[] array, int startIndex, int endIndex)
        {
            if (endIndex - startIndex > 1)
            {
                int partitionIndex = DoPartition(array, startIndex, endIndex);

                if (partitionIndex == 0) return;
                if (endIndex - startIndex == partitionIndex) return;

                if (partitionIndex > 1)
                    DoSort(array, startIndex, partitionIndex);

                if (partitionIndex + 1 < endIndex)
                    DoSort(array, partitionIndex + 1, endIndex);
            }
        }

        /*
         * Method that partitions and sorts
         */
        private static int DoPartition(int[] array, int start, int end)
        {
            var size = end - start;
            if (size == 0 || end < start) return 1;

            Mutex mLock = new Mutex();
            var subArray = new int[size];
            Array.Copy(array, start, subArray, 0, size);

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

            int[] nSmallerEqual = new int[_processorCount];
            int[] nGreaterThan = new int[_processorCount];

            // Set chunk size
            decimal decimalChunk = (decimal) size / (decimal) _processorCount;
            var chunk = (int)Math.Ceiling(decimalChunk);

            Console.WriteLine("Sorting indexes {0} through {1} on pivot {2}", start, end, pivot);

            //// Divide array into chunks
            Parallel.For(0, _processorCount, id =>
            {
                // Starting and ending point of chunk
                int startIndex = chunk * id;
                int endIndex = chunk * (id + 1);
                if (endIndex > size) endIndex = size;

                // Check for less than or equal to and greater than pivot 
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (subArray[i] <= pivot)
                    {
                        mLock.WaitOne();
                        nSmallerEqual[id]++;
                        mLock.ReleaseMutex();

                    }
                    else
                    {
                        mLock.WaitOne();
                        nGreaterThan[id]++;
                        mLock.ReleaseMutex();
                    }
                }

                // These values hold total count for lesser than or greater than values
                // Will be used for inserting values back to original array 
                int smallerThanCount = 0;
                int greaterThanCount = 0;

                mLock.WaitOne();
                for (int i = 0; i < id; i++)
                {

                    smallerThanCount += nSmallerEqual[i];      // calculate the offset of 1st smaller than pivot element 
                    greaterThanCount += nGreaterThan[i];      // calculate offset of first greater than pivot element 
                }
                mLock.ReleaseMutex();

                Console.WriteLine("ID: {0}. Start: {1}. End: {2}. SmallerEqual: {3}. GreaterThan {4}", id, startIndex, endIndex, smallerThanCount, greaterThanCount);

                // Using count variables, copy from temp array back to original array
                for (int i = startIndex; i < endIndex; i++)
                {
                    // Add from left side of array if smaller
                    if (temp[i] <= pivot)
                    {
                        mLock.WaitOne();
                        subArray[smallerThanCount] = temp[i];
                        //Console.WriteLine("ID: {0}. SMALLER THAN. i = {1}. end = {2}. smallerThanCount = {3}. greaterThanCount = {4}. Writing {5} to index {6}", id, i, endIndex, smallerThanCount, greaterThanCount, temp[i], smallerThanCount);

                        smallerThanCount = smallerThanCount + 1;

                        mLock.ReleaseMutex();

                    }
                    // Add from right side of array if greater
                    else
                    {
                        mLock.WaitOne();
                        subArray[(size - 1) - greaterThanCount] = temp[i];
                        //Console.WriteLine("ID: {0}. GREATER THAN. i = {1}. end = {2}. smallerThanCount = {3}. greaterThanCount = {4}. Writing {5} to index {6}", id, i, endIndex, smallerThanCount, greaterThanCount, temp[i], (size - 1) - greaterThanCount);

                        greaterThanCount = greaterThanCount + 1;

                        mLock.ReleaseMutex();

                    }
                }
            });

            int subArrayCount = 0;
            for (int i = start; i < end; i++)
            {
                array[i] = subArray[subArrayCount];
                subArrayCount++;
            }
            Console.WriteLine("Sorted:");
            PrintArray(array);
            CheckForDuplicates(array);

            int partitionIndex = 0;
            for (int i = 0; i < _processorCount; i++)
            {
                partitionIndex += nSmallerEqual[i];     
            }

            return partitionIndex;
        }

        private static void CheckForDuplicates(int[] array)
        {
            if (array.Length != array.Distinct().Count())
            {
                Console.WriteLine("Contains duplicates");
            }
        }

        private static void PrintArray(int[] array)
        {
            foreach (var item in array)
            {
                Console.Write(item + " ");
            }

            Console.WriteLine();
        }

        //// Question 5: Fill array sequentially with random values
        private static int[] FillArrayWithRandomValues(int[] array)
        {
            array = Enumerable.Range(0, 10).ToArray();
            var random = new Random();
            for (var i = 0; i < array.Length; i++)
            {
                int randomIndex = random.Next(array.Length);
                int temp = array[randomIndex];
                array[randomIndex] = array[i];
                array[i] = temp;
            }

            return array;
        }
    }
}
