namespace ApiMediadorCascaron.Application.DTOs;

/// <summary>
/// Modelo de entrada para el endpoint proxy.
/// </summary>
/// <param name="Endpoint">Identificador del endpoint externo configurado (ej: obtener-persona).</param>
/// <param name="Parametro">Valor de entrada para el query string del servicio externo.</param>
/// <param name="Update">Bandera que define si se omite la lectura de Redis.</param>
public sealed record ProxyRequest(string Endpoint, string Parametro, bool Update = false);
