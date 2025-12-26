using System.Collections;
using UnityEditor.TestRunner.TestLaunchers;
using UnityEditor.TestTools.TestRunner.TestRun.Tasks;

namespace UnityEditor.TestTools.TestRunner.TestRun
{
    internal class WaitForPlayerRunTask : TestTaskBase
    {
        public override IEnumerator Execute(TestJobData testJobData)
        {
            var playerBuildOptions = testJobData.PlayerBuildOptions;
            if ((playerBuildOptions.options & BuildOptions.AutoRunPlayer) != BuildOptions.AutoRunPlayer)
            {
                yield break;
            }
            while (RemoteTestRunController.instance.isRunning)
            {
                yield return null;
            }
        }
    }
}
