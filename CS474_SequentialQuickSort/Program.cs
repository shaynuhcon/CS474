using System;
using System.Diagnostics;

namespace CS474_SequentialQuickSort
{
    class Program
    {
        static void QuickSort(int[] arr, int low, int high)
        {
            if (low < high)
            {

                /* pi is partitioning index, arr[pi] is  
                now at right place */
                int pi = Partition(arr, low, high);

                // Recursively sort elements before 
                // partition and after partition 
                QuickSort(arr, low, pi - 1);
                QuickSort(arr, pi + 1, high);
            }
        }

        static int Partition(int[] arr, int low,
            int high)
        {
            int pivot = arr[high];

            // index of smaller element 
            int i = (low - 1);
            for (int j = low; j < high; j++)
            {
                // If current element is smaller  
                // than or equal to pivot 
                if (arr[j] <= pivot)
                {
                    i++;

                    // swap arr[i] and arr[j] 
                    int temp = arr[i];
                    arr[i] = arr[j];
                    arr[j] = temp;
                }
            }

            // swap arr[i+1] and arr[high] (or pivot) 
            int temp1 = arr[i + 1];
            arr[i + 1] = arr[high];
            arr[high] = temp1;

            return i + 1;
        }

        private static void FillArrayWithRandomValues(int[] array)
        {
            var random = new Random();
            for (var i = 0; i < array.Length; i++) array[i] = random.Next(0, 100);
        }

        public static bool IsSorted(int[] arr)
        {
            for (int i = 1; i < arr.Length; i++)
            {
                if (arr[i - 1] > arr[i])
                {
                    return false;
                }
            }
            return true;
        }

        static void Main()
        {
            int[] array = new int[10000];
            FillArrayWithRandomValues(array);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            QuickSort(array, 0, array.Length - 1);

            stopwatch.Stop();
            var elapsedTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();
            
            if (IsSorted(array))
            {
                Console.WriteLine("Array sorted sequentially in {0} ms", elapsedTime);
            }
            else
            {
                Console.WriteLine("Array is not sorted");
            }

            Console.ReadLine();
        }
    }
}
