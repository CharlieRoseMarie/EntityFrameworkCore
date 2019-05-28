// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Microsoft.EntityFrameworkCore.Diagnostics
{
    public interface IDbCommandInterceptor
    {
        DbDataReader ReaderExecuting ([NotNull] DbCommand command, [NotNull] CommandEventData eventData);
        int? ScalarExecuting ([NotNull] DbCommand command, [NotNull] CommandEventData eventData);
        object NonQueryExecuting ([NotNull] DbCommand command, [NotNull] CommandEventData eventData);
        Task<DbDataReader> ReaderExecutingAsync ([NotNull] DbCommand command, [NotNull] CommandEventData eventData, CancellationToken cancellationToken = default);
        Task<int> ScalarExecutingAsync ([NotNull] DbCommand command, [NotNull] CommandEventData eventData, CancellationToken cancellationToken = default);
        Task<object> NonQueryExecutingAsync ([NotNull] DbCommand command, [NotNull] CommandEventData eventData, CancellationToken cancellationToken = default);
    }
}
