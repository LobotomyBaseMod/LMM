using System.Collections.Generic;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools
{
    /// <summary>
    /// Represents a test run configuration. This record holds information about the test run environment and the tests to be executed.
    /// </summary>
    /// <param name="TestMode">
    /// The Test Mode for this test run.
    /// </param>
    /// <param name="TestPlatform">
    /// The Test Platform for this test run.
    /// </param>
    /// <param name="TestList">
    /// The list of tests to be executed.
    /// </param>
    public record TestData(TestMode TestMode, RuntimePlatform TestPlatform, IEnumerable<ITest> TestList);
}
