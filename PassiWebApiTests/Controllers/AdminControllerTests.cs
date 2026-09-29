using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Models;
using NUnit.Framework;
using passi_webapi.Controllers;
using Repos;
using Services;

namespace PassiWebApiTests.Controllers
{
    public class AdminControllerTests : TestBase
    {
        // Matches the controller's private `pagesize` field.
        private const int PageSize = 100;

        [Test]
        public async System.Threading.Tasks.Task GetUsersSkipsByPageTimesPageSizeNotPageSquared()
        {
            using var scope = ServiceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

            // Seed more than two full pages of brand-new users. Because CreationTime is stamped by
            // the DB context on insert and GetUsers orders by CreationTime descending, this batch is
            // guaranteed to occupy the top of the ordering ahead of anything earlier tests created.
            // Inserted one row (and SaveChanges call) at a time so each gets a distinct
            // CreationTime timestamp; the DbContext stamps CreationTime from the system clock on
            // SaveChanges, and a single batched insert can produce ties at the page boundary,
            // which makes an ORDER BY CreationTime + OFFSET/LIMIT query non-deterministic there.
            var markerPrefix = Guid.NewGuid().ToString();
            var seededEmails = new List<string>();
            for (var i = 0; i < 2 * PageSize + 5; i++)
            {
                var email = $"{markerPrefix}-{i}@passi.cloud";
                seededEmails.Add(email);
                dbContext.Users.Add(new UserDb { EmailHash = email, Guid = Guid.NewGuid() });
                dbContext.SaveChanges();
            }

            var controller = new AdminController(dbContext, mapper);

            var page0 = await controller.GetUsers(0);
            var page1 = await controller.GetUsers(1);

            Assert.That(page0, Has.Count.EqualTo(PageSize));
            Assert.That(page1, Has.Count.EqualTo(PageSize));

            var seededEmailSet = new HashSet<string>(seededEmails);
            var page0Emails = page0.Select(x => x.EmailHash).ToList();
            var page1Emails = page1.Select(x => x.EmailHash).ToList();

            // Both pages are drawn entirely from our freshly-seeded, most-recent users.
            Assert.That(page0Emails.All(seededEmailSet.Contains), Is.True,
                "Page 0 should only contain freshly-seeded users");
            Assert.That(page1Emails.All(seededEmailSet.Contains), Is.True,
                "Page 1 should only contain freshly-seeded users");

            // With the correct Skip(page * pagesize), page 0 and page 1 must not overlap.
            // The previous Skip(page * page) bug made page 1 skip only 1 record, so it shared
            // 99 of its 100 rows with page 0.
            var overlap = page0Emails.Intersect(page1Emails).ToList();
            Assert.That(overlap, Is.Empty,
                "Page 1 should contain a fresh set of users, not (almost) the same rows as page 0");
        }
    }
}
