using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Scifa.UnionTypes.Result;

namespace Scifa.UnionTypes;


public static class Result
{
    /// <summary>
    /// Creates a new instance of <see cref="UntypedOk{T}"/> with the specified value.
    /// This can be implictly cast to <see cref="Result{T, TError}"/> for any error type.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="value"/></typeparam>
    /// <param name="value">The value</param>
    /// <returns>An object that can be implicitly cast to an instance of <see cref="Result{T, TError}"/> regardless of the error type.</returns>
    public static UntypedOk<T> Ok<T>(T value) => new(value);

    /// <summary>
    /// Creates a new instance of <see cref="UntypedOk{T}"/> with the specified value.
    /// This can be implictly cast to <see cref="Result{T, TError}"/> for any value type.
    /// </summary>
    /// <typeparam name="TError">The type of <paramref name="error"/></typeparam>
    /// <param name="error">Details of the error that occurred</param>
    /// <returns>An object that can be implicitly cast to an instance of <see cref="Result{T, TError}"/> regardless of the value type.</returns>
    public static UntypedError<TError> Error<TError>(TError error) => new(error);

    /// <summary>
    /// Execute a given function catching any exceptions that occur and returning them as an <see cref="Result{T, TError}"/> with the error type
    /// set to <see cref="ExceptionDispatchInfo"/>. It is expected that you use <see cref="Result{T, TError}.MapError{TError2}(Func{TError, TError2})"/>
    /// or equivalent to convert the <see cref="ExceptionDispatchInfo"/> into an alternative error type
    /// </summary>
    /// <typeparam name="T">The return type of <paramref name="action"/></typeparam>
    /// <param name="action">The function to execute</param>
    /// <returns>An instance of <see cref="Result{T, TError}"/> containing either the result of <paramref name="action"/> or an instance of
    /// <see cref="ExceptionDispatchInfo"/> wrapping the exception thrown from <paramref name="action"/>.</returns>
    public static Result<T, ExceptionDispatchInfo> Catch<T>(Func<T> action)
        => Catch(action, _ => true);

    /// <summary>
    /// Execute a given function catching any exceptions that occur and returning them as an <see cref="Result{T, TError}"/> with the error type
    /// set to <see cref="ExceptionDispatchInfo"/>. It is expected that you use <see cref="Result{T, TError}.MapError{TError2}(Func{TError, TError2})"/>
    /// or equivalent to convert the <see cref="ExceptionDispatchInfo"/> into an alternative error type
    /// </summary>
    /// <typeparam name="T">The return type of <paramref name="action"/></typeparam>
    /// <param name="action">The function to execute</param>
    /// <param name="filter">A filter which limits the types of exceptions that will be caught and converted to <see cref="Result{T, TError}"/>.
    /// Any exception _not_ matching this filter will remain uncaugt and will propagate to the caller</param>
    /// <returns>An instance of <see cref="Result{T, TError}"/> containing either the result of <paramref name="action"/> or an instance of 
    /// <see cref="ExceptionDispatchInfo"/> wrapping the exception thrown from <paramref name="action"/>.</returns>
    public static Result<T, ExceptionDispatchInfo> Catch<T>(Func<T> action, Predicate<Exception> filter)
    {
        T result;
        try
        {
            result = action();
        }
        catch (Exception ex) when (filter(ex))
        {
            return Error(ExceptionDispatchInfo.Capture(ex));
        }

        return Ok(result);
    }

    /// <summary>
    /// Execute a given function catching any exceptions that occur and returning them as an <see cref="Result{T, TError}"/> with the error type
    /// set to <see cref="ExceptionDispatchInfo"/>. It is expected that you use <see cref="Result{T, TError}.MapError{TError2}(Func{TError, TError2})"/>
    /// or equivalent to convert the <see cref="ExceptionDispatchInfo"/> into an alternative error type
    /// </summary>
    /// <typeparam name="T">The return type of <paramref name="action"/></typeparam>
    /// <param name="action">The function to execute</param>
    /// <returns>A task containing an instance of <see cref="Result{T, TError}"/> containing either the result of <paramref name="action"/> or an instance of
    /// <see cref="ExceptionDispatchInfo"/> wrapping the exception thrown from <paramref name="action"/>.</returns>
    public static Task<Result<T, ExceptionDispatchInfo>> CatchAsync<T>(Func<Task<T>> action)
        => CatchAsync(action, _ => true);

