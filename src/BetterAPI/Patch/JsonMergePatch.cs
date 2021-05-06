using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BetterAPI.Patch
{
    public sealed class JsonMergePatch<T> : DynamicObject where T : class
    {
        private readonly HashSet<string> _changed;
        private readonly ITypeReadAccessor _reads;
        private readonly ITypeWriteAccessor _writes;
        private readonly AccessorMembers _members;
        private T _data;
        
        public JsonMergePatch() : this(typeof(T)) { }

        public JsonMergePatch(Type type)
        {
            _reads = ReadAccessor.Create(type, AccessorMemberTypes.Properties, AccessorMemberScope.Public, out _members);
            _writes = WriteAccessor.Create(type);
            _data = Activator.CreateInstance<T>();
            _changed = new HashSet<string>();
        }

        public IEnumerable<string> GetChangedMemberNames() => _changed;
        
        public bool TrySetPropertyValue(string name, object? value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name);
            _writes[_data, name] = value;
            _changed.Add(name);
            return true;
        }

        public bool TryGetPropertyValue(string name, out object? value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name);
            try
            {
                value = _reads[_data, name];
                return true;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            if (binder == null)
                throw new ArgumentNullException(nameof(binder));
            return TrySetPropertyValue(binder.Name, value);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            if (binder == null)
                throw new ArgumentNullException(nameof(binder));
            return TryGetPropertyValue(binder.Name, out result);
        }

        public void CopyChangedValues(T original)
        {
            var from = _data;
            var to = original;
            foreach (var property in GetChangedMemberNames())
                _writes[to, property] = _reads[from, property];
        }
        
        public void ApplyTo(T original, ModelStateDictionary? modelState = null)
        {
            CopyChangedValues(original);

            ValidateReadOnlyProperties(modelState);
        }

        private void ValidateReadOnlyProperties(ModelStateDictionary? modelState)
        {
            // assumes presence means value is true!
            foreach (var property in _members.Where(x => x.HasAttribute<ReadOnlyAttribute>()))
            {
                if (_changed.Contains(property.Name))
                {
                    modelState?.AddModelError(property.Name, $"ApplyTo is attempting to change '{property.Name}' to '{_reads[_data, property.Name]}', but it is defined as read only.");
                }
            }
        }
        
        public void Clear()
        {
            _data = Activator.CreateInstance<T>();
            _changed.Clear();
        }
    }
}