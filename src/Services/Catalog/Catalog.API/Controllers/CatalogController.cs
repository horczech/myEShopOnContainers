﻿using System.Net;
using Catalog.API.Extensions;
using Catalog.API.Infrastructure;
using Catalog.API.IntegrationEvents.Events;
using Catalog.API.Model;
using Catalog.API.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.eShopOnContainers.Services.Catalog.API.IntegrationEvents;
using Microsoft.Extensions.Options;

namespace Catalog.API.Controllers;

[Route("api/v1/catalog")]
[ApiController]
public class CatalogController : ControllerBase
{
    private readonly CatalogContext _catalogContext;
    private readonly CatalogSettings _settings;
    private readonly ICatalogIntegrationEventService _catalogIntegrationEventService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(CatalogContext context, IOptionsSnapshot<CatalogSettings> settings, ICatalogIntegrationEventService catalogIntegrationEventService, IWebHostEnvironment webHostEnvironment, ILogger<CatalogController> logger)
    {
        _catalogContext = context ?? throw new ArgumentNullException(nameof(context));
        _catalogIntegrationEventService = catalogIntegrationEventService ?? throw new ArgumentNullException(nameof(catalogIntegrationEventService));
        _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        _logger = logger;
        _settings = settings.Value;

        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    [HttpGet]
    [Route("seed")]
    public async Task<IActionResult> SeedDatabase()
    {
        var seeder = new CatalogContextSeed();
        await seeder.SeedAsync(_catalogContext, _webHostEnvironment, _logger);

        return Ok();
    }

    [HttpGet]
    [Route("items")]
    [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(IEnumerable<CatalogItem>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> ItemsAsync([FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0, string? ids = null)
    {
        if (!string.IsNullOrEmpty(ids))
        {
            var items = await GetItemsByIdsAsync(ids);

            if (!items.Any())
            {
                return BadRequest("ids value invalid. Must be comma-separated list of numbers");
            }

            return Ok(items);
        }

        var totalItems = await _catalogContext.CatalogItems
            .LongCountAsync();

        var itemsOnPage = await _catalogContext.CatalogItems
            .OrderBy(c => c.Name)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        itemsOnPage = ChangeUriPlaceholder(itemsOnPage);

        var model = new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);

        return Ok(model);
    }

    private async Task<List<CatalogItem>> GetItemsByIdsAsync(string ids)
    {
        var numIds = ids
            .Split(',')
            .Select(id => (IsValid: int.TryParse(id, out var x), Id: x))
            .ToList();

        if (!numIds.All(nid => nid.IsValid))
        {
            return [];
        }

        var idsToSelect = numIds
            .Select(id => id.Id);

        var catalogItems = await _catalogContext.CatalogItems.Where(ci => idsToSelect.Contains(ci.Id)).ToListAsync();
        catalogItems = ChangeUriPlaceholder(catalogItems);

        return catalogItems;
    }

    [HttpGet]
    [Route("items/{id:int}")]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(CatalogItem), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<CatalogItem>> ItemByIdAsync(int id)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        var item = await _catalogContext.CatalogItems.SingleOrDefaultAsync(ci => ci.Id == id);

        var baseUri = _settings.PicBaseUrl;
        var azureStorageEnabled = _settings.AzureStorageEnabled;

        item.FillProductUrl(baseUri, azureStorageEnabled: azureStorageEnabled);

        if (item is not null)
        {
            return item;
        }

        return NotFound();
    }

    [HttpGet]
    [Route("items/withname/{name:minlength(1)}")]
    [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<PaginatedItemsViewModel<CatalogItem>>> ItemsWithNameAsync(string name, [FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0)
    {
        var totalItems = await _catalogContext.CatalogItems
            .Where(c => c.Name.StartsWith(name))
            .LongCountAsync();

        var itemsOnPage = await _catalogContext.CatalogItems
            .Where(c => c.Name.StartsWith(name))
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        itemsOnPage = ChangeUriPlaceholder(itemsOnPage);

        return new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);
    }

    [HttpGet]
    [Route("items/type/{catalogTypeId}/brand/{catalogBrandId:int?}")]
    [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<PaginatedItemsViewModel<CatalogItem>>> ItemsByTypeIdAndBrandIdAsync(int catalogTypeId, int? catalogBrandId, [FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0)
    {
        var root = (IQueryable<CatalogItem>)_catalogContext.CatalogItems;

        root = root.Where(ci => ci.CatalogTypeId == catalogTypeId);

        if (catalogBrandId.HasValue)
        {
            root = root.Where(ci => ci.CatalogBrandId == catalogBrandId);
        }

        var totalItems = await root
            .LongCountAsync();

        var itemsOnPage = await root
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        itemsOnPage = ChangeUriPlaceholder(itemsOnPage);

        return new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);
    }

    [HttpGet]
    [Route("items/type/all/brand/{catalogBrandId:int?}")]
    [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<PaginatedItemsViewModel<CatalogItem>>> ItemsByBrandIdAsync(int? catalogBrandId, [FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0)
    {
        var root = (IQueryable<CatalogItem>)_catalogContext.CatalogItems;

        if (catalogBrandId.HasValue)
        {
            root = root.Where(ci => ci.CatalogBrandId == catalogBrandId);
        }

        var totalItems = await root
            .LongCountAsync();

        var itemsOnPage = await root
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        itemsOnPage = ChangeUriPlaceholder(itemsOnPage);

        return new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);
    }

    [HttpGet]
    [Route("catalogtypes")]
    [ProducesResponseType(typeof(List<CatalogType>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<List<CatalogType>>> CatalogTypesAsync()
    {
        return await _catalogContext.CatalogTypes.ToListAsync();
    }

    [HttpGet]
    [Route("catalogbrands")]
    [ProducesResponseType(typeof(List<CatalogBrand>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<List<CatalogBrand>>> CatalogBrandsAsync()
    {
        return await _catalogContext.CatalogBrands.ToListAsync();
    }

    [Route("items")]
    [HttpPut]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    public async Task<ActionResult> UpdateProductAsync([FromBody] CatalogItem productToUpdate)
    {
        var catalogItem = await _catalogContext.CatalogItems.SingleOrDefaultAsync(i => i.Id == productToUpdate.Id);

        if (catalogItem == null)
        {
            return NotFound(new { Message = $"Item with id {productToUpdate.Id} not found." });
        }

        var oldPrice = catalogItem.Price;
        var raiseProductPriceChangedEvent = oldPrice != productToUpdate.Price;

        // Update current product
        catalogItem = productToUpdate;
        _catalogContext.CatalogItems.Update(catalogItem);

        if (raiseProductPriceChangedEvent) // Save product's data and publish integration event through the Event Bus if price has changed
        {
            //Create Integration Event to be published through the Event Bus
            var priceChangedEvent = new ProductPriceChangedIntegrationEvent(catalogItem.Id, productToUpdate.Price, oldPrice);

            // Achieving atomicity between original Catalog database operation and the IntegrationEventLog thanks to a local transaction
            await _catalogIntegrationEventService.SaveEventAndCatalogContextChangesAsync(priceChangedEvent);

            // Publish through the Event Bus and mark the saved event as published
            await _catalogIntegrationEventService.PublishThroughEventBusAsync(priceChangedEvent);
        }
        else // Just save the updated product because the Product's Price hasn't changed.
        {
            await _catalogContext.SaveChangesAsync();
        }

        const string actionName = nameof(ItemByIdAsync);
        return CreatedAtAction(actionName, new { id = productToUpdate.Id }, null);
    }

    [Route("items")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    public async Task<ActionResult> CreateProductAsync([FromBody] CatalogItem product)
    {
        var item = new CatalogItem
        {
            CatalogBrandId = product.CatalogBrandId,
            CatalogTypeId = product.CatalogTypeId,
            Description = product.Description,
            Name = product.Name,
            PictureFileName = product.PictureFileName,
            Price = product.Price
        };

        _catalogContext.CatalogItems.Add(item);

        await _catalogContext.SaveChangesAsync();

        const string actionName = nameof(ItemByIdAsync);
        return CreatedAtAction(actionName, new { id = item.Id }, null);
    }

    [Route("{id}")]
    [HttpDelete]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> DeleteProductAsync(int id)
    {
        var product = _catalogContext.CatalogItems.SingleOrDefault(x => x.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        _catalogContext.CatalogItems.Remove(product);

        await _catalogContext.SaveChangesAsync();

        return NoContent();
    }

    private List<CatalogItem> ChangeUriPlaceholder(List<CatalogItem> catalogItems)
    {
        var baseUri = _settings.PicBaseUrl;
        var azureStorageEnabled = _settings.AzureStorageEnabled;

        foreach (var item in catalogItems)
        {
            item.FillProductUrl(baseUri, azureStorageEnabled: azureStorageEnabled);
        }

        return catalogItems;
    }
}
