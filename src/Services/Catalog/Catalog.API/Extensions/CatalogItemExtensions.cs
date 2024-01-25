using Catalog.API.Model;

namespace Catalog.API.Extensions;

public static class CatalogItemExtensions
{
    public static void FillProductUrl(this CatalogItem? item, string picBaseUrl, bool azureStorageEnabled)
    {
        if (item is not null)
        {
            item.PictureUri = azureStorageEnabled
                ? picBaseUrl + item.PictureFileName
                : picBaseUrl.Replace("[0]", item.Id.ToString());
        }
    }
}
