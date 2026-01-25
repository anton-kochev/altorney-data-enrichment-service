using Infrastructure.Formatters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Extensions;

/// <summary>
/// Configures MVC options to include CSV formatters for input and output.
/// Uses IConfigureOptions pattern to enable proper dependency injection of ILoggerFactory
/// after the DI container is built.
/// </summary>
public sealed class CsvFormattersConfiguration : IConfigureOptions<MvcOptions>
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvFormattersConfiguration"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory for creating formatter loggers.</param>
    public CsvFormattersConfiguration(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Configures MVC options to include CSV input and output formatters.
    /// Formatters are inserted at position 0 to give them priority over default formatters.
    /// </summary>
    /// <param name="options">The MVC options to configure.</param>
    public void Configure(MvcOptions options)
    {
        var logger = _loggerFactory.CreateLogger<CsvInputFormatter>();
        options.InputFormatters.Insert(0, new CsvInputFormatter(logger));
        options.OutputFormatters.Insert(0, new CsvOutputFormatter());
    }
}
