// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.TestUtilities.FakeProvider
{
    public class FakeRelationalOptionsExtension : RelationalOptionsExtension
    {
        public FakeRelationalOptionsExtension()
        {
        }

        protected FakeRelationalOptionsExtension(FakeRelationalOptionsExtension copyFrom)
            : base(copyFrom)
        {
        }

        protected override RelationalOptionsExtension Clone()
            => new FakeRelationalOptionsExtension(this);

        public override bool ApplyServices(IServiceCollection services)
        {
            AddEntityFrameworkRelationalDatabase(services);

            return true;
        }

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
        }

        public static IServiceCollection AddEntityFrameworkRelationalDatabase(IServiceCollection serviceCollection)
        {
            var builder = new EntityFrameworkRelationalServicesBuilder(serviceCollection)
                .TryAdd<LoggingDefinitions, TestRelationalLoggingDefinitions>()
                .TryAdd<IDatabaseProvider, DatabaseProvider<FakeRelationalOptionsExtension>>()
                .TryAdd<ISqlGenerationHelper, RelationalSqlGenerationHelper>()
                .TryAdd<IRelationalTypeMappingSource, TestRelationalTypeMappingSource>()
                .TryAdd<IMigrationsSqlGenerator, TestRelationalMigrationSqlGenerator>()
                .TryAdd<IProviderConventionSetBuilder, TestRelationalConventionSetBuilder>()
                .TryAdd<IRelationalConnection, FakeRelationalConnection>()
                .TryAdd<IHistoryRepository>(_ => null)
                .TryAdd<IUpdateSqlGenerator, FakeSqlGenerator>()
                .TryAdd<IModificationCommandBatchFactory, TestModificationCommandBatchFactory>()
                .TryAdd<IRelationalDatabaseCreator, FakeRelationalDatabaseCreator>();

            builder.TryAddCoreServices();

            return serviceCollection;
        }
    }
}
