using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using FiapCloudGames.Payments.Application.Interfaces;
using FiapCloudGames.Queue.Configurations.Sqs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Queue.Publishers;

public class EmailNotificationPublisher(
    IAmazonSQS sqsClient,
    IOptions<SqsSettings> sqsSettings,
    ILogger<EmailNotificationPublisher> logger,
    ICorrelationContext correlationContext) : IEmailNotificationPublisher
{
    public async Task PublishAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[payments-service] CorrelationId: {CorrelationId} | Publishing email notification to SQS: To={To}, Subject={Subject}",
            correlationContext.CorrelationId, to, subject);

        var messageBody = JsonSerializer.Serialize(new
        {
            To = to,
            Subject = subject,
            Body = body,
            CorrelationId = correlationContext.CorrelationId
        });

        await sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = sqsSettings.Value.EmailQueueUrl,
            MessageBody = messageBody
        }, cancellationToken);
    }
}