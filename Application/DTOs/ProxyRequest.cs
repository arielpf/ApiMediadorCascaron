namespace ApiMediadorCascaron.Application.DTOs;

/// <summary>
/// Modelo de entrada para el endpoint proxy.
/// </summary>
/// <param name="Key">Clave de negocio para resolución en caché Redis.</param>
/// <param name="Update">Bandera que define si se omite la lectura de Redis.</param>
public sealed record ProxyRequest(string Key, bool Update = false);
