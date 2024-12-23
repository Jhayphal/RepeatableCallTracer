﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace RepeatableCallTracer.Dependencies
{
    public sealed class ReflectionBasedTracedDependenciesProvider : ITracedTargetDependenciesProvider
    {
        private readonly CallTracerOptions options;

        public ReflectionBasedTracedDependenciesProvider(CallTracerOptions options)
        {
            this.options = options;
        }

        public IEnumerable<ITracedDependency> RetrieveDependenciesAndValidateIfRequired<TTarget>(TTarget target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var result = new List<ITracedDependency>();

            var targetType = target.GetType();
            var properties = targetType
                .GetFields(BindingFlags.Instance
                    | BindingFlags.Static
                    | BindingFlags.GetProperty
                    | BindingFlags.Public
                    | BindingFlags.NonPublic);

            foreach (var property in properties)
            {
                if (property.GetValue(target) is ITracedDependency dependency)
                {
                    result.Add(dependency);
                }
                else if (options.ThrowIfHasUntrackedDependencies)
                {
                    throw new InvalidProgramException($"Property {property.Name} is not an {nameof(ITracedDependency)}.");
                }
            }

            var fields = targetType
                .GetFields(BindingFlags.Instance
                    | BindingFlags.Static
                    | BindingFlags.GetField
                    | BindingFlags.Public
                    | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                if (field.GetValue(target) is ITracedDependency dependency)
                {
                    var add = true;

                    foreach (var existing in result)
                    {
                        if (ReferenceEquals(existing, dependency))
                        {
                            add = false;

                            break;
                        }
                    }

                    if (add)
                    {
                        result.Add(dependency);
                    }
                }
                else if (options.ThrowIfHasUntrackedDependencies)
                {
                    throw new InvalidProgramException($"Field {field.Name} is not an {nameof(ITracedDependency)}.");
                }
            }

            return result;
        }
    }
}
