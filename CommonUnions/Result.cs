using System;

namespace Scifa.UnionTypes;

public static class Result
{
    public static Result<T, Exception> TryExecute<T>(Func<T> function)
        => TryExecute<T, Exception>(function);

    public static Result<T, TException> TryExecute<T, TException>(Func<T> function)
        where TException : Exception
    {
        try
        {
            return Result<T, TException>.Ok(function());
        }
        catch (TException ex)
        {
            return Result<T, TException>.Error(ex);
        }
    }

    public static Result<Unit, Exception> TryExecute(Action function)
        => TryExecute<Exception>(function);

    public static Result<Unit, TException> TryExecute<TException>(Action function)
        where TException : Exception
    {
        try
        {
            function();
            return Result<Unit, TException>.Ok(Unit.Instance);
        }
        catch (TException ex)
        {
            return Result<Unit, TException>.Error(ex);
        }
    }

    public static class WithErrorType<TError>
    {
        public static Result<T, TError> Ok<T>(T value) => Result<T, TError>.Ok(value);
    }
    public static class WithType<T>
    {
        public static Result<T, TError> Error<TError>(TError error) => Result<T, TError>.Error(error);
    }
}

[UnionType]
public readonly partial record struct Result<T, TError>
{
    public static partial Result<T, TError> Ok(T value);
    public static partial Result<T, TError> Error(TError value);

    public Result<U, TError> Bind<U>(Func<T, Result<U, TError>> bind)
    {
        return Match(
                ok: x => bind(x),
                error: Result.WithType<U>.Error
              );
    }

    public Result<T, UError> BindError<UError>(Func<TError, Result<T, UError>> bind)
        => Match(
            ok: Result.WithErrorType<UError>.Ok,
            error: err => bind(err));
}
