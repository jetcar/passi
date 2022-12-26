﻿// <auto-generated />
using System;
using System.Reflection;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#pragma warning disable 219, 612, 618
#nullable disable

namespace IdentityServer.CompiledModel
{
    internal partial class ApiResourceEntityType
    {
        public static RuntimeEntityType Create(RuntimeModel model, RuntimeEntityType baseEntityType = null)
        {
            var runtimeEntityType = model.AddEntityType(
                "IdentityServer4.EntityFramework.Entities.ApiResource",
                typeof(ApiResource),
                baseEntityType);

            var id = runtimeEntityType.AddProperty(
                "Id",
                typeof(int),
                propertyInfo: typeof(ApiResource).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(ApiResource).GetField("<Id>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                valueGenerated: ValueGenerated.OnAdd,
                afterSaveBehavior: PropertySaveBehavior.Throw);
            id.AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            var allowedAccessTokenSigningAlgorithms = runtimeEntityType.AddProperty(
                "AllowedAccessTokenSigningAlgorithms",
                typeof(string),
                propertyInfo: typeof(ApiResource).GetProperty("AllowedAccessTokenSigningAlgorithms", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(ApiResource).GetField("<AllowedAccessTokenSigningAlgorithms>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);

            var created = runtimeEntityType.AddProperty(
                "Created",
                typeof(DateTime),
                propertyInfo: typeof(ApiResource).GetProperty("Created", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(ApiResource).GetField("<Created>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            var description = runtimeEntityType.AddProperty(
                "Description",
                typeof(string),
                propertyInfo: typeof(ApiResource).GetProperty("Description", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(ApiResource).GetField("<Description>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);

            var displayName = runtimeEntityType.AddProperty(
                "DisplayName",
                typeof(string),
                propertyInfo: typeof(ApiResource).GetProperty("DisplayName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(ApiResource).GetField("<DisplayName>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);

            var enabled = runtimeEntityType.AddProperty(
                "Enabled",
                typeof(bool),
                propertyInfo: typeof(ApiResource).GetProperty("Enabled", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(ApiResource).GetField("<Enabled>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            var lastAccessed = runtimeEntityType.AddProperty(
                "LastAccessed",
                typeof(DateTime?),
                propertyInfo: typeof(ApiResource).GetProperty("LastAccessed", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(ApiResource).GetField("<LastAccessed>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);

            var name = runtimeEntityType.AddProperty(
                "Name",
                typeof(string),
                propertyInfo: typeof(ApiResource).GetProperty("Name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(ApiResource).GetField("<Name>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);

            var nonEditable = runtimeEntityType.AddProperty(
                "NonEditable",
                typeof(bool),
                propertyInfo: typeof(ApiResource).GetProperty("NonEditable", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(ApiResource).GetField("<NonEditable>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            var showInDiscoveryDocument = runtimeEntityType.AddProperty(
                "ShowInDiscoveryDocument",
                typeof(bool),
                propertyInfo: typeof(ApiResource).GetProperty("ShowInDiscoveryDocument", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(ApiResource).GetField("<ShowInDiscoveryDocument>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            var updated = runtimeEntityType.AddProperty(
                "Updated",
                typeof(DateTime?),
                propertyInfo: typeof(ApiResource).GetProperty("Updated", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(ApiResource).GetField("<Updated>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);

            var key = runtimeEntityType.AddKey(
                new[] { id });
            runtimeEntityType.SetPrimaryKey(key);

            return runtimeEntityType;
        }

        public static void CreateAnnotations(RuntimeEntityType runtimeEntityType)
        {
            runtimeEntityType.AddAnnotation("Relational:FunctionName", null);
            runtimeEntityType.AddAnnotation("Relational:Schema", null);
            runtimeEntityType.AddAnnotation("Relational:SqlQuery", null);
            runtimeEntityType.AddAnnotation("Relational:TableName", "ApiResources");
            runtimeEntityType.AddAnnotation("Relational:ViewName", null);
            runtimeEntityType.AddAnnotation("Relational:ViewSchema", null);

            Customize(runtimeEntityType);
        }

        static partial void Customize(RuntimeEntityType runtimeEntityType);
    }
}
