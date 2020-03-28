using System;

namespace Gunter.Data.Abstractions
{
    public interface IServiceMapping
    {
        Type HandleeType { get; }

        Type HandlerType { get; }
    }
}