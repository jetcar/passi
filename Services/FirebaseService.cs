using System;
using System.Collections.Generic;
using System.Threading;
using ConfigurationManager;
using FirebaseAdmin.Messaging;
using Repos;
using Serilog.Core;
using AppCommon;
using Microsoft.EntityFrameworkCore;
using PostSharp.Extensibility;
using Message = FirebaseAdmin.Messaging.Message;

namespace Services
{
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class FirebaseService : IFirebaseService
    {
        private AppSetting _appSetting;
        private CurrentContext _currentContext;
        private IFireBaseClient _fireBaseClient;
        private IRedisService _redisService;

        public FirebaseService(AppSetting appSetting, CurrentContext currentContext, IFireBaseClient fireBaseClient, IRedisService redisService)
        {
            _appSetting = appSetting;
            _currentContext = currentContext;
            _fireBaseClient = fireBaseClient;
            _redisService = redisService;
        }

        public virtual string SendNotification(string clientToken, string title, string body, string notification, Guid sessionId)
        {
            var registrationTokens = clientToken;
            var message = new Message()
            {
                Token = registrationTokens,
                Data = new Dictionary<string, string>()
                {
                    {"title", title},
                    {"body", body},
                },
                Notification = new Notification()
                {
                    Title = title,
                    Body = notification,
                },
                //Android = new AndroidConfig() { TimeToLive = new TimeSpan(0, 1, 0) }
            };
            var appsetting = _appSetting;
            var currentContext = _currentContext;
            new Thread(() =>
            {
                Thread.Sleep(3000);
                try
                {
                    var response = _fireBaseClient.Send(message);
                }
                catch (Exception e)
                {
                    var sessionrepo = new SessionsRepository(new PassiDbContext(appsetting, currentContext, Logger.None), appsetting, _redisService);
                    var strategy = sessionrepo.GetExecutionStrategy();
                    strategy.Execute(() =>
                    {
                        using (var transaction = sessionrepo.BeginTransaction())
                        {
                            var session = sessionrepo.GetSessionById(sessionId);
                            session.Status = Models.SessionStatus.Error;
                            session.ErrorMessage = e.Message.Truncate(256);
                            sessionrepo.Update(session);
                            transaction.Commit();
                        }
                    });

                }
            }).Start();
            return "";
        }
    }

    public interface IFirebaseService
    {
        string SendNotification(string clientToken, string title, string body, string notification, Guid sessionId);
    }
}