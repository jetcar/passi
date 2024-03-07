using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using MauiViewModels.utils.Services.Certificate;
using Microsoft.EntityFrameworkCore;
using Models;
using NodaTime;
using Repos;
using Microsoft.Extensions.Configuration;

namespace RegisterManyUsersTestApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var timeout = 30000;
            var certPath = "../certs_passi";

            _configuration = new ConfigurationBuilder().AddJsonFile("test.appsettings.json").Build();

            Directory.CreateDirectory(certPath);
            var list = new int[10000];
            var certService = new CertificatesService(null, null, null, null, null);
            var passiDb = new PassiDbContext()
            {
                _connectionString =
                    $"host={_configuration["DbHost"]};port={_configuration["DbPort"]};database=Passi;user id=postgres;password={_configuration["DbPassword"]};Ssl Mode=allow;"
            };
            passiDb.Database.Migrate();
            var index = 0;
            var locker = new object();
            var locker2 = new object();
            var users = new List<UserDb>(11000);
            var start = DateTime.UtcNow;
            Parallel.ForEach(list, new ParallelOptions() { MaxDegreeOfParallelism = 12 }, (a) =>
            {
                var localIndex = 0;
                lock (locker)
                {
                    localIndex = Interlocked.Increment(ref index);
                }

                var subject = $"test{localIndex}@passi.cloud";
                var cert = certService.GenerateCertificate(subject.Replace("@", ""), new MySecureString("")).Result;

                var path = $"{certPath}/admin{localIndex}.crt";
                if (File.Exists(path))
                    return;

                var userDb = UserDb(localIndex, cert, subject);

                using (var writer = new StreamWriter(File.Open(path, FileMode.OpenOrCreate)))
                {
                    writer.Write($"{userDb.EmailHash};{userDb.Guid.ToString()};{userDb.Device.DeviceId};{cert.Item1.Thumbprint};{cert.Item2};{Convert.ToBase64String(cert.Item3)}");
                    writer.Flush();
                }

                lock (locker2)
                {
                    users.Add(userDb);
                    if (users.Count > 1000)
                    {
                        Console.WriteLine(localIndex);

                        var deviceDbs = users.Select(x => x.Device).ToList();
                        passiDb.Devices.BulkInsert(deviceDbs);

                        passiDb.Users.BulkInsert(users);
                        foreach (var user in users)
                        {
                            foreach (var userCertificate in user.Certificates)
                            {
                                userCertificate.UserId = user.Id;
                            }
                            foreach (var userInvitationDb in user.Invitations)
                            {
                                userInvitationDb.UserId = user.Id;
                            }
                        }

                        var certificateDbs = users.SelectMany(x => x.Certificates).ToList();
                        passiDb.Certificates.BulkInsert(certificateDbs);
                        var userInvitationDbs = users.SelectMany(x => x.Invitations).ToList();
                        passiDb.Invitations.BulkInsert(userInvitationDbs);
                        foreach (var user in users)
                        {
                            user.DeviceId = user.Device.Id;
                        }
                        passiDb.Users.BulkUpdate(users);
                        users.Clear();
                        var end = DateTime.UtcNow;
                        Console.WriteLine((end - start).TotalSeconds);
                        start = end;
                    }
                }
            });
        }

        private static IConfigurationRoot _configuration;

        private static UserDb UserDb(int localIndex, Tuple<X509Certificate2, string, byte[]> cert, string subject)
        {
            return new UserDb()
            {
                Guid = Guid.NewGuid(),
                CreationTime = Instant.FromDateTimeUtc(DateTime.UtcNow),
                Device = new DeviceDb()
                {
                    DeviceId = Guid.NewGuid().ToString(),
                    Platform = "android",
                }
                ,
                Certificates = new List<CertificateDb>()
                {
                    new CertificateDb()
                    {
                        CreationTime = Instant.FromDateTimeUtc(DateTime.UtcNow),
                        Thumbprint = cert.Item1.Thumbprint,
                        PublicCert = Convert.ToBase64String(cert.Item1.GetRawCertData())
                    }
                },
                EmailHash = subject,
                Invitations = new List<UserInvitationDb>()
                {
                    new UserInvitationDb()
                    {
                        Code = "123456",
                        CreationTime = Instant.FromDateTimeUtc(DateTime.UtcNow),
                        IsConfirmed = true,
                    }
                }
            };
        }
    }
}