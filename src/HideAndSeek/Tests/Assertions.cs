// ReSharper disable ExplicitCallerInfoArgument

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using UnityEngine.Assertions;

namespace OneLetterShor.HideAndSeek.Tests;

internal static class Assertions
{
    private static readonly Dictionary<Type, string> _aliasByType = new()
    {
        { typeof(bool),    "bool"    },
        { typeof(byte),    "byte"    },
        { typeof(sbyte),   "sbyte"   },
        { typeof(char),    "char"    },
        { typeof(decimal), "decimal" },
        { typeof(double),  "double"  },
        { typeof(float),   "float"   },
        { typeof(int),     "int"     },
        { typeof(uint),    "uint"    },
        { typeof(nint),    "nint"    },
        { typeof(nuint),   "nuint"   },
        { typeof(long),    "long"    },
        { typeof(ulong),   "ulong"   },
        { typeof(short),   "short"   },
        { typeof(ushort),  "ushort"  },
        { typeof(object),  "object"  },
        { typeof(string),  "string"  }
    };
    
    [DoesNotReturn]
    private static void FailAssertion(
        string? message,
        string assertionExpression)
    {
#if SHOULD_EXPLICITLY_LOG_ASSERTION_FAILURES
        StackTrace stackTrace = new(0, true);
        Logger.Fatal($"(EXPLICITLY LOGGED) Assertion Failure: -> {assertionExpression} <- {message}\n{stackTrace}");
#endif
        
        throw new AssertionException($"Assertion Failure: -> {assertionExpression} <- {message}", null);
    }
    
    private static void FailSoftAssertion(
        string? message,
        LogLevel logLevels,
        bool canGenerateStackTrace,
        int skipFrames,
        string assertionExpression,
        string assertingMemberName,
        int assertingLineNumber,
        string assertingFilePath)
    {
        StackTrace? stackTrace = canGenerateStackTrace
            ? new StackTrace(skipFrames, true)
            : null;
        
        Logger.Log(
            logLevels,
            $"Soft Assertion Failure: -> {assertionExpression} <- {message}\n{stackTrace}",
            assertingMemberName,
            assertingLineNumber,
            assertingFilePath
        );
    }
    
    /// <summary>
    /// Declares to static analyzers that a specific expression is <see langword="true"/>.
    /// </summary>
    /// <param name="condition">The expression that is expected to be <see langword="true"/>.</param>
    /// <param name="message">Additional information about the assertion.</param>
    /// <param name="assertionExpression">Used to improve logging of assertion failure. Do not overwrite.</param>
    /// <exception cref="AssertionException">Thrown if <paramref name="condition"/> is <see langword="false"/>.</exception>
    internal static void Assert(
        [DoesNotReturnIf(false)] bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string assertionExpression = "")
    {
        if (condition) return;
        
        FailAssertion(message, assertionExpression);
    }
    
    /// <inheritdoc cref="Assert"/>
    [Conditional("DEBUG")]
    internal static void DebugAssert(
        [DoesNotReturnIf(false)] bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string assertionExpression = "")
    {
        Assert(condition, message, assertionExpression);
    }
    
    /// <summary>
    /// Logs an assertion failure if a specific expression is <see langword="false"/>.
    /// </summary>
    /// <param name="condition">The expression that is expected to be <see langword="true"/>.</param>
    /// <param name="message">Additional information about the assertion.</param>
    /// <param name="logLevels">The <see cref="LogLevel"/> used for logging.</param>
    /// <param name="canGenerateStackTrace">Toggles generating a <see cref="StackTrace"/> on failures.</param>
    /// <param name="skipFrames">How many frames the <see cref="StackTrace"/> skips.</param>
    /// <param name="assertionExpression">Used to improve logging of assertion failure. Do not overwrite.</param>
    /// <param name="assertingMemberName">Used to inform the logger on who called the assert method.</param>
    /// <param name="assertingLineNumber">Used to inform the logger on who called the assert method.</param>
    /// <param name="assertingFilePath">Used to inform the logger on who called the assert method.</param>
    internal static void SoftAssert(
        bool condition,
        string? message = null,
        LogLevel logLevels = LogLevel.Error,
        bool canGenerateStackTrace = false,
        int skipFrames = 1,
        [CallerArgumentExpression(nameof(condition))] string assertionExpression = "",
        [CallerMemberName] string assertingMemberName = "",
        [CallerLineNumber] int assertingLineNumber = 0,
        [CallerFilePath] string assertingFilePath = "")
    {
        
        if (condition) return;
        
        FailSoftAssertion(
            message,
            logLevels,
            canGenerateStackTrace,
            skipFrames + 1,
            assertionExpression,
            assertingMemberName,
            assertingLineNumber,
            assertingFilePath
        );
    }
    
