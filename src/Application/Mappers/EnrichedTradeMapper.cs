using Application.DTOs;
using Domain.Entities;

namespace Application.Mappers;

/// <summary>
/// Provides mapping operations between EnrichedTrade domain entity and EnrichedTradeOutputDto.
/// </summary>
public static class EnrichedTradeMapper
{
    /// <summary>
    /// Maps an EnrichedTrade domain entity to an EnrichedTradeOutputDto.
    /// </summary>
    /// <param name="entity">The enriched trade entity.</param>
    /// <param name="priceString">The original trimmed price string for format preservation.</param>
    /// <returns>The output DTO.</returns>
    public static EnrichedTradeOutputDto ToDto(EnrichedTrade entity, string priceString)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(priceString);

        return new EnrichedTradeOutputDto
        {
            Date = entity.Date.FormattedValue,
            ProductName = entity.ProductName,
            Currency = entity.Currency.Value,
            Price = priceString
        };
    }
}
