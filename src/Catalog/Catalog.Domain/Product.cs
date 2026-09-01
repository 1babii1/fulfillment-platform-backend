using FulfillmentPlatform.SharedKernel;

namespace FulfillmentPlatform.Catalog.Domain;

public sealed class Product
{
    private readonly List<ProductVariant> _variants = [];

    private Product(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; }

    public string Name { get; }

    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    public static Result<Product> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Product>(CatalogErrors.Required("product.name"));
        }

        return Result.Success(new Product(Guid.CreateVersion7(), name.Trim()));
    }

    public Result<ProductVariant> AddVariant(string sku, string name)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return Result.Failure<ProductVariant>(CatalogErrors.Required("variant.sku"));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<ProductVariant>(CatalogErrors.Required("variant.name"));
        }

        string normalizedSku = sku.Trim();
        if (_variants.Any(variant => string.Equals(variant.Sku, normalizedSku, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<ProductVariant>(CatalogErrors.DuplicateSku(normalizedSku));
        }

        ProductVariant variant = new(Guid.CreateVersion7(), Id, normalizedSku, name.Trim());
        _variants.Add(variant);

        return Result.Success(variant);
    }
}
