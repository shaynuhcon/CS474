using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CS474_JSort
{
    class Program
    {
        // Global processor count set to 4 for now to match example in Jie's paper
        private static int _processorCount = 4;

        static void Main(string[] args)
        {
            int[] array = GetArray();

            // Get starting values 
            int n = array.Length;
            int startIndex = 0;
            int endIndex = n - 1;

            DoSort(array, startIndex, endIndex);
        }

        /*
         * Get array
         */
        private static int[] GetArray()
        {
            // Array values that match example in Jie's paper
            int[] array = {5, 17, 42, 3, 32, 22, 51, 26, 15, 9, 19, 99};
            return array;
        }

        /*
         * Recursive method that checks when sorting/partitioning will be done 
         */
        private static void DoSort(int[] array, int startIndex, int endIndex)
        {
            if (startIndex < endIndex)
            {
                int partitionIndex = DoPartition(array, startIndex, endIndex);

                if (partitionIndex > 1)
                    DoSort(array, startIndex, partitionIndex - 1);

                if (partitionIndex + 1 < endIndex)
                    DoSort(array, partitionIndex + 1, endIndex);
            }
        }

        /*
         * Method that partitions and sorts
         */
        private static int DoPartition(int[] array, int startIndex, int endIndex)
        {
            // Use median of three to get pivot, commented out for now 
            //int middle = (startIndex + endIndex) / 2;
            //int median = (startIndex + endIndex + middle) / 3;
            //int pivot = array[median];

            // Set pivot to a[5] as stated in Jie's paper
            int pivot = array[5];

            Mutex mLock = new Mutex();
            
            // Set chunk size
            var chunk = array.Length / _processorCount;

            // Initialize temporary arrays
            int[] temp = new int[array.Length];
            int[] nSmallerEqual = new int[chunk];
            int[] nGreaterThan = new int[chunk];

            // Set thread safe variables for unique ID 
            var queue = new ConcurrentQueue<int>();
            for (int i = 0; i <= chunk - 1; i++)
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
                int start = j * chunk; 
                int end = ((j + 1) * chunk) - 1;

                // Counter variables initialized with above start/end variables
                int lesser = start;
                int greater = end;
                
                // Check for less than or equal to and greater than pivot 
                for (int i = start; i <= end; i++)
                {
                    if (array[i] <= pivot)
                    {
                        mLock.WaitOne();
                        temp[lesser] = array[i];
                        mLock.ReleaseMutex();

                        lesser++;
                    }
                    else
                    {
                        mLock.WaitOne();
                        temp[greater] = array[i];
                        mLock.ReleaseMutex();

                        greater--;
                    }
                }

                mLock.WaitOne();
                nSmallerEqual[uniqueId] = lesser - start; // # elements smaller than pivot 
                nGreaterThan[uniqueId] = end - greater; // # elements greater than pivot 
                mLock.ReleaseMutex();

                queue.Enqueue(uniqueId);
            });


            return pivot;
        }

    }
}
