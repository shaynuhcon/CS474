using System;
using System.Threading.Tasks;

namespace ParallelProgrammingDemo
{
    class Program
    {
        static void Main()
        {
            DateTime StartDateTime = DateTime.Now;

            Console.WriteLine(@"Parallel For Loop Execution start at : {0}", StartDateTime);

            Parallel.For(0, 10, i => {
                long total = DoSomeIndependentTask();
                Console.WriteLine("{0} - {1}", i, total);
            });

            DateTime EndDateTime = DateTime.Now;
            Console.WriteLine(@"Parallel For Loop Execution end at : {0}", EndDateTime);

            TimeSpan span = EndDateTime - StartDateTime;
            int ms = (int)span.TotalMilliseconds;
            Console.WriteLine(@"Time Taken to Execute the Loop in miliseconds {0}", ms);

            Console.WriteLine("Press any key to exist");
            Console.ReadLine();
        }

        static long DoSomeIndependentTask()
        {
            //Do Some Time Consuming Task here
            //Most Probably some calculation or DB related activity
            long total = 0;
            for (int i = 1; i < 100000000; i++)
            {
                total += i;
            }
            return total;
        }
    }
}