using System.Text.Json.Nodes;

namespace ApiMediadorCascaron.Application.Interfaces;

/// <summary>
/// Define el contrato de orquestación para el proxy hacia servicio externo con soporte de caché Redis.
/// </summary>
public interface IExternalProxyService
{
    /// <summary>
    /// Obtiene datos externos con comportamiento condicional de caché según el parámetro <paramref name="update"/>.
    /// </summary>
    /// <param name="endpointKey">Identificador lógico del endpoint externo configurado.</param>
    /// <param name="cacheKey">Clave lógica de caché en Redis.</param>
    /// <param name="update">Si es true, omite lectura de Redis y fuerza actualización desde origen externo.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación asíncrona.</param>
    /// <returns>Respuesta JSON normalizada del origen externo o desde caché.</returns>
    Task<JsonNode> GetOrRefreshAsync(string endpointKey, string cacheKey, bool update, CancellationToken cancellationToken);
}
