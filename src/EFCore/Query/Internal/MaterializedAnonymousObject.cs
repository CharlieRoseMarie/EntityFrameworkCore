﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace Microsoft.EntityFrameworkCore.Query.Internal
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public readonly struct MaterializedAnonymousObject
    {
        ///// <summary>
        /////     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        /////     the same compatibility standards as public APIs. It may be changed or removed without notice in
        /////     any release. You should only use it directly in your code with extreme caution and knowing that
        /////     doing so can result in application failures when updating to a new Entity Framework Core release.
        ///// </summary>
        //public static bool IsGetValueExpression(
        //    [NotNull] MethodCallExpression methodCallExpression,
        //    out QuerySourceReferenceExpression querySourceReferenceExpression)
        //{
        //    querySourceReferenceExpression = null;

        //    if (methodCallExpression.Object?.Type == typeof(MaterializedAnonymousObject)
        //        && methodCallExpression.Method.Equals(GetValueMethodInfo)
        //        && methodCallExpression.Object is QuerySourceReferenceExpression qsre)
        //    {
        //        querySourceReferenceExpression = qsre;

        //        return true;
        //    }

        //    return false;
        //}

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public static readonly ConstructorInfo AnonymousObjectCtor
            = typeof(MaterializedAnonymousObject).GetTypeInfo()
                .DeclaredConstructors
                .Single(c => c.GetParameters().Length == 1);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public static readonly MethodInfo GetValueMethodInfo
            = typeof(MaterializedAnonymousObject).GetTypeInfo()
                .GetDeclaredMethod(nameof(GetValue));

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public static bool operator ==(MaterializedAnonymousObject x, MaterializedAnonymousObject y) => x.Equals(y);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public static bool operator !=(MaterializedAnonymousObject x, MaterializedAnonymousObject y) => !x.Equals(y);

        private readonly object[] _values;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        [UsedImplicitly]
        public MaterializedAnonymousObject([NotNull] object[] values) => _values = values;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public bool IsDefault() => _values == null;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is null
                ? false
                : obj is MaterializedAnonymousObject anonymousObject
                  && _values.SequenceEqual(anonymousObject._values);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return _values.Aggregate(
                    0,
                    (current, argument)
                        => current + ((current * 397) ^ (argument?.GetHashCode() ?? 0)));
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public object GetValue(int index) => _values[index];
    }
}
