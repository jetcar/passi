// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#pragma warning disable 219, 612, 618
#nullable disable

namespace Repos.CompiledModels
{
    public partial class PassiDbContextModel
    {
        partial void Initialize()
        {
            var dataProtectionKey = DataProtectionKeyEntityType.Create(this);
            var adminDb = AdminDbEntityType.Create(this);
            var certificateDb = CertificateDbEntityType.Create(this);
            var deviceDb = DeviceDbEntityType.Create(this);
            var sessionDb = SessionDbEntityType.Create(this);
            var userDb = UserDbEntityType.Create(this);
            var userInvitationDb = UserInvitationDbEntityType.Create(this);

            CertificateDbEntityType.CreateForeignKey1(certificateDb, userDb);
            CertificateDbEntityType.CreateForeignKey2(certificateDb, certificateDb);
            CertificateDbEntityType.CreateForeignKey3(certificateDb, userDb);
            DeviceDbEntityType.CreateForeignKey1(deviceDb, userDb);
            SessionDbEntityType.CreateForeignKey1(sessionDb, userDb);
            SessionDbEntityType.CreateForeignKey2(sessionDb, userDb);
            UserDbEntityType.CreateForeignKey1(userDb, deviceDb);
            UserDbEntityType.CreateForeignKey2(userDb, userDb);
            UserInvitationDbEntityType.CreateForeignKey1(userInvitationDb, userDb);
            UserInvitationDbEntityType.CreateForeignKey2(userInvitationDb, userDb);

            DataProtectionKeyEntityType.CreateAnnotations(dataProtectionKey);
            AdminDbEntityType.CreateAnnotations(adminDb);
            CertificateDbEntityType.CreateAnnotations(certificateDb);
            DeviceDbEntityType.CreateAnnotations(deviceDb);
            SessionDbEntityType.CreateAnnotations(sessionDb);
            UserDbEntityType.CreateAnnotations(userDb);
            UserInvitationDbEntityType.CreateAnnotations(userInvitationDb);

            AddAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            AddAnnotation("ProductVersion", "6.0.10");
            AddAnnotation("Relational:MaxIdentifierLength", 63);
        }
    }
}
