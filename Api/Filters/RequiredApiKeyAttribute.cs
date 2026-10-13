
using System;

namespace Api.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequiredApiKeyAttribute:Attribute
    {
    
    }
}