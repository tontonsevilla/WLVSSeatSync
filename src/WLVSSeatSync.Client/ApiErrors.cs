using System.Net;
using System.Text.Json;

using Refit;

namespace WLVSSeatSync.Client;

public static class ApiErrors
{
    public static string Describe(ApiException? error)
    {
        if (error?.StatusCode == HttpStatusCode.TooManyRequests)
            return "Too many attempts. Please wait a minute and try again.";

        if (!string.IsNullOrWhiteSpace(error?.Content))
        {
            try
            {
                var problem = JsonSerializer.Deserialize<ApiProblem>(error.Content, JsonSerializerOptions.Web);
                var fieldError = problem?.Errors?.Values.SelectMany(messages => messages).FirstOrDefault();
                var message = fieldError ?? problem?.Detail ?? problem?.Title;
                if (!string.IsNullOrWhiteSpace(message))
                    return message;
            }
            catch (JsonException)
            {
                // Not a ProblemDetails body; use the generic message below.
            }
        }

        return "Something went wrong. Please try again.";
    }
}
