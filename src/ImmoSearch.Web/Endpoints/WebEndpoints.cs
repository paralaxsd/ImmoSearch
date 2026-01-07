namespace ImmoSearch.Web.Endpoints;

public static class WebEndpoints
{
    public static void MapApiProxy(this WebApplication app)
    {
        app.Map("/api/{**path}", async (HttpContext context, string path) =>
        {
            var clientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = clientFactory.CreateClient("ApiProxy");

            var requestMessage = new HttpRequestMessage
            {
                Method = new HttpMethod(context.Request.Method),
                RequestUri = new Uri(client.BaseAddress!, path),
                Content = new StreamContent(context.Request.Body)
            };

            foreach (var header in context.Request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            var response = await client.SendAsync(requestMessage, context.RequestAborted);
            context.Response.StatusCode = (int)response.StatusCode;

            foreach (var header in response.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in response.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            await response.Content.CopyToAsync(context.Response.Body);
        });
    }
}