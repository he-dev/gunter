using System;
using Gunter.Data.Abstractions;

namespace Gunter.Services
{
    internal class Handle<THandlee> : IServiceMapping
    {
        public static IServiceMapping With<THandler>() => new Handle<THandlee> { HandlerType = typeof(THandler) };

        public Type HandleeType => typeof(THandlee);
        public Type HandlerType { get; private set; }
    }
}