using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Documents;

namespace HouseOs.Tests.Features.Documents;

public class CategorieParDefautTests
{
    [Theory]
    [InlineData("application/pdf", CategorieDocument.Manuel)]
    [InlineData("image/jpeg", CategorieDocument.Photo)]
    [InlineData("image/png", CategorieDocument.Photo)]
    [InlineData("image/webp", CategorieDocument.Photo)]
    [InlineData("image/heic", CategorieDocument.Photo)]
    [InlineData("application/octet-stream", CategorieDocument.Autre)]
    public void Deduit_la_categorie_du_type_mime(string typeMime, CategorieDocument attendue)
    {
        Assert.Equal(attendue, DocumentsEndpoints.CategorieParDefaut(typeMime));
    }
}
