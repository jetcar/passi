﻿// <auto-generated />
using System;
using System.Collections.Generic;
using System.Reflection;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#pragma warning disable 219, 612, 618
#nullable disable

namespace IdentityServer.CompiledModel
{
    internal partial class ClientPostLogoutRedirectUriEntityType
    {
        public static RuntimeEntityType Create(RuntimeModel model, RuntimeEntityType baseEntityType = null)
        {
            var runtimeEntityType = model.AddEntityType(
                "IdentityServer4.EntityFramework.Entities.ClientPostLogoutRedirectUri",
                typeof(ClientPostLogoutRedirectUri),
                baseEntityType);

            var id = runtimeEntityType.AddProperty(
                "Id",
                typeof(int),
                propertyInfo: typeof(ClientPostLogoutRedirectUri).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(ClientPostLogoutRedirectUri).GetField("<Id>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                valueGenerated: ValueGenerated.OnAdd,
                afterSaveBehavior: PropertySaveBehavior.Throw);
            id.AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            var clientId = runtimeEntityType.AddProperty(
                "ClientId",
                typeof(int),
                propertyInfo: typeof(ClientPostLogoutRedirectUri).GetProperty("ClientId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(ClientPostLogoutRedirectUri).GetField("<ClientId>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            var postLogoutRedirectUri = runtimeEntityType.AddProperty(
                "PostLogoutRedirectUri",
                typeof(string),
                propertyInfo: typeof(ClientPostLogoutRedirectUri).GetProperty("PostLogoutRedirectUri", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(ClientPostLogoutRedirectUri).GetField("<PostLogoutRedirectUri>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);

            var key = runtimeEntityType.AddKey(
                new[] { id });
            runtimeEntityType.SetPrimaryKey(key);

            var index = runtimeEntityType.AddIndex(
                new[] { clientId });

            return runtimeEntityType;
        }

        public static RuntimeForeignKey CreateForeignKey1(RuntimeEntityType declaringEntityType, RuntimeEntityType principalEntityType)
        {
            var runtimeForeignKey = declaringEntityType.AddForeignKey(new[] { declaringEntityType.FindProperty("ClientId") },
                principalEntityType.FindKey(new[] { principalEntityType.FindProperty("Id") }),
                principalEntityType,
                deleteBehavior: DeleteBehavior.Cascade,
                required: true);

            var client = declaringEntityType.AddNavigation("Client",
                runtimeForeignKey,
                onDependent: true,
                typeof(Client),
                propertyInfo: typeof(ClientPostLogoutRedirectUri).GetProperty("Client", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(ClientPostLogoutRedirectUri).GetField("<Client>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            var postLogoutRedirectUris = principalEntityType.AddNavigation("PostLogoutRedirectUris",
                runtimeForeignKey,
                onDependent: false,
                typeof(List<ClientPostLogoutRedirectUri>),
                propertyInfo: typeof(Client).GetProperty("PostLogoutRedirectUris", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(Client).GetField("<PostLogoutRedirectUris>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            return runtimeForeignKey;
        }

        public static void CreateAnnotations(RuntimeEntityType runtimeEntityType)
        {
            runtimeEntityType.AddAnnotation("Relational:FunctionName", null);
            runtimeEntityType.AddAnnotation("Relational:Schema", null);
            runtimeEntityType.AddAnnotation("Relational:SqlQuery", null);
            runtimeEntityType.AddAnnotation("Relational:TableName", "ClientPostLogoutRedirectUri");
            runtimeEntityType.AddAnnotation("Relational:ViewName", null);
            runtimeEntityType.AddAnnotation("Relational:ViewSchema", null);

            Customize(runtimeEntityType);
        }

        static partial void Customize(RuntimeEntityType runtimeEntityType);
    }
}
