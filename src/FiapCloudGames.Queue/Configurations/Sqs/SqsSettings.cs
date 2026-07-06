namespace FiapCloudGames.Queue.Configurations.Sqs;

public class SqsSettings
{
    public string Region { get; set; } = "us-east-1";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty; // Ex: http://localhost:4566 para LocalStack
}