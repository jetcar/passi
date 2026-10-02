using System;

namespace PassiWebApiTests;

/// <summary>
/// Lazily creates and shares a single instance of <typeparamref name="T"/> across every caller,
/// building it from <paramref name="factory"/> the first time <see cref="GetOrCreate"/> is called.
/// </summary>
public class SharedTestResource<T> where T : class
{
    private readonly object _lock = new();
    private readonly Func<T> _factory;
    private T _value;

    public SharedTestResource(Func<T> factory)
    {
        _factory = factory;
    }

    public T Current => _value;

    public T GetOrCreate()
    {
        if (_value == null)
        {
            lock (_lock)
            {
                if (_value == null)
                {
                    _value = _factory();
                }
            }
        }

        return _value;
    }

    public void Reset()
    {
        lock (_lock)
        {
            _value = null;
        }
    }
}
