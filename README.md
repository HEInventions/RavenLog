# RavenLog
A C# wrapper that simplifies SharpRaven configuration and sending sentry events. The wrapper follows a Singleton pattern and therefore only one instance can be used throughout a project.

![Blip](https://img.shields.io/badge/nuget-v1.1.0-blue.svg)

#Client Usage

```C#
using HEInventions.Logging;

// The Sentry DSN client key found in your Sentry project settings.
String Sentry_DSN = "https://1234abc@testsentry.com/3";
// Set the threshold level for Sentry messages. Choose from Debug, Info, Warning, Error, and Fatal.
String ErrorThreshold = "Error";
// (Optional) Set additional tags if necessary to send up with each Sentry message (e.g. useful for filtering in Sentry).
Dictionary<string, string> ExtraTags = new Dictionary<string, string>
{
    { "customer", "Joe Bloggs" },
    { "location", "Smallville" }
};

// Configure the RavenLog client. The client must be configured before sending Sentry messages.
RavenLog.Configure(Sentry_DSN, ErrorThreshold, ExtraTags);

// You can also use the Action delegate any time a Sentry event is sent.
RavenLog.Instance.OnMessage += (SentryEvent) =>
{
    Console.WriteLine(SentryEvent.Message);
};

// Example 1 - Send up an error message.
int num1 = 5;
int num2 = 0;
try
{
    var x = num1 / num2;
}
catch (DivideByZeroException ex)
{
    RavenLog.Instance.Error("Attempted to divide by zero");
}

// Example 2 - Send up an error message and the exception object.
int num1 = 5;
int num2 = 0;
try
{
    var x = num1 / num2;
}
catch (DivideByZeroException ex)
{
    RavenLog.Instance.Error("Attempted to divide by zero", ex);
}
```
**Note: As we set the threshold to "Error" above, anything below Error, e.g. ```RavenLog.Instance.Warn("Attempted to divide by zero")``` will not be sent to Sentry.**

### For reference:
```c#
RavenLog.Instance.Fatal("");
RavenLog.Instance.Error("");
RavenLog.Instance.Warn("");
RavenLog.Instance.Info("");
RavenLog.Instance.Debug("");
```
