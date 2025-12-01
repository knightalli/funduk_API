namespace OrderService.Domain;

/// <summary>
/// Файл OrderId.cs удаляй, этот код переноси в Common.Contracts в Ids.cs. Потом using поменять надо будет
/// </summary>
public readonly record struct OrderId(Guid Value);