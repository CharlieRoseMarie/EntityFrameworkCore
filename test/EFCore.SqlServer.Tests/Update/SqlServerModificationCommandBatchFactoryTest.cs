// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Update.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.Update
{
    public class SqlServerModificationCommandBatchFactoryTest
    {
        [ConditionalFact]
        public void Uses_MaxBatchSize_specified_in_SqlServerOptionsExtension()
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlServer("Database=Crunchie", b => b.MaxBatchSize(1));

            var typeMapper = new SqlServerTypeMappingSource(
                TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
                TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>());

            var logger = new FakeDiagnosticsLogger<DbLoggerCategory.Database.Command>();

            var factory = new SqlServerModificationCommandBatchFactory(
                new ModificationCommandBatchFactoryDependencies(
                    new RelationalCommandBuilderFactory(
                        new RelationalCommandBuilderDependencies(
                            typeMapper)),
                    new SqlServerSqlGenerationHelper(
                        new RelationalSqlGenerationHelperDependencies()),
                    new SqlServerUpdateSqlGenerator(
                        new UpdateSqlGeneratorDependencies(
                            new SqlServerSqlGenerationHelper(
                                new RelationalSqlGenerationHelperDependencies()),
                            typeMapper)),
                    new TypedRelationalValueBufferFactoryFactory(
                        new RelationalValueBufferFactoryDependencies(
                            typeMapper, new CoreSingletonOptions())),
                    logger),
                optionsBuilder.Options);

            var batch = factory.Create();

            Assert.True(batch.AddCommand(new ModificationCommand("T1", null, new ParameterNameGenerator().GenerateNext, false, null)));
            Assert.False(batch.AddCommand(new ModificationCommand("T1", null, new ParameterNameGenerator().GenerateNext, false, null)));
        }

        [ConditionalFact]
        public void MaxBatchSize_is_optional()
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlServer("Database=Crunchie");

            var typeMapper = new SqlServerTypeMappingSource(
                TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
                TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>());

            var logger = new FakeDiagnosticsLogger<DbLoggerCategory.Database.Command>();

            var factory = new SqlServerModificationCommandBatchFactory(
                new ModificationCommandBatchFactoryDependencies(
                    new RelationalCommandBuilderFactory(
                        new RelationalCommandBuilderDependencies(
                            typeMapper)),
                    new SqlServerSqlGenerationHelper(
                        new RelationalSqlGenerationHelperDependencies()),
                    new SqlServerUpdateSqlGenerator(
                        new UpdateSqlGeneratorDependencies(
                            new SqlServerSqlGenerationHelper(
                                new RelationalSqlGenerationHelperDependencies()),
                            typeMapper)),
                    new TypedRelationalValueBufferFactoryFactory(
                        new RelationalValueBufferFactoryDependencies(
                            typeMapper, new CoreSingletonOptions())),
                    logger),
                optionsBuilder.Options);

            var batch = factory.Create();

            Assert.True(batch.AddCommand(new ModificationCommand("T1", null, new ParameterNameGenerator().GenerateNext, false, null)));
            Assert.True(batch.AddCommand(new ModificationCommand("T1", null, new ParameterNameGenerator().GenerateNext, false, null)));
        }
    }
}
