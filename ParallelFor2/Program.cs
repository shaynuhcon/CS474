using System;
using System.Collections.Generic;
using System.Diagnostics;  // for the stop watch
using System.Threading.Tasks;    // for parallel for

namespace ParallelFor2
{
    class Program
    {
        #region Sequential_Loop
        static void MultiplyMatricesSequential(double[,] matA, double[,] matB,
                                                double[,] result)
        {
            int matACols = matA.GetLength(1);
            int matBCols = matB.GetLength(1);
            int matARows = matA.GetLength(0);

            for (int i = 0; i < matARows; i++)
            {
                for (int j = 0; j < matBCols; j++)
                {
                    for (int k = 0; k < matACols; k++)
                    {
                        result[i, j] += matA[i, k] * matB[k, j];
                    }
                }
            }
        }
        #endregion

        #region Parallel_Loop
        // no control of the task size
        static void MultiplyMatricesParallel(double[,] matA, double[,] matB, double[,] result)
        {
            int matACols = matA.GetLength(1);
            int matBCols = matB.GetLength(1);
            int matARows = matA.GetLength(0);

            // A basic matrix multiplication.
            // Parallelize the outer loop to partition the source array by rows.
            Parallel.For(0, matARows, i =>
            {
                for (int j = 0; j < matBCols; j++)
                {
                    // Use a temporary to improve parallel performance.
                    double temp = 0;
                    for (int k = 0; k < matACols; k++)
                    {
                        temp += matA[i, k] * matB[k, j];
                    }
                    result[i, j] = temp;
                }

            }); // Parallel.For

        }

        // task size is controlled by the value of chunk, the high value the larger the tasks
        static void MultiplyMatricesParallelChunk(double[,] matA, double[,] matB, double[,] result, int chunk)
        {
            int matACols = matA.GetLength(1);
            int matBCols = matB.GetLength(1);
            int matARows = matA.GetLength(0);

            // A basic matrix multiplication.
            // Parallelize the outer loop to partition the source array by chunk of rows.
            Parallel.For(0, matARows / chunk, ii =>
            {
                int nSIZE = (ii + 1) * chunk;
                for (int i = ii * chunk; i < nSIZE; i++)
                {
                    for (int j = 0; j < matBCols; j++)
                    {
                        // Use a temporary to improve parallel performance.
                        double temp = 0;
                        for (int k = 0; k < matACols; k++)
                        {
                            temp += matA[i, k] * matB[k, j];
                        }
                        result[i, j] = temp;
                    }
                }
            }); // Parallel.For

        }


        #endregion


        #region Main
        static void Main(string[] args)
        {
            // Set up matrices. Use small values to better view 
            // result matrix. Increase the counts to see greater 
            // speedup in the parallel loop vs. the sequential loop.
            int colCount = 1024;
            int rowCount = colCount;
            int colCount2 = colCount;
            double[,] m1 = InitializeMatrix(rowCount, colCount);
            double[,] m2 = InitializeMatrix(colCount, colCount2);
            double[,] result = new double[rowCount, colCount2];

            // First do the sequential version.
            Console.WriteLine("Executing sequential loop...");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            MultiplyMatricesSequential(m1, m2, result);
            stopwatch.Stop();
            Console.WriteLine("Sequential loop time in milliseconds: {0}", stopwatch.ElapsedMilliseconds);

            // For the skeptics.
            PrintCheckSum(rowCount, colCount2, result);

            // Reset timer and results matrix. 
            stopwatch.Reset();
            result = new double[rowCount, colCount2];

            // Do the parallel loop.
            Console.WriteLine("Executing parallel loop...");
            stopwatch.Start();
            MultiplyMatricesParallel(m1, m2, result);
            stopwatch.Stop();
            Console.WriteLine("Parallel loop time in milliseconds: {0}", stopwatch.ElapsedMilliseconds);
            PrintCheckSum(rowCount, colCount2, result);
            result = new double[rowCount, colCount2];

            for (int chunk = 2; chunk <= 512; chunk *= 2)
            {
                // Do the parallel loop with chunk 
                Console.WriteLine("Executing parallel loop chunk == {0}...", chunk);
                stopwatch.Reset();
                stopwatch.Start();
                MultiplyMatricesParallelChunk(m1, m2, result, chunk);
                stopwatch.Stop();
                Console.WriteLine("Parallel loop time in milliseconds: {0}", stopwatch.ElapsedMilliseconds);

                PrintCheckSum(rowCount, colCount2, result);
            }

            // Keep the console window open in debug mode.
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }


        #endregion

        #region Helper_Methods

        static double[,] InitializeMatrix(int rows, int cols)
        {
            double[,] matrix = new double[rows, cols];

            Random r = new Random();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    matrix[i, j] = r.Next(100);
                }
            }
            return matrix;
        }

        private static void PrintCheckSum(int rowCount, int colCount, double[,] matrix)
        {
            double dSum = 0;
            Console.WindowWidth = 150;
            Console.WriteLine();
            for (int x = 0; x < rowCount; x++)
            {
                for (int y = 0; y < colCount; y++)
                {
                    dSum += (long)matrix[x, y];
                }
            }
            Console.WriteLine("{0:#.##} ", dSum);
        }
        #endregion
    }

}