    /// <summary>
    /// Execute a given function catching any exceptions that occur and returning them as an <see cref="Result{T, TError}"/> with the error type
    /// set to <see cref="ExceptionDispatchInfo"/>. It is expected that you use <see cref="Result{T, TError}.MapError{TError2}(Func{TError, TError2})"/>
    /// or equivalent to convert the <see cref="ExceptionDispatchInfo"/> into an alternative error type
    /// </summary>
    /// <typeparam name="T">The return type of <paramref name="action"/></typeparam>
    /// <param name="action">The function to execute</param>
    /// <param name="filter">A filter which limits the types of exceptions that will be caught and converted to <see cref="Result{T, TError}"/>.
    /// Any exception _not_ matching this filter will remain uncaugt and will propagate to the caller</param>
    /// <returns>A task containing an instance of <see cref="Result{T, TError}"/> containing either the result of <paramref name="action"/> or an instance of 
    /// <see cref="ExceptionDispatchInfo"/> wrapping the exception thrown from <paramref name="action"/>.</returns>
    public static async Task<Result<T, ExceptionDispatchInfo>> CatchAsync<T>(Func<Task<T>> action, Predicate<Exception> filter)
    {
        T result;
        try
        {
            result = await action();
        }
        catch (Exception ex) when (filter(ex))
        {
            return Error(ExceptionDispatchInfo.Capture(ex));
        }

        return Ok(result);
    }

    /// <summary>
    /// Convert an enumerable of results into a single result containing an array of all the values or the first error that occurred
    /// </summary>
    /// <typeparam name="T">The type of the value contained in the <see cref="Result{T, TError}"/> instances.</typeparam>
    /// <typeparam name="TError">The type of errors that may be contained in the <see cref="Result{T, TError}"/> instances.</typeparam>
    /// <param name="results">The collection of results</param>
    /// <returns>A single result containing all the values of the reuslts or the first error result if any failed.</returns>
    public static Result<T[], TError> Collect<T, TError>(this IEnumerable<Result<T, TError>> results)
        => results.Aggregate(
            Result<IEnumerable<T>, TError>.Ok([]),
            (acc, result) => acc.Bind(accValue => result.Map(value => accValue.Append(value))),
            acc => acc.Map(values => values.ToArray())
        );

    /// <summary>
    /// A utility type that permits create an instance of <see cref="Result{T, TError}"/> without specifying the type of the error.
    /// </summary>
    public record class UntypedOk<T>(T Value);

    /// <summary>
    /// A utility type that permits create an instance of <see cref="Result{T, TError}"/> without specifying the type of the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="Value"></param>
    public record class UntypedError<T>(T Value);
}

[UnionType]
public partial record Result<T, TError>
{
    public static partial Result<T, TError> Ok(T value);
    public static partial Result<T, TError> Error(TError value);

    public void Deconstruct(out T? value, out TError? err) => (value, err) = Match(ok: v => (v, default(TError)), error: e => (default(T), e));

    public Result<T, TError> Tee(Action<T> teeOk)
    {
        Do(v => teeOk(v),
           _ => { });
        return this;
    }

    public Result<T2, TError2> Map<T2, TError2>(Func<T, T2> mapOk, Func<TError, TError2> mapError)
        => Match(
            ok: v => Result<T2, TError2>.Ok(mapOk(v)),
            error: e => Result<T2, TError2>.Error(mapError(e)));

    public Result<T2, TError> Map<T2>(Func<T, T2> mapOk)
        => Match(
            ok: value => Result<T2, TError>.Ok(mapOk(value)),
            error: Result<T2, TError>.Error
        );

    public Result<T, TError2> MapError<TError2>(Func<TError, TError2> mapError)
        => Match(
            ok: Result<T, TError2>.Ok,
            error: value => Result<T, TError2>.Error(mapError(value))
    );

    public Result<T2, TError2> Bind<T2, TError2>(Func<T, Result<T2, TError2>> bind, Func<TError, Result<T2, TError2>> bindError)
        => Match(bind, bindError);

    public Result<T2, TError> Bind<T2>(Func<T, Result<T2, TError>> bind)
        => Match(
            ok: bind,
            error: Result<T2, TError>.Error
        );

    public Result<T, TError2> BindError<TError2>(Func<TError, Result<T, TError2>> bindError)
        => Match(
            ok: Result<T, TError2>.Ok,
    error: bindError
        );

    public T ThrowOnError(Func<TError, Exception> exceptionFactory)
        => Match(
            ok: v => v,
            error: e => throw exceptionFactory(e)
        );

    public T IfError(T errorValue) => Match(ok: v => v, error: _ => errorValue);

    public bool ToBool() => Match(ok: _ => true, error: _ => false);

    public bool TryGetOk([NotNullWhen(true)] out T? success, [NotNullWhen(false)] out TError? error)
    {
        (success, error, bool isOk) = Match(
            ok: v => (v, default(TError?), true),
            error: e => (default(T?), e, false)
        );
        return isOk;
    }

    public static implicit operator Result<T, TError>(UntypedOk<T> success) => Ok(success.Value);
    public static implicit operator Result<T, TError>(UntypedError<TError> error) => Error(error.Value);

    public static implicit operator Result<T, TError>(T success) => Ok(success);
    public static implicit operator Result<T, TError>(TError error) => Error(error);
}
