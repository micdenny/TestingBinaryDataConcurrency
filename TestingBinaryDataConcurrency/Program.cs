using Newtonsoft.Json;
using Syroot.BinaryData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace TestingBinaryDataConcurrency
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new Application();
            app.SimulateConcurrency();
        }
    }

    public class Application
    {
        public void SimulateConcurrency()
        {
            var threadNumber = 10;

            var waitHandle = new ManualResetEvent(false);
            var countdown = new CountdownEvent(threadNumber);

            var binaries = new ConcurrentBag<byte[]>();

            var threads = new List<Thread>();
            for (int i = 0; i < threadNumber; i++)
            {
                var tNum = i + 1;
                var t = new Thread(() =>
                {
                    Console.WriteLine($"Thread {tNum} ready to fire!");
                    countdown.Signal();

                    // synch all threads
                    waitHandle.WaitOne();

                    Console.WriteLine($"[{tNum}]: Generating the binary...");

                    var binary = GetBinary();
                    binaries.Add(binary);

                    Console.WriteLine($"[{tNum}]: Done");
                })
                {
                    Name = "GetBinary()",
                    IsBackground = true
                };
                threads.Add(t);
                t.Start();
            }

            countdown.Wait();

            Console.WriteLine("When you're ready, fire in the hole by pressing enter...");
            Console.ReadLine();

            waitHandle.Set();

            foreach (var thread in threads)
            {
                thread.Join();
            }

            Console.WriteLine($"{binaries.Count} binaries has been generated.");
        }

        public void VerySimpleTest()
        {
            var binary = GetBinary();
            Console.WriteLine(Encoding.ASCII.GetString(binary));

            var obj = GetObject(binary);
            Console.WriteLine(JsonConvert.SerializeObject(obj, Formatting.Indented));
        }

        private byte[] GetBinary()
        {
            using (var stream = new MemoryStream())
            {
                var value = new MyWonderfulClass
                {
                    Id = 123,
                    Descr = Guid.NewGuid().ToString(),
                    Price = 12.123m,
                    Quantity = 12
                };
                stream.WriteObject(value, ByteConverter.Big);
                return stream.ToArray();
            }
        }

        private MyWonderfulClass GetObject(byte[] value)
        {
            using (var stream = new MemoryStream(value))
            {
                return stream.ReadObject<MyWonderfulClass>(ByteConverter.Big);
            }
        }
    }

    public class MyWonderfulClass
    {
        public long Id { get; set; }
        public string Descr { get; set; }
        public short Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
