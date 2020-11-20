using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace PerfTest
{

    class Program
    {
        const string _actualTests = @"
            AcknowledgmentFrameTest [3]
            ActionFrameTest [3]
            ArpPacketTest [4]
            AssociationRequestFrameTest [3]
            AssociationResponseFrameTest [3]
            AuthenticationFrameTest [4]
            BeaconFrameTest [3]
            BitConversionPerformance [3]
            BlockAcknowledgmentControlFieldTest [4]
            BlockAcknowledgmentFrameTest [3]
            BlockAcknowledgmentRequestFrameTest [3]
            ByteArraySegmentTest [1]
            ByteCopyPerformance [2]
            ByteRetrievalPerformance [2]
            CapabilityInformationFieldTest [20]
            ChecksumUtils [1]
            ConstructingPacketsTest [1]
            ContentionFreeEndFrameTest [3]
            CtsFrameTest [3]
            DataDataFrameTest [4]
            DeauthenticationFrameTest [3]
            DhcpV4PacketTest [2]
            DisassociationFrameTest [3]
            DrdaPacketTest [6]
            EthernetPacketTest [6]
            FrameControlFieldTest [17]
            GreIPv6PacketTest [1]
            IcmpV4PacketTest [4]
            IcmpV6PacketTest [4]
            IgmpV2PacketTest [3]
            InformationElementListTest [13]
            InformationElementTest [8]
            IPPacketTest [3]
            IPv4PacketTest [5]
            IPv6PacketTest [9]
            L2tpPacketTest [1]
            LinkTypeNullCaptureTest [3]
            LinuxCookedCaptureTest [3]
            LldpTest [5]
            MacFrameTest [8]
            NullDataFrameTest [3]
            OspfV2PacketTest [18]
            PacketTest [1]
            PerPacketInformationTest [11]
            PpiFieldsTests [9]
            PppoePppTest [3]
            PppPacketTest [2]
            ProbeRequestTest [3]
            ProbeResponseTest [3]
            QosDataFrameTest [4]
            QosNullDataFrameTest [3]
            RadioPacketTest [7]
            RadioTapFieldsTest [15]
            RawPacketTest [3]
            ReassociationRequestFrameTest [3]
            SequenceControlFieldTest [4]
            StringOutputTest [4]
            TcpPacketTest [10]
            TeredoPacketTest [1]
            UdpTest [7]
            Vlan802_1QTest [1]
            WakeOnLanTest [6]
        ";

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SYMBOLIC_LINK_FLAG dwFlags);

        [Flags]
        enum SYMBOLIC_LINK_FLAG
        {
            File = 0,
            Directory = 1,
            AllowUnprivilegedCreate = 2
        }

        static string FindTestDir()
        {
            var root = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var dir = root;

            while (true)
            {
                var search = Path.Combine(dir, "Test", "CaptureFiles");

                Console.WriteLine($"Searching {search} for test caps");

                if (Directory.Exists(search))
                {
                    Console.WriteLine("Found test caps");
                    return search;
                }

                if ((dir = Path.GetDirectoryName(dir)) == null)
                {
                    throw new InvalidOperationException($"Could not find test caps relative to {root}.");
                }
            }
        }

        static void Init()
        {
            AppDomain.CurrentDomain.UnhandledException += (o, e) => Console.WriteLine(e.ExceptionObject);

            var testDir = FindTestDir();
            var symlink = Path.Combine(testDir, "..", "..", "PerfTest", "CaptureFiles");

            if (!Directory.Exists(symlink))
            {
                Console.WriteLine($"Creating symlink {symlink}");

                if (!CreateSymbolicLink(
                    symlink,
                    testDir,
                    SYMBOLIC_LINK_FLAG.Directory | SYMBOLIC_LINK_FLAG.AllowUnprivilegedCreate))
                {
                    throw new Win32Exception();
                }
            }
        }

        static LightweightTest[] LoadTests() =>
            typeof(Test.Performance.BitConversionPerformance)
                .Assembly
                .GetTypes()
                .Where(x => x.GetCustomAttribute<NUnit.Framework.TestFixtureAttribute>() != null)
                .Select(x => new
                {
                    Fixture = x.IsAbstract && x.IsSealed ?
                        null :
                        new ThreadLocal<object>(() =>
                        {
                            var fixture = Activator.CreateInstance(x);

                            foreach (var fixtureSetup in x
                                .GetMethods()
                                .Where(x => x.GetCustomAttribute<NUnit.Framework.SetUpFixtureAttribute>() != null))
                            {
                                fixtureSetup.Invoke(fixture, Array.Empty<object>());
                            }

                            return fixture;
                        }),
                    TestSetup = x.GetMethods().Where(x => x.GetCustomAttribute<NUnit.Framework.SetUpAttribute>() != null).ToArray(),
                    Tests = x
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                        .Where(x => x.GetCustomAttribute<NUnit.Framework.TestAttribute>() != null)
                        .ToArray()
                })
                .SelectMany(x => x.Tests.Select(y => new LightweightTest(x.Fixture, x.TestSetup, y)))
                .ToArray();

        static void RunTests(LightweightTest[] tests)
        {
            var count = 0;
            foreach (var t in tests)
            {
                foreach (var m in t.TestSetup)
                {
                    Console.WriteLine($"Calling test setup method {m.Name}");
                    m.Invoke(t.Fixture.Value, Array.Empty<object>());
                }

                Console.WriteLine("Calling test method {0}", t.Test.Name);
                t.Test.Invoke(t.Test.IsStatic ? null : t.Fixture.Value, Array.Empty<object>());
                count++;
            }
            Console.WriteLine("\r\n{0:n0} tests executed\r\n", count);
        }

        static void DumpTestInfo()
        {
            var tests = LoadTests().OrderBy(x => x.Test.DeclaringType.Name);

            Console.WriteLine("Loaded {0:n0} tests", tests.Count());
            var _remaining = _actualTests;

            foreach (var g in tests.GroupBy(x => x.Test.DeclaringType).OrderBy(x => x.Key.Name))
            {
                var fixtureCount = string.Format("{0} [{1:n0}]", g.Key.Name, g.Count());
                _remaining = _remaining.Replace(string.Format("    {0}\r\n", fixtureCount), "");
                Console.WriteLine(fixtureCount);

                foreach (var t in g)
                {
                    Console.WriteLine($"  {t.Test.Name}");
                }

                Console.WriteLine();
            }

            Console.WriteLine(
                "Missing:\r\n{0}",
                string.Join(
                    "\r\n",
                    _remaining
                        .Split("\r\n".ToCharArray())
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x))));
        }

        static void Main(string[] args)
        {

            Init();
            //DumpTestInfo();


            for (var i = 0; i < 10; i++)
            {
                RunTests(LoadTests());
            }
        }
    }
}
