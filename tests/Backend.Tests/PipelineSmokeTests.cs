using Backend.Api.Services;

namespace Backend.Tests;

/// <summary>
/// Prueba mínima para verificar que el proyecto de pruebas compila,
/// referencia al backend y se ejecuta en el pipeline de CI.
/// Las pruebas V-01..V-03 y C-01..C-03 van en sus propios archivos.
/// </summary>
public class PipelineSmokeTests
{
    [Fact]
    public void PasswordService_VerifiesTheHashItGenerated()
    {
        var service = new PasswordService();
        var salt = service.GenerateSalt();
        var hash = service.HashPassword("Charlie123!", salt);

        Assert.True(service.VerifyPassword("Charlie123!", salt, hash));
        Assert.False(service.VerifyPassword("otra-clave", salt, hash));
    }
}
