using System;
using System.Collections.Generic;
using System.Linq;
using Models;
using WebApiDto.Auth.Dto;

namespace Services
{
    public class ObjectMapper : IMapper
    {
        public TDestination Map<TDestination>(object source)
        {
            if (source == null)
                return default;

            var destinationType = typeof(TDestination);

            // UserDb -> UserDto
            if (source is UserDb userDb && destinationType == typeof(UserDto))
            {
                return (TDestination)(object)new UserDto
                {
                    CreationTime = userDb.CreationTime.ToDateTimeUtc(),
                    Id = userDb.Id,
                    EmailHash = userDb.EmailHash,
                    Guid = userDb.Guid,
                    DeviceId = userDb.DeviceId,
                    Device = userDb.Device != null ? Map<DeviceDto>(userDb.Device) : null,
                    Certificates = userDb.Certificates?.Select(Map<CertificateDto>).ToList(),
                    Invitations = userDb.Invitations?.Select(Map<UserInvitationDto>).ToList(),
                    SessionUsers = userDb.SessionUsers?.Select(Map<SessionDto>).ToList()
                };
            }

            // DeviceDb -> DeviceDto
            if (source is DeviceDb deviceDb && destinationType == typeof(DeviceDto))
            {
                return (TDestination)(object)new DeviceDto
                {
                    Id = deviceDb.Id,
                    DeviceId = deviceDb.DeviceId,
                    NotificationToken = deviceDb.NotificationToken,
                    Platform = deviceDb.Platform,
                    CreationTime = deviceDb.CreationTime.ToDateTimeUtc()
                };
            }

            // CertificateDb -> CertificateDto
            if (source is CertificateDb certDb && destinationType == typeof(CertificateDto))
            {
                return (TDestination)(object)new CertificateDto
                {
                    Thumbprint = certDb.Thumbprint,
                    UserId = certDb.UserId,
                    PublicCert = certDb.PublicCert,
                    ParentCertSignature = certDb.ParentCertSignature,
                    ParentCertId = certDb.ParentCertId,
                    CreationTime = certDb.CreationTime.ToDateTimeUtc()
                };
            }

            // UserInvitationDb -> UserInvitationDto
            if (source is UserInvitationDb invitationDb && destinationType == typeof(UserInvitationDto))
            {
                return (TDestination)(object)new UserInvitationDto
                {
                    CreationTime = invitationDb.CreationTime.ToDateTimeUtc(),
                    Id = invitationDb.Id,
                    UserId = invitationDb.UserId,
                    Code = invitationDb.Code,
                    IsConfirmed = invitationDb.IsConfirmed
                };
            }

            // SimpleSessionDb -> SessionDto
            if (source is SimpleSessionDb sessionDb && destinationType == typeof(SessionDto))
            {
                return (TDestination)(object)new SessionDto
                {
                    CreationTime = sessionDb.CreationTime.ToDateTimeUtc(),
                    Guid = sessionDb.Guid,
                    UserId = sessionDb.UserId,
                    Status = sessionDb.Status.HasValue ? (SessionStatusDto)sessionDb.Status.Value : null,
                    SignedHash = sessionDb.SignedHashNew,
                    ExpirationTime = sessionDb.ExpirationTime.ToDateTimeUtc(),
                    ClientId = null,
                    RandomString = null,
                    PublicCertThumbprint = null,
                    CheckColor = null,
                    ReturnUrl = null,
                    ErrorMessage = null
                };
            }

            // SessionTempRecord -> SessionDto
            if (source is SessionTempRecord sessionTemp && destinationType == typeof(SessionDto))
            {
                return (TDestination)(object)new SessionDto
                {
                    CreationTime = DateTime.UtcNow,
                    Guid = sessionTemp.Guid,
                    UserId = sessionTemp.UserId,
                    ClientId = sessionTemp.ClientId,
                    RandomString = sessionTemp.RandomString,
                    Status = sessionTemp.Status.HasValue ? (SessionStatusDto)sessionTemp.Status.Value : null,
                    SignedHash = null,
                    PublicCertThumbprint = sessionTemp.PublicCertThumbprint,
                    CheckColor = sessionTemp.CheckColor,
                    ReturnUrl = sessionTemp.ReturnUrl,
                    ExpirationTime = sessionTemp.ExpirationTime,
                    ErrorMessage = sessionTemp.ErrorMessage
                };
            }

            throw new NotSupportedException($"Mapping from {source.GetType().Name} to {destinationType.Name} is not supported");
        }
    }
}
