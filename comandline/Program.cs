using Interprocomm;
using System;
using System.Threading;

namespace comandline
{
    internal class Program
    {
        #region Private Fields

        private static Mutex mutex;

        #endregion Private Fields

        #region Private Methods

        private static void Main(string[] args)
        {
            bool created;
            mutex = new Mutex(true, "{7b1a161b-30cc-4679-856b-30953873033d}", out created);
            if (created)
            {
                var server = new Server("{7b1a161b-30cc-4679-856b-30953873033d}");
                //server code...
            }
            else
            {
                var client = new Client("{7b1a161b-30cc-4679-856b-30953873033d}");
                //client code...
            }
        }

        #endregion Private Methods
    }
}