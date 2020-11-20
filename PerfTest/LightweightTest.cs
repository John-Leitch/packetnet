using System.Reflection;
using System.Threading;

namespace PerfTest
{
    class LightweightTest
    {
        public ThreadLocal<object> Fixture { get; set; }
        public MethodInfo[] TestSetup { get; set; }
        public MethodInfo Test { get; set; }

        public LightweightTest(ThreadLocal<object> fixture, MethodInfo[] testSetup, MethodInfo test)
        {
            Fixture = fixture;
            TestSetup = testSetup;
            Test = test;
        }
    }
}
