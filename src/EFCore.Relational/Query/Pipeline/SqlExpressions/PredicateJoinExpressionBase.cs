﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.EntityFrameworkCore.Relational.Query.Pipeline.SqlExpressions
{
    public abstract class PredicateJoinExpressionBase : JoinExpressionBase
    {
        protected PredicateJoinExpressionBase(TableExpressionBase table, SqlExpression joinPredicate)
            : base(table)
        {
            JoinPredicate = joinPredicate;
        }

        public SqlExpression JoinPredicate { get; }

        public override bool Equals(object obj)
            => obj != null
            && (ReferenceEquals(this, obj)
                || obj is PredicateJoinExpressionBase predicateJoinExpressionBase
                    && Equals(predicateJoinExpressionBase));

        private bool Equals(PredicateJoinExpressionBase predicateJoinExpressionBase)
            => base.Equals(predicateJoinExpressionBase)
            && JoinPredicate.Equals(predicateJoinExpressionBase.JoinPredicate);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ JoinPredicate.GetHashCode();

                return hashCode;
            }
        }
    }
}
