using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal.Commands;
using System;

namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ParametrizedIgnoreAttribute : NUnitAttribute, IWrapTestMethod
    {
        public object[] Arguments { get; }
        public string Reason { get; set; }

        public ParametrizedIgnoreAttribute(params object[] Arguments)
        {
            this.Arguments = Arguments;
        }

        public TestCommand Wrap(TestCommand command)
        {
            return new ParametrizedIgnoreCommand(command, Arguments, Reason);
        }
    }
}
