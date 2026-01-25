using System.Globalization;
using Application.DTOs;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Mappers;

/// <summary>
/// Provides mapping operations between TradeInputDto and Trade domain entity.
/// </summary>
public static class TradeMapper
{
    /// <summary>
    /// Result of attempting to map a TradeInputDto to a Trade entity.
    /// </summary>
    public sealed record MappingResult
    {
        /// <summary>
        /// The Trade entity if mapping succeeded; null if validation failed.
        /// </summary>
        public Trade? Trade { get; init; }

        /// <summary>
        /// The trimmed price string for output preservation.
        /// </summary>
        public string TrimmedPrice { get; init; } = string.Empty;

        /// <summary>
        /// True if mapping succeeded.
        /// </summary>
        public bool IsSuccess => Trade is not null;

        /// <summary>
        /// Validation failure details if mapping failed.
        /// </summary>
        public ValidationFailure? Failure { get; init; }
    }

    /// <summary>
    /// Details about why validation failed.
    /// </summary>
    public sealed record ValidationFailure
    {
        public required ValidationFailureType Type { get; init; }
        public List<string>? MissingFields { get; init; }
        public string? InvalidDateReason { get; init; }
        public TradeInputDto RawInput { get; init; } = null!;
    }

    /// <summary>
    /// Type of validation failure.
    /// </summary>
    public enum ValidationFailureType
    {
        MissingFields,
        InvalidDateFormat,
        InvalidProductId,
        InvalidCurrency,
        InvalidPrice
    }

    /// <summary>
    /// Attempts to map a TradeInputDto to a Trade domain entity.
    /// </summary>
    /// <param name="input">The input DTO to map.</param>
    /// <returns>A MappingResult containing the Trade entity or validation failure details.</returns>
    public static MappingResult TryMapToTrade(TradeInputDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Step 1: Check for missing required fields
        var missingFields = CollectMissingFields(input);
        if (missingFields.Count > 0)
        {
            return new MappingResult
            {
                Failure = new ValidationFailure
                {
                    Type = ValidationFailureType.MissingFields,
                    MissingFields = missingFields,
                    RawInput = input
                }
            };
        }

        // Step 2: Validate and create TradeDate
        TradeDate tradeDate;
        try
        {
            tradeDate = TradeDate.Create(input.Date);
        }
        catch (ArgumentException ex)
        {
            return new MappingResult
            {
                Failure = new ValidationFailure
                {
                    Type = ValidationFailureType.InvalidDateFormat,
                    InvalidDateReason = ex.Message,
                    RawInput = input
                }
            };
        }

        // Step 3: Validate and create ProductIdentifier
        ProductIdentifier productId;
        try
        {
            if (!int.TryParse(input.ProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedProductId))
            {
                return new MappingResult
                {
                    Failure = new ValidationFailure
                    {
                        Type = ValidationFailureType.InvalidProductId,
                        RawInput = input
                    }
                };
            }
            productId = ProductIdentifier.Create(parsedProductId);
        }
        catch (ArgumentException)
        {
            return new MappingResult
            {
                Failure = new ValidationFailure
                {
                    Type = ValidationFailureType.InvalidProductId,
                    RawInput = input
                }
            };
        }

        // Step 4: Validate and create Currency
        Currency currency;
        try
        {
            currency = Currency.Create(input.Currency);
        }
        catch (ArgumentException)
        {
            return new MappingResult
            {
                Failure = new ValidationFailure
                {
                    Type = ValidationFailureType.InvalidCurrency,
                    RawInput = input
                }
            };
        }

        // Step 5: Validate and create Price
        Price price;
        string trimmedPrice = input.Price.Trim();
        try
        {
            if (!decimal.TryParse(trimmedPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsedPrice))
            {
                return new MappingResult
                {
                    Failure = new ValidationFailure
                    {
                        Type = ValidationFailureType.InvalidPrice,
                        RawInput = input
                    }
                };
            }
            price = Price.Create(parsedPrice);
        }
        catch (ArgumentException)
        {
            return new MappingResult
            {
                Failure = new ValidationFailure
                {
                    Type = ValidationFailureType.InvalidPrice,
                    RawInput = input
                }
            };
        }

        // All validation passed - create Trade entity
        var trade = Trade.Create(tradeDate, productId, currency, price);

        return new MappingResult
        {
            Trade = trade,
            TrimmedPrice = trimmedPrice
        };
    }

    /// <summary>
    /// Collects names of missing or empty required fields.
    /// </summary>
    private static List<string> CollectMissingFields(TradeInputDto input)
    {
        var missing = new List<string>(4);
        if (string.IsNullOrWhiteSpace(input.Date)) missing.Add("date");
        if (string.IsNullOrWhiteSpace(input.ProductId)) missing.Add("productId");
        if (string.IsNullOrWhiteSpace(input.Currency)) missing.Add("currency");
        if (string.IsNullOrWhiteSpace(input.Price)) missing.Add("price");
        return missing;
    }
}
