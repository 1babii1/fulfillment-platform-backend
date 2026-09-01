using FulfillmentPlatform.Catalog.Domain;
using FulfillmentPlatform.SharedKernel;

namespace Catalog.Domain.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Create_WithName_TrimsAndCreatesProduct()
    {
        Result<Product> result = Product.Create("  Linen shirt  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Linen shirt", result.Value.Name);
    }

    [Fact]
    public void Create_WithoutName_ReturnsValidationFailure()
    {
        Result<Product> result = Product.Create(" ");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
    }

    [Fact]
    public void AddVariant_WithNewSku_AddsVariant()
    {
        Product product = Product.Create("Linen shirt").Value;

        Result<ProductVariant> result = product.AddVariant("shirt-s-blue", "Small / Blue");

        Assert.True(result.IsSuccess);
        Assert.Single(product.Variants);
        Assert.Equal(product.Id, result.Value.ProductId);
    }

    [Fact]
    public void AddVariant_WithDuplicateSkuIgnoringCase_ReturnsConflict()
    {
        Product product = Product.Create("Linen shirt").Value;
        _ = product.AddVariant("shirt-s-blue", "Small / Blue");

        Result<ProductVariant> result = product.AddVariant("SHIRT-S-BLUE", "Small / Blue");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error?.Type);
    }
}
