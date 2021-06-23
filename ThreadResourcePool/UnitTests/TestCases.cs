using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace ThreadResourcePool.UnitTests
{
    [TestFixture]
    public class TestCases
    {
        private readonly List<int> result = new List<int>();
        private readonly Random random = new Random();
        private volatile int counter = 0;

        [Test]
        public void TestPool1()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            IThreadResourcePool p = new ThreadResourcePool(cancellationTokenSource.Token);
            p.AddResource("A");
            p.AddResource("B");
            p.AddResource("C");


            p.QueueUserWorkItem(ProcessRun, 1, new List<string>() { "A" });
            p.QueueUserWorkItem(ProcessRun, 2, new List<string>() { "B" });
            p.QueueUserWorkItem(ProcessRun, 3, new List<string>() { "A", "B" });
            p.QueueUserWorkItem(ProcessRun, 4, new List<string>() { "C" });
            p.QueueUserWorkItem(ProcessRun, 5, new List<string>() { "B" });
            //This one has chance to be scheduled before P3 if P2 released very early, this is a pool rather than a blocked sequence so that P should be scheduled at any first chance

            while (result.Count < 5)
            {
                Thread.Sleep(1);
            }

            Assert.IsTrue(result.Take(3).Sum() == 1 + 2 + 4); //No sequence gurantee for P1, P2 and P3 because this is a pool

            Assert.IsTrue(result[3] + result[4] == 3 + 5); //If P2 released first the P5 will be scheduled before P3, so no gurantee between P3 and P5

            Assert.IsTrue(result.Count == 5);

            result.Clear();

            cancellationTokenSource.Cancel();
        }

        [Test]
        public void TestPoolBenchmark10K()
        {
            int _base = 50000;
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            IThreadResourcePool p = new ThreadResourcePool(cancellationTokenSource.Token);
            p.AddResource("A");
            p.AddResource("B");
            p.AddResource("C");

            for (int t = 0; t < _base; t += 5) //In real prod may need to test millions times to verify if this is true
            {
                p.QueueUserWorkItem(ProcessRunSlim, t + 1, new List<string>() { "A" });
                p.QueueUserWorkItem(ProcessRunSlim, t + 2, new List<string>() { "B" });
                p.QueueUserWorkItem(ProcessRunSlim, t + 3, new List<string>() { "A", "B" });
                p.QueueUserWorkItem(ProcessRunSlim, t + 4, new List<string>() { "C" });
                p.QueueUserWorkItem(ProcessRunSlim, t + 5, new List<string>() { "B" });
            }

            while (counter < _base)
            {
                Thread.Sleep(100);
            }

            Assert.IsTrue(counter == _base);
        }

        private void ProcessRunSlim(object state)
        {
            Interlocked.Increment(ref counter);
            Console.WriteLine(state);
        }

        private void ProcessRun(object state)
        {
            int pCode = (int)state;
            Thread.Sleep(random.Next(1, 10));

            if (pCode <= 2)
            {
                Thread.Sleep(10 * pCode); // Force P1 longer than P2
            }

            lock (result)
            {
                result.Add(pCode);
            }
            Console.WriteLine(state);
        }
    }
}
