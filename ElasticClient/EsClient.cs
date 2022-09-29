using System.Net.Http.Headers;
using ElasticClient;
using ElasticClient.Extensions;
using Localization.Libs;
using Processor.Api.Exceptions;

public class EsClient
{
    private readonly string ElasticSearchNotHealthy = ElasticClientResources.ElasticSearchNotHealthy;

    private const string AuthorizationHeaderKey = "Authorization";
    private const string ContentTypeHeaderValue = "application/json";
    private HostConfig HostConfig { get; }
    private AuthenticationCredentials? AuthCredentials { get; }

    public EsClient(EsClientConfig esClientConfig)
    {
        HostConfig = esClientConfig.Host;
        AuthCredentials = esClientConfig.GetAuthCredentials();
    }

    private HttpClient Client
    {
        get
        {
            var httpClient = new HttpClient();
            if (AuthCredentials != null)
            {
                httpClient.DefaultRequestHeaders.Add(AuthorizationHeaderKey, AuthCredentials.ToHeaderValue());
            }

            var acceptHeader = new MediaTypeWithQualityHeaderValue(ContentTypeHeaderValue);
            httpClient.DefaultRequestHeaders.Accept.Add(acceptHeader); //ACCEPT header
            return httpClient;
        }
    }

    public async Task CheckElasticAvailable(double secondsToResponse)
    {
        var requestIri = new Uri($"https://{HostConfig.Host}:{HostConfig.Port}/_cat/health");
        var delay = TimeSpan.FromSeconds(secondsToResponse);
        var cts = new CancellationTokenSource(delay);

        try
        {
            var responseMessage = await Client.GetAsync(requestIri, cts.Token);

            if (!responseMessage.IsSuccessStatusCode)
            {
                var responseContent = await responseMessage.Content.ReadAsStringAsync(CancellationToken.None);
                throw new ElasticSearchNotHealthyException(responseContent);
            }
        }
        catch (TaskCanceledException e)
        {
            throw new ElasticSearchNotAvailableException(HostConfig.Host, HostConfig.Port, delay.Seconds);
        }
    }

    public EsResponse WriteRecord(string index, CancellationToken token, string data, string? docId = null, string? type = "_doc")
    {
        var parameters = new RequestParameters(index, type, docId);
        return WriteRecord(parameters, token, data);
    }

    public EsResponse WriteRecord(RequestParameters requestParameters, CancellationToken token, string data)
    {
        var request = new EsRequest(HostConfig, requestParameters, data);
        return WriteRecord(request, token);
    }

    public EsResponse WriteRecord(EsRequest esRequest, CancellationToken token) //todo fix dat
    {
        var requestMessage = esRequest.ToHttpRequestMessage();
        var result = Client.Send(requestMessage, token).ToEsResponseAsync().Result;
        return result;
    }

    public Task<EsResponse> WriteRecordAsync(string index, CancellationToken token, string? data = null, string? docId = null, string? type = "_doc")
    {
        var parameters = new RequestParameters(index, type, docId);
        return WriteRecordAsync(parameters, token, data);
    }

    public async Task<EsResponse> WriteRecordAsync(RequestParameters requestParameters, CancellationToken token, string? data = null)
    {
        var request = new EsRequest(HostConfig, requestParameters, data);
        return await WriteRecordAsync(request, token);
    }

    public async Task<EsResponse> WriteRecordAsync(EsRequest esRequest, CancellationToken token)
    {
        var requestMessage = esRequest.ToHttpRequestMessage();

        var response = await Client.SendAsync(requestMessage, token);
        var asEsResponse = await response.ToEsResponseAsync();
        return asEsResponse;
    }
}