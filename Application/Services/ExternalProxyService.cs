using ApiMediadorCascaron.Application.Interfaces;
using ApiMediadorCascaron.Application.Metrics;
using Microsoft.AspNetCore.WebUtilities;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ApiMediadorCascaron.Application.Services;

/// <summary>
/// Implementa la lógica de mediación/proxy con lectura/escritura en Redis.
/// </summary>
public sealed class ExternalProxyService : IExternalProxyService
{
    private readonly HttpClient _httpClient;
    private readonly IDatabase _redisDatabase;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Inicializa una nueva instancia del servicio de proxy.
    /// </summary>
    public ExternalProxyService(
        HttpClient httpClient,
        IConnectionMultiplexer connectionMultiplexer,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;

        // Permite seleccionar la base de datos lógica de Redis vía appsettings.
        var databaseId = _configuration.GetValue<int?>("Redis:DatabaseId") ?? 0;
        _redisDatabase = connectionMultiplexer.GetDatabase(databaseId);
    }

    /// <inheritdoc />
    public async Task<JsonNode> GetOrRefreshAsync(string cacheKey, bool update, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            throw new ArgumentException("The cache key is required.", nameof(cacheKey));
        }

        var ttlMinutes = _configuration.GetValue<int?>("Redis:DefaultTtlMinutes") ?? 30;

        // Requisito: si update=false se debe intentar resolver desde Redis antes de consultar externo.
        if (!update)
        {
            var cachedValue = await _redisDatabase.StringGetAsync(cacheKey).ConfigureAwait(false);
            if (cachedValue.HasValue)
            {
                ProxyMetrics.CacheHitCounter.Inc();
                return JsonNode.Parse(cachedValue.ToString())
                    ?? throw new JsonException("Cached value is not a valid JSON payload.");
            }

            ProxyMetrics.CacheMissCounter.Inc();
        }

        var baseUrl = _configuration["ExternalApi:BaseUrl"]
            ?? throw new InvalidOperationException("ExternalApi:BaseUrl is not configured.");
        var queryParamName = _configuration["ExternalApi:CacheKeyQueryParam"] ?? "key";

        var requestUrl = QueryHelpers.AddQueryString(baseUrl, queryParamName, cacheKey);

        using var latencyTimer = ProxyMetrics.ExternalLatencyHistogram.NewTimer();
        using var response = await _httpClient.GetAsync(requestUrl, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var externalPayload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var payloadNode = JsonNode.Parse(externalPayload)
            ?? throw new JsonException("External response is not a valid JSON payload.");

        // 🛠️ TRANSFORMACIÓN DE RESPUESTA:
        // Aquí puedes mapear, enriquecer o agregar campos al JSON externo antes de persistir en Redis.
        // Ejemplo:
        // if (payloadNode is JsonObject jsonObject)
        // {
        //     jsonObject["proxyProcessedAtUtc"] = DateTime.UtcNow;
        //     jsonObject["source"] = "external-api";
        // }

        var normalizedJson = payloadNode.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = false
        });

        // Requisito: siempre persistir/sobrescribir en Redis cuando se consulta el origen externo.
        await _redisDatabase.StringSetAsync(
            cacheKey,
            normalizedJson,
            TimeSpan.FromMinutes(ttlMinutes)).ConfigureAwait(false);

        return payloadNode;
    }
}
