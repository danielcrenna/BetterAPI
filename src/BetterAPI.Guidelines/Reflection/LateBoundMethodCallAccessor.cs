using System;
using System.Reflection;

namespace BetterAPI.Guidelines.Reflection
{
    internal sealed class LateBoundMethodCallAccessor : MethodCallAccessor
    {
        private readonly Func<object, object[], object> _binding;

        public LateBoundMethodCallAccessor(MethodInfo method)
        {
            _binding = LateBinding.DynamicMethodBindCall(method);

            MethodName = method.Name;
            Parameters = method.GetParameters();
        }

        public override object Call(object target, object[] args)
        {
            return _binding(target, args);
        }
    }
}