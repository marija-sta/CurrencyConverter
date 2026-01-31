using System.Net;
using Polly;

namespace CurrencyConverter.Infrastructure.Helpers;

internal static class FrankfurterTransientErrorClassifier
{
	internal static bool IsTransient(Outcome<HttpResponseMessage> outcome)
	{
		if (outcome.Exception is not null)
		{
			return true;
		}

		var response = outcome.Result;
		if (response is null)
		{
			return true;
		}

		var statusCode = (int)response.StatusCode;

		if (statusCode >= 500)
		{
			return true;
		}

		return response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests;
	}
}