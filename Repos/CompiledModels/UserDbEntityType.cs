﻿// <auto-generated />
using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Models;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;

#pragma warning disable 219, 612, 618
#nullable disable

namespace Repos.CompiledModels
{
    internal partial class UserDbEntityType
    {
        public static RuntimeEntityType Create(RuntimeModel model, RuntimeEntityType baseEntityType = null)
        {
            var runtimeEntityType = model.AddEntityType(
                "Models.UserDb",
                typeof(UserDb),
                baseEntityType);

            var id = runtimeEntityType.AddProperty(
                "Id",
                typeof(long),
                propertyInfo: typeof(UserDb).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(UserDb).GetField("<Id>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                valueGenerated: ValueGenerated.OnAdd,
                afterSaveBehavior: PropertySaveBehavior.Throw,
                sentinel: 0L);
            id.TypeMapping = LongTypeMapping.Default.Clone(
                comparer: new ValueComparer<long>(
                    (long v1, long v2) => v1 == v2,
                    (long v) => v.GetHashCode(),
                    (long v) => v),
                keyComparer: new ValueComparer<long>(
                    (long v1, long v2) => v1 == v2,
                    (long v) => v.GetHashCode(),
                    (long v) => v),
                providerValueComparer: new ValueComparer<long>(
                    (long v1, long v2) => v1 == v2,
                    (long v) => v.GetHashCode(),
                    (long v) => v));
            id.AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            var creationTime = runtimeEntityType.AddProperty(
                "CreationTime",
                typeof(Instant),
                propertyInfo: typeof(BaseModel).GetProperty("CreationTime", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(BaseModel).GetField("<CreationTime>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                sentinel: NodaTime.Instant.FromUnixTimeTicks(0L));
            creationTime.TypeMapping = TimestampTzInstantMapping.Default.Clone(
                comparer: new ValueComparer<Instant>(
                    (Instant v1, Instant v2) => v1.Equals(v2),
                    (Instant v) => v.GetHashCode(),
                    (Instant v) => v),
                keyComparer: new ValueComparer<Instant>(
                    (Instant v1, Instant v2) => v1.Equals(v2),
                    (Instant v) => v.GetHashCode(),
                    (Instant v) => v),
                providerValueComparer: new ValueComparer<Instant>(
                    (Instant v1, Instant v2) => v1.Equals(v2),
                    (Instant v) => v.GetHashCode(),
                    (Instant v) => v));
            creationTime.AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);

            var deviceId = runtimeEntityType.AddProperty(
                "DeviceId",
                typeof(long?),
                propertyInfo: typeof(UserDb).GetProperty("DeviceId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(UserDb).GetField("<DeviceId>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true,
                valueGenerated: ValueGenerated.OnAdd);
            deviceId.TypeMapping = LongTypeMapping.Default.Clone(
                comparer: new ValueComparer<long?>(
                    (Nullable<long> v1, Nullable<long> v2) => v1.HasValue && v2.HasValue && (long)v1 == (long)v2 || !v1.HasValue && !v2.HasValue,
                    (Nullable<long> v) => v.HasValue ? ((long)v).GetHashCode() : 0,
                    (Nullable<long> v) => v.HasValue ? (Nullable<long>)(long)v : default(Nullable<long>)),
                keyComparer: new ValueComparer<long?>(
                    (Nullable<long> v1, Nullable<long> v2) => v1.HasValue && v2.HasValue && (long)v1 == (long)v2 || !v1.HasValue && !v2.HasValue,
                    (Nullable<long> v) => v.HasValue ? ((long)v).GetHashCode() : 0,
                    (Nullable<long> v) => v.HasValue ? (Nullable<long>)(long)v : default(Nullable<long>)),
                providerValueComparer: new ValueComparer<long?>(
                    (Nullable<long> v1, Nullable<long> v2) => v1.HasValue && v2.HasValue && (long)v1 == (long)v2 || !v1.HasValue && !v2.HasValue,
                    (Nullable<long> v) => v.HasValue ? ((long)v).GetHashCode() : 0,
                    (Nullable<long> v) => v.HasValue ? (Nullable<long>)(long)v : default(Nullable<long>)));
            deviceId.AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);
            deviceId.AddAnnotation("Relational:DefaultValueSql", "0");

            var emailHash = runtimeEntityType.AddProperty(
                "EmailHash",
                typeof(string),
                propertyInfo: typeof(UserDb).GetProperty("EmailHash", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(UserDb).GetField("<EmailHash>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true,
                maxLength: 256);
            emailHash.TypeMapping = NpgsqlStringTypeMapping.Default.Clone(
                comparer: new ValueComparer<string>(
                    (string v1, string v2) => v1 == v2,
                    (string v) => v.GetHashCode(),
                    (string v) => v),
                keyComparer: new ValueComparer<string>(
                    (string v1, string v2) => v1 == v2,
                    (string v) => v.GetHashCode(),
                    (string v) => v),
                providerValueComparer: new ValueComparer<string>(
                    (string v1, string v2) => v1 == v2,
                    (string v) => v.GetHashCode(),
                    (string v) => v),
                mappingInfo: new RelationalTypeMappingInfo(
                    storeTypeName: "character varying(256)",
                    size: 256));
            emailHash.TypeMapping = ((NpgsqlStringTypeMapping)emailHash.TypeMapping).Clone(npgsqlDbType: NpgsqlTypes.NpgsqlDbType.Varchar);
        emailHash.AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);

        var guid = runtimeEntityType.AddProperty(
            "Guid",
            typeof(Guid),
            propertyInfo: typeof(UserDb).GetProperty("Guid", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            fieldInfo: typeof(UserDb).GetField("<Guid>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            sentinel: new Guid("00000000-0000-0000-0000-000000000000"));
        guid.TypeMapping = GuidTypeMapping.Default.Clone(
            comparer: new ValueComparer<Guid>(
                (Guid v1, Guid v2) => v1 == v2,
                (Guid v) => v.GetHashCode(),
                (Guid v) => v),
            keyComparer: new ValueComparer<Guid>(
                (Guid v1, Guid v2) => v1 == v2,
                (Guid v) => v.GetHashCode(),
                (Guid v) => v),
            providerValueComparer: new ValueComparer<Guid>(
                (Guid v1, Guid v2) => v1 == v2,
                (Guid v) => v.GetHashCode(),
                (Guid v) => v),
            mappingInfo: new RelationalTypeMappingInfo(
                storeTypeName: "uuid"));
        guid.AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);

        var modifiedById = runtimeEntityType.AddProperty(
            "ModifiedById",
            typeof(long?),
            propertyInfo: typeof(BaseModel).GetProperty("ModifiedById", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            fieldInfo: typeof(BaseModel).GetField("<ModifiedById>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            nullable: true);
        modifiedById.TypeMapping = LongTypeMapping.Default.Clone(
            comparer: new ValueComparer<long?>(
                (Nullable<long> v1, Nullable<long> v2) => v1.HasValue && v2.HasValue && (long)v1 == (long)v2 || !v1.HasValue && !v2.HasValue,
                (Nullable<long> v) => v.HasValue ? ((long)v).GetHashCode() : 0,
                (Nullable<long> v) => v.HasValue ? (Nullable<long>)(long)v : default(Nullable<long>)),
            keyComparer: new ValueComparer<long?>(
                (Nullable<long> v1, Nullable<long> v2) => v1.HasValue && v2.HasValue && (long)v1 == (long)v2 || !v1.HasValue && !v2.HasValue,
                (Nullable<long> v) => v.HasValue ? ((long)v).GetHashCode() : 0,
                (Nullable<long> v) => v.HasValue ? (Nullable<long>)(long)v : default(Nullable<long>)),
            providerValueComparer: new ValueComparer<long?>(
                (Nullable<long> v1, Nullable<long> v2) => v1.HasValue && v2.HasValue && (long)v1 == (long)v2 || !v1.HasValue && !v2.HasValue,
                (Nullable<long> v) => v.HasValue ? ((long)v).GetHashCode() : 0,
                (Nullable<long> v) => v.HasValue ? (Nullable<long>)(long)v : default(Nullable<long>)));
        modifiedById.AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);

        var modifiedTime = runtimeEntityType.AddProperty(
            "ModifiedTime",
            typeof(Instant?),
            propertyInfo: typeof(BaseModel).GetProperty("ModifiedTime", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            fieldInfo: typeof(BaseModel).GetField("<ModifiedTime>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            nullable: true);
        modifiedTime.TypeMapping = TimestampTzInstantMapping.Default.Clone(
            comparer: new ValueComparer<Instant?>(
                (Nullable<Instant> v1, Nullable<Instant> v2) => v1.HasValue && v2.HasValue && ((Instant)v1).Equals((Instant)v2) || !v1.HasValue && !v2.HasValue,
                (Nullable<Instant> v) => v.HasValue ? ((Instant)v).GetHashCode() : 0,
                (Nullable<Instant> v) => v.HasValue ? (Nullable<Instant>)(Instant)v : default(Nullable<Instant>)),
            keyComparer: new ValueComparer<Instant?>(
                (Nullable<Instant> v1, Nullable<Instant> v2) => v1.HasValue && v2.HasValue && ((Instant)v1).Equals((Instant)v2) || !v1.HasValue && !v2.HasValue,
                (Nullable<Instant> v) => v.HasValue ? ((Instant)v).GetHashCode() : 0,
                (Nullable<Instant> v) => v.HasValue ? (Nullable<Instant>)(Instant)v : default(Nullable<Instant>)),
            providerValueComparer: new ValueComparer<Instant?>(
                (Nullable<Instant> v1, Nullable<Instant> v2) => v1.HasValue && v2.HasValue && ((Instant)v1).Equals((Instant)v2) || !v1.HasValue && !v2.HasValue,
                (Nullable<Instant> v) => v.HasValue ? ((Instant)v).GetHashCode() : 0,
                (Nullable<Instant> v) => v.HasValue ? (Nullable<Instant>)(Instant)v : default(Nullable<Instant>)));
        modifiedTime.AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);

        var key = runtimeEntityType.AddKey(
            new[] { id });
        runtimeEntityType.SetPrimaryKey(key);

        var iX_Users_DeviceId = runtimeEntityType.AddIndex(
            new[] { deviceId },
            name: "IX_Users_DeviceId");

        var iX_Users_EmailHash = runtimeEntityType.AddIndex(
            new[] { emailHash },
            name: "IX_Users_EmailHash",
            unique: true);

        var iX_Users_Guid = runtimeEntityType.AddIndex(
            new[] { guid },
            name: "IX_Users_Guid",
            unique: true);

        var iX_Users_ModifiedById = runtimeEntityType.AddIndex(
            new[] { modifiedById },
            name: "IX_Users_ModifiedById");

        return runtimeEntityType;
    }

    public static RuntimeForeignKey CreateForeignKey1(RuntimeEntityType declaringEntityType, RuntimeEntityType principalEntityType)
    {
        var runtimeForeignKey = declaringEntityType.AddForeignKey(new[] { declaringEntityType.FindProperty("DeviceId") },
            principalEntityType.FindKey(new[] { principalEntityType.FindProperty("Id") }),
            principalEntityType,
            deleteBehavior: DeleteBehavior.Cascade);

        var device = declaringEntityType.AddNavigation("Device",
            runtimeForeignKey,
            onDependent: true,
            typeof(DeviceDb),
            propertyInfo: typeof(UserDb).GetProperty("Device", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            fieldInfo: typeof(UserDb).GetField("<Device>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

        var users = principalEntityType.AddNavigation("Users",
            runtimeForeignKey,
            onDependent: false,
            typeof(ICollection<UserDb>),
            propertyInfo: typeof(DeviceDb).GetProperty("Users", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            fieldInfo: typeof(DeviceDb).GetField("<Users>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

        return runtimeForeignKey;
    }

    public static RuntimeForeignKey CreateForeignKey2(RuntimeEntityType declaringEntityType, RuntimeEntityType principalEntityType)
    {
        var runtimeForeignKey = declaringEntityType.AddForeignKey(new[] { declaringEntityType.FindProperty("ModifiedById") },
            principalEntityType.FindKey(new[] { principalEntityType.FindProperty("Id") }),
            principalEntityType,
            deleteBehavior: DeleteBehavior.Restrict);

        var modifiedBy = declaringEntityType.AddNavigation("ModifiedBy",
            runtimeForeignKey,
            onDependent: true,
            typeof(UserDb),
            propertyInfo: typeof(BaseModel).GetProperty("ModifiedBy", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            fieldInfo: typeof(BaseModel).GetField("<ModifiedBy>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

        var inverseModifiedBy = principalEntityType.AddNavigation("InverseModifiedBy",
            runtimeForeignKey,
            onDependent: false,
            typeof(ICollection<UserDb>),
            propertyInfo: typeof(UserDb).GetProperty("InverseModifiedBy", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            fieldInfo: typeof(UserDb).GetField("<InverseModifiedBy>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

        return runtimeForeignKey;
    }

    public static void CreateAnnotations(RuntimeEntityType runtimeEntityType)
    {
        runtimeEntityType.AddAnnotation("Relational:FunctionName", null);
        runtimeEntityType.AddAnnotation("Relational:Schema", null);
        runtimeEntityType.AddAnnotation("Relational:SqlQuery", null);
        runtimeEntityType.AddAnnotation("Relational:TableName", "Users");
        runtimeEntityType.AddAnnotation("Relational:ViewName", null);
        runtimeEntityType.AddAnnotation("Relational:ViewSchema", null);

        Customize(runtimeEntityType);
    }

    static partial void Customize(RuntimeEntityType runtimeEntityType);
}
}
