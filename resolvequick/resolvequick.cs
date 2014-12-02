using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Collections;
using System.IO;

namespace resolvequick
{
    // Useful way to store info that can be passed as a state on a work item
    public class SomeState
    {
        public string hostname;
        public SomeState(string iHostname)
        {
            hostname = iHostname;
        }
    }

    public class Alpha
    {
        public Hashtable HashCount;
        public ManualResetEvent eventX;
        public static int iCount = 0;
        public static int iMaxCount = 0;
        public Alpha(int MaxCount)
        {
            HashCount = new Hashtable(MaxCount);
            iMaxCount = MaxCount;
        }

        // Beta is the method that will be called when the work item is
        // serviced on the thread pool.
        // That means this method will be called when the thread pool has
        // an available thread for the work item.
        public void Beta(Object state)
        {
            IPHostEntry hostEntry;
            string hostname = ((SomeState)state).hostname;
            try
            {
                hostEntry = Dns.GetHostEntry(hostname);
                if (hostEntry.AddressList.Length > 0)
                {
                    foreach (System.Net.IPAddress ip in hostEntry.AddressList)
                    {
                        Console.WriteLine("[+] {0} - {1}", hostname, ip);
                    }
                }
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Console.WriteLine("[!] {0} - {1}", hostname, e.Message);
            }
            catch (System.ArgumentOutOfRangeException e)
            {
                Console.WriteLine("[!] {0} - {1}", hostname, e.Message);
            }

            int iX = 2000;
            Thread.Sleep(iX);
            // The Interlocked.Increment method allows thread-safe modification
            // of variables accessible across multiple threads.
            Interlocked.Increment(ref iCount);
            if (iCount == iMaxCount)
            {
                eventX.Set();
            }
        }
    }

    public class resolvequick
    {
        public static int Main(string[] args)
        {
            if (args.Length == 1)
            {
                string filename = args[0];
                FileInfo fInfo = new FileInfo(filename);
                if (!fInfo.Exists)
                {
                    Console.WriteLine("Specified file was not found: {0}", filename);
                }
                else
                {
                    ManualResetEvent eventX = new ManualResetEvent(false);

                    string[] lines = File.ReadAllLines(filename);
                    int linecount = lines.Length;

                    Alpha oAlpha = new Alpha(linecount);
                    oAlpha.eventX = eventX;
                    foreach (string hostname in lines)
                    {
                        // Queue the work items:
                        ThreadPool.QueueUserWorkItem(new WaitCallback(oAlpha.Beta), new SomeState(hostname));
                    }
                    Console.WriteLine("Waiting for Thread Pool to drain");
                    // The call to exventX.WaitOne sets the event to wait until
                    // eventX.Set() occurs.
                    // (See oAlpha.Beta).
                    // Wait until event is fired, meaning eventX.Set() was called:
                    eventX.WaitOne(Timeout.Infinite, true);
                    // The WaitOne won't return until the event has been signaled.
                    Console.WriteLine("Thread Pool has been drained (Event fired)");
                }
            }
            else
            {
                Console.WriteLine("Usage: resolvequick.exe <filename>\n\tFile should be a line delimited list of hostnames to resolve");
            }
            return 0;
        }
    }
}
