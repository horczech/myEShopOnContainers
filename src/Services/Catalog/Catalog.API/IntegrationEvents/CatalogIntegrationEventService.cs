using Microsoft.eShopOnContainers.Services.Catalog.API.IntegrationEvents;

namespace Catalog.API.IntegrationEvents;

public class CatalogIntegrationEventService : ICatalogIntegrationEventService, IDisposable
{
    private readonly ILogger<CatalogIntegrationEventService> _logger;

    public CatalogIntegrationEventService(ILogger<CatalogIntegrationEventService> logger)
    {
        _logger = logger;
    }
    
    
    public Task SaveEventAndCatalogContextChangesAsync(IntegrationEvent evt)
    {
        _logger.LogWarning("NOT IMPLEMENTED!!!");
        return Task.CompletedTask;
    }

    public Task PublishThroughEventBusAsync(IntegrationEvent evt)
    {
        _logger.LogWarning("NOT IMPLEMENTED!!!");
        return Task.CompletedTask;    }

    public void Dispose()
    {
        _logger.LogWarning("NOT IMPLEMENTED!!!");
    }
}
