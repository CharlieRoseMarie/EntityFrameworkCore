// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.ChangeTracking.Internal
{
    public class InternalMixedEntityEntryTest : InternalEntityEntryTestBase
    {
        [ConditionalFact]
        public void Can_get_entity()
        {
            var model = BuildModel();
            var configuration = InMemoryTestHelpers.Instance.CreateContextServices(model);

            var entity = new SomeEntity();
            var entry = CreateInternalEntry(configuration, model.FindEntityType(typeof(SomeEntity).FullName), entity);

            Assert.Same(entity, entry.Entity);
        }

        [ConditionalFact]
        public void Can_set_and_get_property_value_from_CLR_object()
        {
            var model = BuildModel(finalize: false);
            var entityType = model.FindEntityType(typeof(SomeEntity).FullName);
            var keyProperty = entityType.AddProperty("Id_", typeof(int));
            var nonKeyProperty = entityType.FindProperty("Name");
            model.FinalizeModel();
            var configuration = InMemoryTestHelpers.Instance.CreateContextServices(model);

            var entity = new SomeEntity
            {
                Id = 77,
                Name = "Magic Tree House"
            };
            var entry = CreateInternalEntry(configuration, entityType, entity);

            Assert.Equal(0, entry[keyProperty]); // In shadow
            Assert.Equal("Magic Tree House", entry[nonKeyProperty]);

            entry[keyProperty] = 78;
            entry[nonKeyProperty] = "Normal Tree House";

            Assert.Equal(77, entity.Id); // In shadow
            Assert.Equal("Normal Tree House", entity.Name);
        }

        [ConditionalFact]
        public void Asking_for_entity_instance_causes_it_to_be_materialized()
        {
            var model = BuildModel();
            var entityType = model.FindEntityType(typeof(SomeEntity).FullName);
            var configuration = InMemoryTestHelpers.Instance.CreateContextServices(model);

            var entry = CreateInternalEntry(
                configuration,
                entityType,
                new SomeEntity
                {
                    Id = 1,
                    Name = "Kool"
                },
                new ValueBuffer(new object[] { 1, "Kool" }));

            var entity = (SomeEntity)entry.Entity;

            Assert.Equal("Kool", entity.Name);
        }

        [ConditionalFact]
        public void All_original_values_can_be_accessed_for_entity_that_does_no_notification()
        {
            var model = BuildModel();
            var entityType = model.FindEntityType(typeof(SomeEntity).FullName);

            AllOriginalValuesTest(
                model, entityType, new SomeEntity
                {
                    Id = 1,
                    Name = "Kool"
                });
        }

        [ConditionalFact]
        public void All_original_values_can_be_accessed_for_entity_that_does_changed_only_notifications_if_eager_values_on()
        {
            var model = BuildModel(finalize: false);
            var entityType = model.FindEntityType(typeof(ChangedOnlyEntity).FullName);
            entityType.SetChangeTrackingStrategy(ChangeTrackingStrategy.Snapshot);
            model.FinalizeModel();

            AllOriginalValuesTest(
                model, entityType, new ChangedOnlyEntity
                {
                    Id = 1,
                    Name = "Kool"
                });
        }

        [ConditionalFact]
        public void Setting_CLR_property_with_snapshot_change_tracking_requires_DetectChanges()
            => SetPropertyClrTest(
                new SomeEntity
                {
                    Id = 1,
                    Name = "Kool"
                }, needsDetectChanges: true);

        [ConditionalFact]
        public void Setting_CLR_property_with_changed_only_notifications_does_not_require_DetectChanges()
            => SetPropertyClrTest(
                new ChangedOnlyEntity
                {
                    Id = 1,
                    Name = "Kool"
                }, needsDetectChanges: false);

        [ConditionalFact]
        public void Setting_CLR_property_with_full_notifications_does_not_require_DetectChanges()
            => SetPropertyClrTest(
                new FullNotificationEntity
                {
                    Id = 1,
                    Name = "Kool"
                }, needsDetectChanges: false);

        protected override IMutableModel BuildModel(bool finalize = true)
        {
            var modelBuilder = new ModelBuilder(new ConventionSet());
            var model = modelBuilder.Model;

            var someSimpleEntityType = model.AddEntityType(typeof(SomeSimpleEntityBase));
            var simpleKeyProperty = someSimpleEntityType.AddProperty("Id", typeof(int));
            simpleKeyProperty.ValueGenerated = ValueGenerated.OnAdd;
            someSimpleEntityType.SetPrimaryKey(simpleKeyProperty);

            var someCompositeEntityType = model.AddEntityType(typeof(SomeCompositeEntityBase));
            var compositeKeyProperty1 = someCompositeEntityType.AddProperty("Id1", typeof(int));
            var compositeKeyProperty2 = someCompositeEntityType.AddProperty("Id2", typeof(string));
            compositeKeyProperty2.IsNullable = false;
            someCompositeEntityType.SetPrimaryKey(new[] { compositeKeyProperty1, compositeKeyProperty2 });

            var entityType1 = model.AddEntityType(typeof(SomeEntity));
            entityType1.BaseType = someSimpleEntityType;
            var property3 = entityType1.AddProperty("Name", typeof(string));
            property3.IsConcurrencyToken = false;
            property3.ValueGenerated = ValueGenerated.OnAdd;

            var entityType2 = model.AddEntityType(typeof(SomeDependentEntity));
            entityType2.BaseType = someCompositeEntityType;
            var fk = entityType2.AddProperty("SomeEntityId", typeof(int));
            entityType2.AddForeignKey(new[] { fk }, entityType1.FindPrimaryKey(), entityType1);
            // TODO: declare this on the derived type
            // #2611
            var justAProperty = someCompositeEntityType.AddProperty("JustAProperty", typeof(int));
            justAProperty.ValueGenerated = ValueGenerated.OnAdd;
            someCompositeEntityType.AddKey(justAProperty);

            var entityType3 = model.AddEntityType(typeof(FullNotificationEntity));
            var property6 = entityType3.AddProperty("Id", typeof(int));
            entityType3.SetPrimaryKey(property6);
            var property7 = entityType3.AddProperty("Name", typeof(string));
            property7.IsConcurrencyToken = true;
            entityType3.SetChangeTrackingStrategy(ChangeTrackingStrategy.ChangingAndChangedNotifications);

            var entityType4 = model.AddEntityType(typeof(ChangedOnlyEntity));
            var property8 = entityType4.AddProperty("Id", typeof(int));
            entityType4.SetPrimaryKey(property8);
            var property9 = entityType4.AddProperty("Name", typeof(string));
            property9.IsConcurrencyToken = true;
            entityType4.SetChangeTrackingStrategy(ChangeTrackingStrategy.ChangedNotifications);

            var entityType5 = model.AddEntityType(typeof(SomeMoreDependentEntity));
            entityType5.BaseType = someSimpleEntityType;
            var fk5a = entityType5.AddProperty("Fk1", typeof(int));
            var fk5b = entityType5.AddProperty("Fk2", typeof(string));
            entityType5.AddForeignKey(new[] { fk5a, fk5b }, entityType2.FindPrimaryKey(), entityType2);

            modelBuilder.Entity<OwnerClass>(
                eb =>
                {
                    eb.HasKey(e => e.Id);
                    var owned = eb.OwnsOne(e => e.Owned);
                    owned.WithOwner().HasForeignKey("Id");
                    owned.HasKey("Id");
                    owned.Property(e => e.Value);
                });

            return finalize ? (IMutableModel)model.FinalizeModel() : model;
        }
    }
}
