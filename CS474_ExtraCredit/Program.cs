using System;
using System.Threading;
using System.Threading.Tasks;

/**
 * Name: Shayna Conner
 * Date: 5/13/2019
 * CS474 - Exam Extra Credit
 */

namespace CS474_ExtraCredit
{
    class Program
    {
        // Global, readonly variables
        private static readonly Mutex LockObject = new Mutex();
        private static readonly int CoreCount = 4;
        private static readonly double _size = 1000000000;
        
        static void Main(string[] args)
        {
            // Initialize variables
            var array = new int[CoreCount];
            double nCountIn = 0;

            // Parallel body that counts number of pairs with distance less than 1
            Parallel.For(0, CoreCount, i =>
            {
                Random rand = new Random();

                for (int j = 0; j < _size / CoreCount; j++)
                {
                    double x = rand.NextDouble();
                    double y = rand.NextDouble();

                    if (Math.Sqrt(x * x + y * y) <= 1)
                    {
                        array[i]++;
                    }
                }
                
                // Lock count variable 
                LockObject.WaitOne();
                nCountIn += array[i];
                LockObject.ReleaseMutex();
            });

            // Get approximation of pi and print
            var pi = (nCountIn / _size) * 4;
            Console.WriteLine(pi);
            Console.ReadLine();
        }
    }
}
