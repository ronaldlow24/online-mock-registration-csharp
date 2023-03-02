using Amazon.SQS;
using Amazon.SQS.Model;
using OnlineMockRegistration.Helper;
using OnlineMockRegistration.Models;
using Quartz;
using System.Net;
using System.Text.Json;

namespace OnlineMockRegistration.Cron
{
    [DisallowConcurrentExecution]
    public class TriggerCronJob : IJob
    {
        private readonly IAmazonSQS _sqsCLient;

        public TriggerCronJob(IAmazonSQS sqsCLient)
        {
            _sqsCLient = sqsCLient;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                //get queue url
                var getQueueResponse = await _sqsCLient.GetQueueUrlAsync(Utility.QueueName);
                if (getQueueResponse.HttpStatusCode != HttpStatusCode.OK)
                    return;

                //get message from queue
                var receiveMessageRequest = new ReceiveMessageRequest
                {
                    QueueUrl = getQueueResponse.QueueUrl,
                    MaxNumberOfMessages = 10
                };

                var receiveMessageResponse = await _sqsCLient.ReceiveMessageAsync(receiveMessageRequest);
                if (receiveMessageResponse.HttpStatusCode != HttpStatusCode.OK)
                    return;

                if (!receiveMessageResponse.Messages.Any())
                    return;

                ParallelOptions parallelOptions = new()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                await Parallel.ForEachAsync(receiveMessageResponse.Messages, parallelOptions, async (item, token) =>
                {
                    var messageBody = JsonSerializer.Deserialize<MessageModel>(item.Body);

                    //send email
                    await Utility.SendMailAsync("Online mock registration successful!", $"Online mock registration successful! {messageBody.Name}", new List<string> { messageBody.Email }, token);

                    //delete message from queue
                    var deleteMessageRequest = new DeleteMessageRequest
                    {
                        QueueUrl = getQueueResponse.QueueUrl,
                        ReceiptHandle = item.ReceiptHandle
                    };

                    await _sqsCLient.DeleteMessageAsync(deleteMessageRequest, token);
                });

            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
