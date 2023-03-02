using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.AspNetCore.Mvc;
using OnlineMockRegistration.Helper;
using OnlineMockRegistration.Models;
using System.Net;
using System.Text.Json;

namespace OnlineMockRegistration.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAmazonSQS _sqsCLient;

        public HomeController(ILogger<HomeController> logger, IAmazonSQS sqsCLient)
        {
            _logger = logger;
            _sqsCLient = sqsCLient;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> PostNameEmail(PostNameEmailInputModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest();

                var isEmailValid = Utility.ValidateEmail(model.Email);

                if(!isEmailValid)
                    return BadRequest();

                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    return BadRequest();
                }

                var queueMessage = new MessageModel { Name = model.Name, Email = model.Email, CreatedTimestamp = DateTimeOffset.UtcNow };

                //create queue if not exist
                var getQueueResponse = await _sqsCLient.GetQueueUrlAsync(Utility.QueueName);
                if (getQueueResponse.HttpStatusCode != HttpStatusCode.OK)
                {
                    var createQueueRequest = new CreateQueueRequest(Utility.QueueName);
                    await _sqsCLient.CreateQueueAsync(createQueueRequest);
                }

                //send message to queue
                var sendMessageRequest = new SendMessageRequest
                {
                    QueueUrl = getQueueResponse.QueueUrl,
                    MessageBody = JsonSerializer.Serialize(queueMessage)
                };

                await _sqsCLient.SendMessageAsync(sendMessageRequest);

                return Ok(queueMessage);
            }
            catch
            {
                throw;
            }
        }
    }
}