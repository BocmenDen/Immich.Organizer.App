namespace Immich.Client
{
    public class ImmichClientBuilder
    {
        public static ImmichClient Build(string host, string api)
        {
            HttpClient httpClient = new()
            {
                BaseAddress = new Uri(host)
            };
            httpClient.DefaultRequestHeaders.Add("x-api-key", api);
            return new(httpClient);
        }
    }
}