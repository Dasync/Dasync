using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Dasync.ValueContainer
{
    public sealed class ValueContainerEnumerable : IEnumerable<NamedValue>
    {
        private readonly IValueContainer _container;

        public ValueContainerEnumerable(IValueContainer container)
            => _container = container ?? throw new ArgumentNullException(nameof(container));

        public IEnumerator<NamedValue> GetEnumerator()
            => new ValueContainerEnumerator(_container);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class ValueContainerEnumerator : IEnumerator<NamedValue>
    {
        private IValueContainer _container;
        private int _index;

        public ValueContainerEnumerator(IValueContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            Reset();
        }

        public void Reset()
        {
            if (_container == null)
                throw new ObjectDisposedException(nameof(ValueContainerEnumerator));
            _index = -1;
        }

        public bool MoveNext()
        {
            if (_container == null)
                throw new ObjectDisposedException(nameof(ValueContainerEnumerator));
            _index++;
            return _index < _container.GetCount();
        }

        public NamedValue Current
        {
            get
            {
                if (_container == null)
                    throw new ObjectDisposedException(nameof(ValueContainerEnumerator));
                if (_index < 0)
                    throw new InvalidOperationException("Enumeration has not started.");
                if (_index >= _container.GetCount())
                    throw new InvalidOperationException("Enumeration has reached the end.");
                return new NamedValue
                {
                    Name = _container.GetName(_index),
                    Value = _container.GetValue(_index),
                    Type = _container.GetType(_index),
                    Index = _index
                };
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            _container = null;
        }
    }
}
