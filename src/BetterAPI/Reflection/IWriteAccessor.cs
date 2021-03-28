namespace BetterAPI.Reflection
{
	public interface IWriteAccessor
	{
		object this[object target, string key] { set; }
		bool TrySetValue(object? target, string key, object value);
	}
}