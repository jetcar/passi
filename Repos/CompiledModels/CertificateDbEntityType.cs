// <auto-generated />
using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Models;
using NodaTime;

#pragma warning disable 219, 612, 618
#nullable disable

namespace Repos.CompiledModels
{
    internal partial class CertificateDbEntityType
    {
        public static RuntimeEntityType Create(RuntimeModel model, RuntimeEntityType baseEntityType = null)
        {
            var runtimeEntityType = model.AddEntityType(
                "Models.CertificateDb",
                typeof(CertificateDb),
                baseEntityType);

            var thumbprint = runtimeEntityType.AddProperty(
                "Thumbprint",
                typeof(string),
                propertyInfo: typeof(CertificateDb).GetProperty("Thumbprint", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(CertificateDb).GetField("<Thumbprint>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                afterSaveBehavior: PropertySaveBehavior.Throw,
                maxLength: 256);

            var creationTime = runtimeEntityType.AddProperty(
                "CreationTime",
                typeof(Instant),
                propertyInfo: typeof(BaseModel).GetProperty("CreationTime", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(BaseModel).GetField("<CreationTime>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
            creationTime.AddAnnotation("Relational:ColumnType", "timestamp without time zone");

            var modifiedById = runtimeEntityType.AddProperty(
                "ModifiedById",
                typeof(long?),
                propertyInfo: typeof(BaseModel).GetProperty("ModifiedById", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(BaseModel).GetField("<ModifiedById>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);

            var modifiedTime = runtimeEntityType.AddProperty(
                "ModifiedTime",
                typeof(Instant?),
                propertyInfo: typeof(BaseModel).GetProperty("ModifiedTime", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(BaseModel).GetField("<ModifiedTime>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);
            modifiedTime.AddAnnotation("Relational:ColumnType", "timestamp without time zone");

            var parentCertId = runtimeEntityType.AddProperty(
                "ParentCertId",
                typeof(string),
                propertyInfo: typeof(CertificateDb).GetProperty("ParentCertId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(CertificateDb).GetField("<ParentCertId>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true,
                maxLength: 256);

            var parentCertSignature = runtimeEntityType.AddProperty(
                "ParentCertSignature",
                typeof(string),
                propertyInfo: typeof(CertificateDb).GetProperty("ParentCertSignature", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(CertificateDb).GetField("<ParentCertSignature>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true,
                maxLength: 1024);

            var privateCert = runtimeEntityType.AddProperty(
                "PrivateCert",
                typeof(string),
                propertyInfo: typeof(CertificateDb).GetProperty("PrivateCert", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(CertificateDb).GetField("<PrivateCert>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true,
                maxLength: 1024);

            var publicCert = runtimeEntityType.AddProperty(
                "PublicCert",
                typeof(string),
                propertyInfo: typeof(CertificateDb).GetProperty("PublicCert", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(CertificateDb).GetField("<PublicCert>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true,
                maxLength: 2048);

            var userId = runtimeEntityType.AddProperty(
                "UserId",
                typeof(long),
                propertyInfo: typeof(CertificateDb).GetProperty("UserId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(CertificateDb).GetField("<UserId>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            var key = runtimeEntityType.AddKey(
                new[] { thumbprint });
            runtimeEntityType.SetPrimaryKey(key);

            var iX_Certificates_ModifiedById = runtimeEntityType.AddIndex(
                new[] { modifiedById },
                name: "IX_Certificates_ModifiedById");

            var iX_Certificates_ParentCertId = runtimeEntityType.AddIndex(
                new[] { parentCertId },
                name: "IX_Certificates_ParentCertId",
                unique: true);

            var iX_Certificates_UserId = runtimeEntityType.AddIndex(
                new[] { userId },
                name: "IX_Certificates_UserId");

            return runtimeEntityType;
        }

        public static RuntimeForeignKey CreateForeignKey1(RuntimeEntityType declaringEntityType, RuntimeEntityType principalEntityType)
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

            var certificateModifiedBies = principalEntityType.AddNavigation("CertificateModifiedBies",
                runtimeForeignKey,
                onDependent: false,
                typeof(ICollection<CertificateDb>),
                propertyInfo: typeof(UserDb).GetProperty("CertificateModifiedBies", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(UserDb).GetField("<CertificateModifiedBies>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            return runtimeForeignKey;
        }

        public static RuntimeForeignKey CreateForeignKey2(RuntimeEntityType declaringEntityType, RuntimeEntityType principalEntityType)
        {
            var runtimeForeignKey = declaringEntityType.AddForeignKey(new[] { declaringEntityType.FindProperty("ParentCertId") },
                principalEntityType.FindKey(new[] { principalEntityType.FindProperty("Thumbprint") }),
                principalEntityType,
                deleteBehavior: DeleteBehavior.Restrict,
                unique: true);

            var parentCert = declaringEntityType.AddNavigation("ParentCert",
                runtimeForeignKey,
                onDependent: true,
                typeof(CertificateDb),
                propertyInfo: typeof(CertificateDb).GetProperty("ParentCert", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(CertificateDb).GetField("<ParentCert>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            var inverseParentCert = principalEntityType.AddNavigation("InverseParentCert",
                runtimeForeignKey,
                onDependent: false,
                typeof(CertificateDb),
                propertyInfo: typeof(CertificateDb).GetProperty("InverseParentCert", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(CertificateDb).GetField("<InverseParentCert>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            return runtimeForeignKey;
        }

        public static RuntimeForeignKey CreateForeignKey3(RuntimeEntityType declaringEntityType, RuntimeEntityType principalEntityType)
        {
            var runtimeForeignKey = declaringEntityType.AddForeignKey(new[] { declaringEntityType.FindProperty("UserId") },
                principalEntityType.FindKey(new[] { principalEntityType.FindProperty("Id") }),
                principalEntityType,
                deleteBehavior: DeleteBehavior.Cascade,
                required: true);

            var user = declaringEntityType.AddNavigation("User",
                runtimeForeignKey,
                onDependent: true,
                typeof(UserDb),
                propertyInfo: typeof(CertificateDb).GetProperty("User", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(CertificateDb).GetField("<User>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            var certificates = principalEntityType.AddNavigation("Certificates",
                runtimeForeignKey,
                onDependent: false,
                typeof(ICollection<CertificateDb>),
                propertyInfo: typeof(UserDb).GetProperty("Certificates", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(UserDb).GetField("<Certificates>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));

            return runtimeForeignKey;
        }

        public static void CreateAnnotations(RuntimeEntityType runtimeEntityType)
        {
            runtimeEntityType.AddAnnotation("Relational:FunctionName", null);
            runtimeEntityType.AddAnnotation("Relational:Schema", null);
            runtimeEntityType.AddAnnotation("Relational:SqlQuery", null);
            runtimeEntityType.AddAnnotation("Relational:TableName", "Certificates");
            runtimeEntityType.AddAnnotation("Relational:ViewName", null);
            runtimeEntityType.AddAnnotation("Relational:ViewSchema", null);

            Customize(runtimeEntityType);
        }

        static partial void Customize(RuntimeEntityType runtimeEntityType);
    }
}
