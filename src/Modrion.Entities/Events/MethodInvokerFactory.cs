using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Reflection;

namespace Modrion.Entities;

internal static class MethodInvokerFactory
{
    private static readonly MethodInfo _getServiceInfo =
        typeof(MethodInvokerFactory).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static MethodInvoker Compile(MethodInfo methodInfo, MethodParameterSource[] parameterSources, object? uninvokedReturnValue = null, bool retBoolToResult = true)
    {
        if (methodInfo.DeclaringType == null)
            throw new ArgumentException("Method must have declaring type", nameof(methodInfo));

        var instanceArg = Expression.Parameter(typeof(object), "instance");
        var argsArg = Expression.Parameter(typeof(object[]), "args");
        var serviceProviderArg = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");

        var expressions = new List<Expression>();
        var locals = new List<ParameterExpression>();
        var methodArguments = new Expression[parameterSources.Length];

        Expression? argsCheckExpression = null;

        for (int i = 0; i < parameterSources.Length; i++)
        {
            var source = parameterSources[i];
            var paramType = source.Info.ParameterType;

            if (paramType.IsByRef)
                throw new NotSupportedException("ByRef parameters not supported");

            if (source.IsService)
            {
                var getServiceCall = Expression.Call(
                    typeof(ServiceProviderServiceExtensions),
                    nameof(ServiceProviderServiceExtensions.GetRequiredService),
                    new[] { paramType },
                    serviceProviderArg
                );
                methodArguments[i] = getServiceCall;
            }
            else if (source.ParameterIndex >= 0)
            {
                var indexExpr = Expression.Constant(source.ParameterIndex);
                var argAccess = Expression.ArrayIndex(argsArg, indexExpr);
                methodArguments[i] = Expression.Convert(argAccess, paramType);
            }
        }

        Expression call;
        if (methodInfo.IsStatic)
        {
            call = Expression.Call(methodInfo, methodArguments);
        }
        else
        {
            var instanceCast = Expression.Convert(instanceArg, methodInfo.DeclaringType!);
            call = Expression.Call(instanceCast, methodInfo, methodArguments);
        }

        if (call.Type == typeof(void))
        {
            call = Expression.Block(call, Expression.Constant(null, typeof(object)));
        }
        else if (retBoolToResult && call.Type == typeof(bool))
        {
            var boxMethod = typeof(MethodResult).GetMethod(nameof(MethodResult.From))!;
            call = Expression.Call(boxMethod, call);
        }
        else if (call.Type != typeof(object))
        {
            call = Expression.Convert(call, typeof(object));
        }

        if (argsCheckExpression != null)
        {
            call = Expression.Condition(argsCheckExpression, call, Expression.Constant(uninvokedReturnValue, typeof(object)));
        }

        if (locals.Count > 0 || expressions.Count > 0)
        {
            expressions.Add(call);
            call = Expression.Block(locals, expressions);
        }

        var lambda = Expression.Lambda<MethodInvoker>(call, instanceArg, argsArg, serviceProviderArg);
        return lambda.Compile();
    }

    private static object GetService(IServiceProvider serviceProvider, Type type)
    {
        return serviceProvider.GetRequiredService(type);
    }
}