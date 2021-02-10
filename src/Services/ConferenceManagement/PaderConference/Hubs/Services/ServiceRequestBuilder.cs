﻿using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PaderConference.Core.Interfaces;

namespace PaderConference.Hubs.Services
{
    public class ServiceRequestBuilder<TResponse> : ServiceRequestBuilderBase<TResponse>
    {
        private readonly Func<IRequest<TResponse>> _requestFactory;
        private readonly IMediator _mediator;

        public ServiceRequestBuilder(Func<IRequest<TResponse>> requestFactory, IMediator mediator,
            ServiceInvokerContext context) : base(context)
        {
            _requestFactory = requestFactory;
            _mediator = mediator;
        }

        protected override async Task<SuccessOrError<TResponse>> CreateRequest(CancellationToken token)
        {
            var request = _requestFactory();
            return SuccessOrError<TResponse>.Succeeded(await _mediator.Send(request, token));
        }
    }
}