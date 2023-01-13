using System.Runtime.CompilerServices;

namespace Atheon;

public static class Ensure
{
    public readonly ref struct EnsureWrapper<T>
    {
        public T? Value { get; init; }
        public string? ParamName { get; init; }

        public T Is(Func<T?, bool> predicate, string errorMessage)
        {
            if (Value is null)
            {
                throw new ArgumentNullException(ParamName, errorMessage);
            }

            try
            {
                if (predicate(Value))
                {
                    return Value!;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(errorMessage, ex);
            }

            throw new Exception(errorMessage);
        }
    }

    public static EnsureWrapper<T> That<T>(T? instance, [CallerMemberName] string? paramName = null)
    {
        return new EnsureWrapper<T>()
        {
            Value = instance,
            ParamName = paramName
        };
    }
}
