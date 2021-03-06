#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System.Threading.Tasks;
using Autofac;
using FluentValidation;
using MediatR;
using Strive.Core.Extensions;
using Strive.Core.Interfaces;

namespace Strive.Hubs.Core.Services.Middlewares
{
    public static class ServiceInvokerValidationMiddleware
    {
        public static IServiceRequestBuilder<TResponse> ValidateObject<TResponse, TObj>(
            this IServiceRequestBuilder<TResponse> builder, TObj obj)
        {
            return builder.AddMiddleware(context => ValidateObject(context, obj));
        }

        public static async ValueTask<SuccessOrError<Unit>> ValidateObject<T>(ServiceInvokerContext context, T obj)
        {
            if (!context.Context.TryResolve<IValidator<T>>(out var validator))
                return SuccessOrError<Unit>.Succeeded(Unit.Value);

            var result = validator.Validate(obj);
            if (result.IsValid) return SuccessOrError<Unit>.Succeeded(Unit.Value);

            return result.ToError();
        }
    }
}
