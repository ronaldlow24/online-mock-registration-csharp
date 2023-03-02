using System.Security;

namespace OnlineMockRegistration
{
    public class ApplicationConfiguration
    {
        public static ApplicationConfiguration StaticCurrent { get; set; } = default!;
        public string AWSKey { get; set; } = string.Empty;
        public string AWSSecret { get; set; } = string.Empty;
        public string AWSRegion { get; set; } = string.Empty;
        public string EmailSmtpServer { get; internal set; }
        public string EmailPort { get; internal set; }
        public string EmailUserName { get; internal set; }
        public string EmailFrom { get; internal set; }
        public string EmailPassword { get; internal set; }
    }
}
