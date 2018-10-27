using System;
using System.Runtime.CompilerServices;
using Reusable.Reflection;

namespace Gunter.Services
{
    internal static class ExceptionHelper
    {
        public static Exception InitializationException(Exception inner, [CallerMemberName] string memberName = null)
        {
            return DynamicException.Create(memberName, $"Could not initialize application.", inner);
        }
    }
}