    /// <inheritdoc cref="SoftAssert"/>
    [Conditional("DEBUG")]
    internal static void DebugSoftAssert(
        bool condition,
        string? message = null,
        LogLevel logLevels = LogLevel.Error,
        bool canGenerateStackTrace = false,
        int skipFrames = 1,
        [CallerArgumentExpression(nameof(condition))] string assertionExpression = "",
        [CallerMemberName] string assertingMemberName = "",
        [CallerLineNumber] int assertingLineNumber = 0,
        [CallerFilePath] string assertingFilePath = "")
    {
        SoftAssert(
            condition,
            message,
            logLevels,
            canGenerateStackTrace,
            skipFrames + 1,
            assertionExpression,
            assertingMemberName,
            assertingLineNumber,
            assertingFilePath
        );
    }
    
    /// <summary>
    /// Provides the cast object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to cast <paramref name="obj"/>.</typeparam>
    /// <param name="obj">The object that is expected to be of type <typeparamref name="T"/>.</param>
    /// <param name="t"><paramref name="obj"/> cast to <typeparamref name="T"/>.</param>
    /// <param name="message">Additional information about the assertion.</param>
    /// <param name="objExpression">Used to improve logging of assertion failure. Do not overwrite.</param>
    /// <exception cref="AssertionException">Thrown if <paramref name="obj"/> is not of type <typeparamref name="T"/>.</exception>
    internal static void AssertIs<T>(
        object? obj,
        out T t,
        string? message = null,
        [CallerArgumentExpression(nameof(obj))] string objExpression = "")
    {
        if (obj is T objT)
        {
            t = objT;
            return;
        }
        
        FailAssertion(message, $"{objExpression} is {GetFriendlyTypeName(typeof(T))}");
        
        throw new UnreachableException();
    }
    
    /// <summary>
    /// Creates a C#-style type name for the specified <see cref="Type"/> by attempting to find
    /// a built-in alias or by generating a string that more closely represents source code.
    /// </summary>
    /// <returns>A string that represents how a type would be written in source code.</returns>
    /// <remarks>Type representation is not foolproof, but it works for most use cases.</remarks>
    private static string GetFriendlyTypeName(Type type)
    {
        if (_aliasByType.TryGetValue(type, out var alias))
            return alias;
        
        string friendlyName = type.Name;
        
        string prefix = type.IsNested 
            ? $"{GetFriendlyTypeName(type.DeclaringType!)}."
            : "";
        
        if (type.IsArray)
        {
            int dimensionCount = type.GetArrayRank();
            
            string commas = dimensionCount > 1 
                ? new string(',', dimensionCount - 1)
                : "";
            
            friendlyName = $"{prefix}{GetFriendlyTypeName(type.GetElementType()!)}[{commas}]";
        }
        else if (type.IsGenericType)
        {
            bool isNullableValueType = type.GetGenericTypeDefinition() == typeof(Nullable<>);
            IEnumerable<string> args = type.GetGenericArguments().Select(GetFriendlyTypeName);
            
            string name = type.Name.Substring(0, type.Name.IndexOf('`'));
            
            
            friendlyName = isNullableValueType 
                ? $"{string.Join(", ", args)}?"
                : $"{name}<{string.Join(", ", args)}>";
        }
        
        return $"{prefix}{friendlyName}";
    }
    
    private sealed class UnreachableException(string? message = null) : AssertionException($"Unreachable code. {message}", null);
}