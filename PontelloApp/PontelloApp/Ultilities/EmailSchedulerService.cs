using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PontelloApp.Data;
using PontelloApp.Ultilities;

namespace PontelloApp.Utilities
{
    public class EmailSchedulerService : BackgroundService
    {
        private readonly IServiceProvider _provider;

        public EmailSchedulerService(IServiceProvider provider)
        {
            _provider = provider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailSender = scope.ServiceProvider.GetRequiredService<EmailSender>();

                var dueEmails = db.ScheduledEmails
                    .Where(e => e.IsActive && e.NextSendAt <= DateTime.Now)
                    .ToList();

                foreach (var schedule in dueEmails)
                {
                    //Skips code
                    if (!schedule.IsActive)
                        continue; 

                    var tempPath = Path.Combine(Path.GetTempPath(), schedule.AttachmentName);
                    System.IO.File.WriteAllBytes(tempPath, schedule.AttachmentBytes);

                    await emailSender.SendEmailWithAttachmentAsync(
                        schedule.Email,
                        schedule.Subject,
                        schedule.HtmlBody,
                        tempPath                    
                    );

                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }

                    //remove/comment out will send recur messages, will continue if run app again
                    schedule.IsActive = false; //will send recur message once

                }

                await db.SaveChangesAsync();

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
