namespace Microsoft.eShopOnContainers.Services.Catalog.API.IntegrationEvents;

//ToDo M: implement integration events and delete this dummy class
public record IntegrationEvent();

public interface ICatalogIntegrationEventService
{
    Task SaveEventAndCatalogContextChangesAsync(IntegrationEvent evt);
    Task PublishThroughEventBusAsync(IntegrationEvent evt);
}
