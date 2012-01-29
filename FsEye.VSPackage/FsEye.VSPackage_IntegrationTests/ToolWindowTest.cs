using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VsSDK.IntegrationTestLibrary;
using Microsoft.VSSDK.Tools.VsIdeTesting;

namespace FsEye.VSPackage_IntegrationTests {

    [TestClass()]
    public class ToolWindowTest {
        private delegate void ThreadInvoker();

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }

        /// <summary>
        ///A test for showing the toolwindow
        ///</summary>
        [TestMethod()]
        [HostType("VS IDE")]
        public void ShowToolWindow() {
            UIThreadInvoker.Invoke((ThreadInvoker)delegate() {
                CommandID toolWindowCmd = new CommandID(StephenSwensen.FsEye_VSPackage.GuidList.guidFsEye_VSPackageCmdSet, (int)StephenSwensen.FsEye_VSPackage.PkgCmdIDList.cmdFsEye);

                TestUtils testUtils = new TestUtils();
                testUtils.ExecuteCommand(toolWindowCmd);

                Assert.IsTrue(testUtils.CanFindToolwindow(new Guid(StephenSwensen.FsEye_VSPackage.GuidList.guidToolWindowPersistanceString)));

            });
        }

    }
}
