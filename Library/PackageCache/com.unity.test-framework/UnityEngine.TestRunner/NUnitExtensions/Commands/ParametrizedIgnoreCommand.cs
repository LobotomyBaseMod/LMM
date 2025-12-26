using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using System.Linq;

namespace UnityEngine.TestTools
{
    internal class ParametrizedIgnoreCommand : TestCommand
    {
        private readonly TestCommand m_Command;

        public object[] Arguments { get; }
        public string Reason { get; }

        public ParametrizedIgnoreCommand(TestCommand command, object[] arguments, string reason = "") : base(command.Test)
        {
            m_Command = command;
            Arguments = arguments;
            Reason = reason;
        }

        public override TestResult Execute(ITestExecutionContext context)
        {
            if (context.CurrentTest is TestMethod testMethod)
            {
                var isIgnored = testMethod.parms.Arguments.SequenceEqual(Arguments);
                if (isIgnored)
                {
                    context.CurrentResult.SetResult(ResultState.Ignored, Reason);
                    return context.CurrentResult;
                }
            }

            return m_Command.Execute(context);
        }
    }
}
