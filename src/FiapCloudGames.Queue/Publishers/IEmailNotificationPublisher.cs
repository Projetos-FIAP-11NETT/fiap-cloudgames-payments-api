using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using FiapCloudGames.Queue.Configurations.Sqs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Queue.Publishers;

public interface IEmailNotificationPublisher
{
    Task PublishAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}