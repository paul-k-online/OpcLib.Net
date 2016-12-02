using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpcLib.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            try
            {
                object n = null;
                var s = Convert.ToString(n);
            }
            catch (Exception)
            {
                
                throw;
            }
            


        }
    }
}
