using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Gml.Client.Interfaces;
using Gml.Dto.Messages;
using Newtonsoft.Json;

namespace Gml.Client.Helpers;

public partial class ApiProcedures
{
    private static readonly TimeSpan RoutesCallTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     The entry points to the profile's game server, main address first. <see cref="ResponseMessage{T}.Data" />
    ///     is an empty list when the profile has no servers and <c>null</c> when the routes could not be loaded
    ///     (unknown profile, API down, broken answer); this never throws for those.
    /// </summary>
    public async Task<ResponseMessage<List<ServerRouteDto>>> GetServerRoutes(string profileName, string accessToken)
    {
        try
        {
            // The shared HttpClient has no timeout of its own (it also streams downloads), so this call gets one.
            using var timeout = new CancellationTokenSource(RoutesCallTimeout);
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"/api/v1/servers/{Uri.EscapeDataString(profileName)}/routes");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, timeout.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return new ResponseMessage<List<ServerRouteDto>>();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonConvert.DeserializeObject<ResponseMessage<List<ServerRouteDto>>>(content)
                   ?? new ResponseMessage<List<ServerRouteDto>>();
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException
                                              or JsonException)
        {
            return new ResponseMessage<List<ServerRouteDto>>();
        }
    }
}
