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
        private static readonly Mutex LockObject = new Mutex();
        private static readonly int CoreCount = Environment.ProcessorCount;
        private static readonly int _size = 1000000000;

        static void Main(string[] args)
        {
            double x, y;
            var array = new int[CoreCount];
            int nCountIn = _size * 4;

            Parallel.For(0, CoreCount, i =>
            {
                for (int j = 0; j < _size / CoreCount; j++)
                {
                    Random rand = new Random();

                    x = rand.NextDouble();
                    y = rand.NextDouble();
                    if (x * x + y * y >= 1)
                    {
                        array[i]++;
                    }

                    LockObject.WaitOne();
                    nCountIn -= array[i];
                    LockObject.ReleaseMutex();
                }
            });

            Console.WriteLine(nCountIn * 1.0 / _size);
            Console.ReadLine();
        }
    }
}
