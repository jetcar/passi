using System.Security.Cryptography.X509Certificates;
using System.Security;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using RestSharp;
using WebApiDto.SignUp;
using System.Net;

namespace RegisterManyUsersTestApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var cert = GenerateCertificate($"admin@passi.cloud").Result;

            using (var writer = new BinaryWriter(File.Open($"certs/admin.crt", FileMode.OpenOrCreate)))
            {
                writer.Write(cert.Item3);
            }

            var options = new RestClientOptions("https://localhost");
            options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            var i = 7000;

            var timeout = 30000;
            Directory.CreateDirectory("certs");
            var list = new int[1000000];
            Parallel.ForEach(list, new ParallelOptions() { MaxDegreeOfParallelism = 50 }, (a) =>
            {
                var value = Interlocked.Increment(ref i);
                var client = new RestClient(options);
                var guid = Guid.NewGuid();
                var code = CreateInvitation(timeout, value, client, guid);
                ConfirmRegistration(timeout, value, client, code, guid, cert);
                Console.WriteLine(value);
            });

        }

        private static string CreateInvitation(int timeout, int i, RestClient client, Guid guid)
        {
            var request = new RestRequest("/passiapi/api/test/signup", Method.Post);
            request.Timeout = timeout;
            request.AddJsonBody(new SignupDto()
            { DeviceId = $"admin{i}", Email = $"admin{i}@passi.cloud", UserGuid = guid });
            var restResponse = client.ExecuteAsync(request).Result;
            if (!restResponse.IsSuccessful)
                throw new Exception("error");

            return restResponse.Content.Replace("\"", "");
        }
        private static void ConfirmRegistration(int timeout, int i, RestClient client, string code, Guid guid,
            Tuple<X509Certificate2, string, byte[]> cert)
        {

            var request = new RestRequest("/passiapi/api/test/confirm", Method.Post);
            request.Timeout = timeout;
            request.AddJsonBody(new SignupConfirmationDto()
            { DeviceId = $"admin{i}", Email = $"admin{i}@passi.cloud", Code = code, Guid = guid.ToString(), PublicCert = Convert.ToBase64String(cert.Item1.GetRawCertData()) });
            var result = client.ExecuteAsync(request).GetAwaiter().GetResult();
            if (!result.IsSuccessful)
                throw new Exception("error");

        }


        public static async Task<Tuple<X509Certificate2, string, byte[]>> GenerateCertificate(string subject)
        {
            var random = new SecureRandom();
            var certificateGenerator = new X509V3CertificateGenerator();

            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            var sha512 = subject;
            certificateGenerator.SetIssuerDN(new X509Name($"C=NL, O=Passi, CN={sha512}"));
            certificateGenerator.SetSubjectDN(new X509Name($"C=NL, O=Passi, CN={sha512}"));
            certificateGenerator.SetNotBefore(DateTime.UtcNow.Date);
            certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(1));

            const int strength = 2048;
            var keyGenerationParameters = new KeyGenerationParameters(random, strength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);

            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            var issuerKeyPair = subjectKeyPair;
            const string signatureAlgorithm = "SHA256WithRSA";
            var signatureFactory = new Asn1SignatureFactory(signatureAlgorithm, issuerKeyPair.Private);
            var bouncyCert = certificateGenerator.Generate(signatureFactory);

            // Lets convert it to X509Certificate2
            X509Certificate2 certificate;

            Pkcs12Store store = new Pkcs12StoreBuilder().Build();
            store.SetKeyEntry($"{sha512}_key", new AsymmetricKeyEntry(subjectKeyPair.Private), new[] { new X509CertificateEntry(bouncyCert) });

            using (var ms = new MemoryStream())
            {
                store.Save(ms, new char[0], random);
                var rawBytes = ms.ToArray();
                certificate = new X509Certificate2(rawBytes, "", X509KeyStorageFlags.Exportable);
                var result = new Tuple<X509Certificate2, string, byte[]>(certificate, "", rawBytes);
                return result;
            }
        }
    }
}