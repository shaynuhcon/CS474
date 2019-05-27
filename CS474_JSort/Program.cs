using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CS474_JSort
{
    class Program
    {
        private static readonly Stopwatch Stopwatch = new Stopwatch();
        private static long _elapsedTime;

        static void Main()
        {
            int[] array = new[] { 5, 17, 42, 3, 9, 22, 15, 26, 51, 19, 99, 32 };
            
            // Get starting values 
            int startIndex = 0;
            int endIndex = array.Length;
            
            DoSort(array, startIndex, endIndex);


            // Only output results if array is sorted (not timed)
            if (IsSorted(array))
            {
                Console.WriteLine("Sorted {0} elements in parallel in {1} ms", array.Length, _elapsedTime);
            }
            else
            {
                Console.WriteLine("Array is not sorted");
            }

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

                if (partitionIndex < 1) return;

                if (partitionIndex > 1)
                    DoSort(array, startIndex, partitionIndex);

                if (partitionIndex < endIndex)
                    DoSort(array, partitionIndex, endIndex);
            }
        }

        /*
         * Determine how many processors to use based on size of array
         */
        private static int Spawn(int size)
        {
            if (size < Environment.ProcessorCount)
            {
                return size - 1;

            }

            return Environment.ProcessorCount;
        }

        /*
         * Method that sorts each subarray then returns index that array should be partitioned on
         */
        private static int DoPartition(int[] array, int start, int end)
        {
            var size = end - start;

            // Spawn processors based on size of array
            var processorCount = Spawn(size);

            // Initialize subarray from original array that will be sorted 
            var subArray = new int[size];
            Array.Copy(array, start, subArray, 0, size);

            // Initialize temp array 
            int[] temp = new int[subArray.Length];

            for (int i = 0; i < size; i++)
            {
                temp[i] = subArray[i];
            }

            // Get pivot
            int middle = (0 + size) / 2;
            int median = (0 + size + middle) / 3;
            int pivot = temp[median];

            Console.WriteLine("Sorting subarray {0} to {1} on pivot {2}", start, end, pivot);
            // Swap pivot with last index in both subarray and temp array 
            // to ensure we do not use same pivot too many times
            int placeholder;
            placeholder = temp[size -1];
            temp[size -1] = temp[median];
            temp[median] = placeholder;
            subArray[size - 1] = subArray[median];
            subArray[median] = placeholder;

            // Initialize arrays that will be used to calcualte prefix sums 
            int[] nSmallerEqual = new int[processorCount];
            int[] nGreaterThan = new int[processorCount];

            // Set chunk size
            decimal decimalChunk = (decimal) size / (decimal)processorCount;
            var chunk = (int)Math.Ceiling(decimalChunk);
            
            Mutex mLock = new Mutex();
            Stopwatch.Start();

            // Sort chunked portions of array
            Parallel.For(0, processorCount, id =>
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

                    smallerThanCount += nSmallerEqual[i];
                    greaterThanCount += nGreaterThan[i];
                }
                mLock.ReleaseMutex();

                // Using count variables, copy from temp array back to original array
                for (int i = startIndex; i < endIndex; i++)
                {
                    // Add from left side of array if smaller
                    if (temp[i] <= pivot)
                    {
                        mLock.WaitOne();
                        subArray[smallerThanCount] = temp[i];
                        smallerThanCount = smallerThanCount + 1;

                        mLock.ReleaseMutex();

                    }
                    // Add from right side of array if greater
                    else
                    {
                        mLock.WaitOne();
                        subArray[(size - 1) - greaterThanCount] = temp[i];
                        greaterThanCount = greaterThanCount + 1;

                        mLock.ReleaseMutex();

                    }
                }
            });

            Stopwatch.Stop();
            _elapsedTime = Stopwatch.ElapsedMilliseconds;

            // Copy subarray back to array
            int subArrayCount = 0;
            for (int i = start; i < end; i++)
            {
                array[i] = subArray[subArrayCount];
                subArrayCount++;
            }

            // Determine next partition index using values lesser than pivot
            int lesserThan = 0;
            for (int i = 0; i < subArray.Length; i++)
            {
                if (subArray[i] <= pivot)
                {
                    lesserThan++;
                }
            }
            
            // Don't need to partition subarray if size of subarray is less than # of lesser than values
            if (size < lesserThan) return 0;

            // All values are lesser than pivot so return index right below lesser than count
            if (lesserThan == size) lesserThan--;


            // Pass actual index in array and not index of subarray 
            int partitionIndex = Array.IndexOf(array, subArray[lesserThan]);


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

        static bool IsSorted(int[] a)
        {
            int j = a.Length - 1;
            if (j < 1) return true;
            int ai = a[0], i = 1;
            while (i <= j && ai <= (ai = a[i])) i++;
            return i > j;
        }

        //// Question 5: Fill array sequentially with random values
        private static int[] FillArrayWithRandomValues(int[] array)
        {
            array = Enumerable.Range(0, 50).ToArray();
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
