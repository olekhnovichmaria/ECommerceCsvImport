using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;
using Xunit;

namespace ECommerceCsvImport.Tests;

public class RepositoryTests
{
    [Fact]
    public void Add_AddsProduct_GetAllContainsIt()
    {
        var repository = new InMemoryProductRepository();
        var product = new Product { Sku = "1001", Name = "Ноутбук Lenovo", Price = 45990m, Brand = "Lenovo" };

        repository.Add(product);
        var all = repository.GetAll();

        Assert.Single(all);
        Assert.Equal("1001", all[0].Sku);
    }

    [Fact]
    public void GetBySku_FindsProduct_BySku()
    {
        var repository = new InMemoryProductRepository();
        var product = new Product { Sku = "1001", Name = "Ноутбук Lenovo", Price = 45990m };
        repository.Add(product);

        var found = repository.GetBySku("1001");

        Assert.NotNull(found);
        Assert.Equal("Ноутбук Lenovo", found.Name);
    }

    [Fact]
    public void GetBySku_FindsProduct_CaseInsensitiveAndTrim()
    {
        var repository = new InMemoryProductRepository();
        var product = new Product { Sku = "1001", Name = "Ноутбук", Price = 100m };
        repository.Add(product);

        var found = repository.GetBySku(" 1001 ");

        Assert.NotNull(found);
    }

    [Fact]
    public void EmptyRepository_ReturnsZeroCount()
    {
        var repository = new InMemoryProductRepository();

        Assert.Equal(0, repository.Count);
        Assert.Empty(repository.GetAll());
        Assert.Null(repository.GetBySku("1001"));
    }
}