using System;

namespace BetterAPI.Reflection
{
	public interface ITypeWriteAccessor : IWriteAccessor
	{
		Type Type { get; }
	}
}