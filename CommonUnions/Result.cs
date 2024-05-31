using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Scifa.UnionTypes.Result;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Scifa.UnionTypes;


public static class Result
{
    public static UntypedOk<T> Ok<T>(T value) => new(value);

    public static UntypedError<TError> Error<TError>(TError error) => new(error);

    public static Result<T, ExceptionDispatchInfo> Catch<T>(Func<T> action)
        => Catch(action, _ => true);

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

    public static Task<Result<T, ExceptionDispatchInfo>> CatchAsync<T>(Func<Task<T>> action)
        => CatchAsync(action, _ => true);

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

    public static UntypedOk<Unit> Ok() => new(Unit.Instance);

    public static UntypedError<Unit> Error() => new(Unit.Instance);

    public static Result<Unit, TError> IgnoreValue<T, TError>(this Result<T, TError> @this) => @this.Map(_ => Unit.Instance);

    internal static Result<T[], TError> Collect<T, TError>(this IEnumerable<Result<T, TError>> results)
        => results.Aggregate(
            Result<IEnumerable<T>, TError>.Ok([]),
            (acc, result) => acc.Bind(accValue => result.Map(value => accValue.Append(value))),
            acc => acc.Map(values => values.ToArray())
        );

    public record class UntypedOk<T>(T Value);
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

    internal bool TryGetOk([NotNullWhen(true)] out T? success, [NotNullWhen(false)] out TError? error)
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



file class ResultJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Result<,>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter?)Activator.CreateInstance(typeof(ResultJsonConverter<,>).MakeGenericType(typeToConvert.GetGenericArguments()));

    public class ResultJsonConverter<T, TError> : JsonConverter<Result<T, TError>>
    {
        private const string CaseFieldName = "$case";
        private const int MaxProperties = 1;
        public override Result<T, TError>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var fields = JsonSerializer.Deserialize<Dictionary<string, global::System.Text.Json.JsonElement>>(ref reader, options);

            if (fields is null)
                return default;

            if (!fields.TryGetValue(CaseFieldName, out var caseToken))
            {
                throw new InvalidOperationException($"Unable to deserialize instance of Result<T,TError> as no case was specified.");
            }

            var caseString = caseToken.GetString();
            if (!Enum.TryParse<Result<T, TError>.CaseName>(caseString, out var caseValue))
            {
                throw new InvalidOperationException($"Unable to deserialize instance of Result<T,TError> as case '{caseString}' is unrecognised.");
            }

            return caseValue switch
            {
                Result<T, TError>.CaseName.Ok =>
                    Result<T, TError>.Ok(
                        value: GetValue<T>("value", fields, options)!
                    ),
                Result<T, TError>.CaseName.Error =>
                    Result<T, TError>.Error(
                        value: GetValue<TError>("value", fields, options)!
                    ),
                _ => throw new InvalidOperationException($"Unable to deserialize instance of Result<T,TError> as case '{caseString}' is unrecognised."),
            };
        }

        private static TValue GetValue<TValue>(in string propertyName, in Dictionary<string, global::System.Text.Json.JsonElement> fields, in JsonSerializerOptions options)
        {
            if (!fields.TryGetValue(propertyName, out var valueToken))
            {
                throw new InvalidOperationException($"Unable to deserialize instance of Result<T,TError> as no value for '{propertyName}' was specified.");
            }

            return JsonSerializer.Deserialize<TValue>(valueToken.GetRawText(), options)!;
        }

        public override void Write(Utf8JsonWriter writer, Result<T, TError> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(CaseFieldName);
            writer.WriteStringValue(value.Case.ToString());
            value.Do(
                value =>
                {
                    Write("value", value);
                },
                error =>
                {
                    Write("error", error);
                });
            writer.WriteEndObject();

            void Write<TValue>(string propertyName, TValue value)
            {
                writer.WritePropertyName(propertyName);
                JsonSerializer.Serialize(writer, value, options);
            }
        }
    }
}