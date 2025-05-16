using Microsoft.Extensions.Logging;

using Polly;

namespace PaymentGateway.Api.Extensions
{
    public static class PollyContextExtensions
    {
        public static Context WithLogger(this Context context, ILogger logger)
        {
            context["Logger"] = logger;
            return context;
        }

        public static ILogger GetLogger(this Context context)
        {
            if (context.TryGetValue("Logger", out var logger))
            {
                return logger as ILogger;
            }
            return null;
        }
    }
}
