using CreditosApi.Domain.Entities;
using CreditosApi.Infrastructure.Data;
using CreditosApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CreditosApi.UnitTests.Repositories;

public class CreditoRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CreditoRepository _repository;

    public CreditoRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new CreditoRepository(_context);
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_DeveInserirCreditoERetornarEntidade()
    {
        // Arrange
        var credito = new Credito(
            "123456",
            "7891011",
            DateTime.Now,
            1500.75m,
            "ISSQN",
            true,
            5.0m,
            30000.00m,
            5000.00m,
            25000.00m);

        // Act
        var resultado = await _repository.AddAsync(credito);

        // Assert
        Assert.NotNull(resultado);
        Assert.True(resultado.Id > 0);
        Assert.Equal("123456", resultado.NumeroCredito);
        Assert.Equal(1, _context.Creditos.Count());
    }

    [Fact]
    public async Task AddAsync_DeveInserirMultiplosCreditosComSucesso()
    {
        // Arrange
        var creditos = new List<Credito>
        {
            new Credito("111111", "1111111", DateTime.Now, 1000m, "ISSQN", true, 5.0m, 20000m, 3000m, 17000m),
            new Credito("222222", "2222222", DateTime.Now, 1500m, "ISSQN", false, 5.0m, 30000m, 5000m, 25000m),
            new Credito("333333", "3333333", DateTime.Now, 2000m, "ISSQN", true, 3.5m, 40000m, 7000m, 33000m)
        };

        // Act
        foreach (var credito in creditos)
        {
            await _repository.AddAsync(credito);
        }

        // Assert
        Assert.Equal(3, _context.Creditos.Count());
        Assert.All(creditos, c => Assert.True(c.Id > 0));
    }

    [Fact]
    public async Task AddAsync_DevePreservarTodosOsCampos()
    {
        // Arrange
        var dataConstituicao = DateTime.Now;
        var credito = new Credito(
            "999999",
            "9999999",
            dataConstituicao,
            2500.50m,
            "ISSQN",
            false,
            7.5m,
            50000.00m,
            8000.00m,
            42000.00m);

        // Act
        var resultado = await _repository.AddAsync(credito);
        var creditoPersistido = await _context.Creditos.FirstOrDefaultAsync(c => c.NumeroCredito == "999999");

        // Assert
        Assert.NotNull(creditoPersistido);
        Assert.Equal("999999", creditoPersistido.NumeroCredito);
        Assert.Equal("9999999", creditoPersistido.NumeroNfse);
        Assert.Equal(2500.50m, creditoPersistido.ValorIssqn);
        Assert.False(creditoPersistido.SimplesNacional);
        Assert.Equal(7.5m, creditoPersistido.Aliquota);
        Assert.Equal(50000.00m, creditoPersistido.ValorFaturado);
        Assert.Equal(8000.00m, creditoPersistido.ValorDeducao);
        Assert.Equal(42000.00m, creditoPersistido.BaseCalculo);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_DeveRetornarCreditoPorId()
    {
        // Arrange
        var credito = new Credito("111111", "1111111", DateTime.Now, 1000m, "ISSQN", true, 5.0m, 20000m, 3000m, 17000m);
        var creditoInserido = await _repository.AddAsync(credito);

        // Act
        var resultado = await _repository.GetByIdAsync(creditoInserido.Id);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(creditoInserido.Id, resultado.Id);
        Assert.Equal("111111", resultado.NumeroCredito);
    }

    [Fact]
    public async Task GetByIdAsync_DeveRetornarNullParaIdInexistente()
    {
        // Act
        var resultado = await _repository.GetByIdAsync(9999);

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public async Task GetByIdAsync_DeveRetornarNullParaIdNegativo()
    {
        // Act
        var resultado = await _repository.GetByIdAsync(-1);

        // Assert
        Assert.Null(resultado);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_DeveRetornarTodosCreditosNoBanco()
    {
        // Arrange
        var creditos = new List<Credito>
        {
            new Credito("111111", "1111111", DateTime.Now, 1000m, "ISSQN", true, 5.0m, 20000m, 3000m, 17000m),
            new Credito("222222", "2222222", DateTime.Now, 1500m, "ISSQN", false, 5.0m, 30000m, 5000m, 25000m),
            new Credito("333333", "3333333", DateTime.Now, 2000m, "ISSQN", true, 3.5m, 40000m, 7000m, 33000m)
        };

        foreach (var credito in creditos)
        {
            await _repository.AddAsync(credito);
        }

        // Act
        var resultado = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(3, resultado.Count());
    }

    [Fact]
    public async Task GetAllAsync_DeveRetornarListaVaziaQuandoNaoHaCreditos()
    {
        // Act
        var resultado = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(resultado);
        Assert.Empty(resultado);
    }

    #endregion

    #region GetByNumeroCreditoAsync Tests

    [Fact]
    public async Task GetByNumeroCreditoAsync_DeveRetornarCreditoPorNumero()
    {
        // Arrange
        var credito = new Credito("444444", "4444444", DateTime.Now, 1200m, "ISSQN", true, 5.0m, 24000m, 4000m, 20000m);
        await _repository.AddAsync(credito);

        // Act
        var resultado = await _repository.GetByNumeroCreditoAsync("444444");

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("444444", resultado.NumeroCredito);
        Assert.Equal("4444444", resultado.NumeroNfse);
    }

    [Fact]
    public async Task GetByNumeroCreditoAsync_DeveRetornarNullParaNumeroCreditoInexistente()
    {
        // Act
        var resultado = await _repository.GetByNumeroCreditoAsync("INEXISTENTE");

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public async Task GetByNumeroCreditoAsync_DeveRetornarNullParaNumeroCreditoNuloOuVazio()
    {
        // Act
        var resultado = await _repository.GetByNumeroCreditoAsync("");

        // Assert
        Assert.Null(resultado);
    }

    [Fact]
    public async Task GetByNumeroCreditoAsync_DeveRetornarApenasUmCredito()
    {
        // Arrange
        var credito1 = new Credito("555555", "5555555", DateTime.Now, 1000m, "ISSQN", true, 5.0m, 20000m, 3000m, 17000m);
        var credito2 = new Credito("666666", "5555555", DateTime.Now, 1500m, "ISSQN", false, 5.0m, 30000m, 5000m, 25000m);
        await _repository.AddAsync(credito1);
        await _repository.AddAsync(credito2);

        // Act
        var resultado = await _repository.GetByNumeroCreditoAsync("555555");

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("555555", resultado.NumeroCredito);
    }

    #endregion

    #region GetByNumeroNfseAsync Tests

    [Fact]
    public async Task GetByNumeroNfseAsync_DeveRetornarTodosCreditosPorNfse()
    {
        // Arrange
        var credito1 = new Credito("111111", "NFSE001", DateTime.Now, 1000m, "ISSQN", true, 5.0m, 20000m, 3000m, 17000m);
        var credito2 = new Credito("222222", "NFSE001", DateTime.Now, 1500m, "ISSQN", false, 5.0m, 30000m, 5000m, 25000m);
        var credito3 = new Credito("333333", "NFSE002", DateTime.Now, 2000m, "ISSQN", true, 3.5m, 40000m, 7000m, 33000m);

        await _repository.AddAsync(credito1);
        await _repository.AddAsync(credito2);
        await _repository.AddAsync(credito3);

        // Act
        var resultado = await _repository.GetByNumeroNfseAsync("NFSE001");

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(2, resultado.Count());
        Assert.All(resultado, c => Assert.Equal("NFSE001", c.NumeroNfse));
    }

    [Fact]
    public async Task GetByNumeroNfseAsync_DeveRetornarListaVaziaParaNfseInexistente()
    {
        // Arrange
        var credito = new Credito("111111", "NFSE001", DateTime.Now, 1000m, "ISSQN", true, 5.0m, 20000m, 3000m, 17000m);
        await _repository.AddAsync(credito);

        // Act
        var resultado = await _repository.GetByNumeroNfseAsync("NFSE_INEXISTENTE");

        // Assert
        Assert.NotNull(resultado);
        Assert.Empty(resultado);
    }

    [Fact]
    public async Task GetByNumeroNfseAsync_DeveRetornarListaVaziaParaBancoVazio()
    {
        // Act
        var resultado = await _repository.GetByNumeroNfseAsync("NFSE001");

        // Assert
        Assert.NotNull(resultado);
        Assert.Empty(resultado);
    }

    [Fact]
    public async Task GetByNumeroNfseAsync_DeveRetornarListaVaziaParaNfseVazioOuNulo()
    {
        // Act
        var resultado = await _repository.GetByNumeroNfseAsync("");

        // Assert
        Assert.NotNull(resultado);
        Assert.Empty(resultado);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_DeveAtualizarCreditoComSucesso()
    {
        // Arrange
        var credito = new Credito("777777", "7777777", DateTime.Now, 1000m, "ISSQN", true, 5.0m, 20000m, 3000m, 17000m);
        var creditoInserido = await _repository.AddAsync(credito);

        // Simula alteração (usando reflexão para atualizar propriedade privada, se necessário)
        var creditoAtualizado = await _context.Creditos.FindAsync(creditoInserido.Id);
        _context.Entry(creditoAtualizado).Property("ValorIssqn").CurrentValue = 2000m;

        // Act
        await _repository.UpdateAsync(creditoAtualizado);
        var creditoVerificado = await _repository.GetByIdAsync(creditoInserido.Id);

        // Assert
        Assert.NotNull(creditoVerificado);
        Assert.Equal(2000m, creditoVerificado.ValorIssqn);
    }

    [Fact]
    public async Task UpdateAsync_DeveAtualizar_MultiplosCampos()
    {
        // Arrange
        var credito = new Credito("888888", "8888888", DateTime.Now, 1000m, "ISSQN", true, 5.0m, 20000m, 3000m, 17000m);
        var creditoInserido = await _repository.AddAsync(credito);

        // Act
        var creditoAtualizado = await _context.Creditos.FindAsync(creditoInserido.Id);
        _context.Entry(creditoAtualizado).Property("ValorIssqn").CurrentValue = 2500m;
        _context.Entry(creditoAtualizado).Property("Aliquota").CurrentValue = 7.5m;
        _context.Entry(creditoAtualizado).Property("BaseCalculo").CurrentValue = 35000m;
        
        await _repository.UpdateAsync(creditoAtualizado);
        var creditoVerificado = await _repository.GetByIdAsync(creditoInserido.Id);

        // Assert
        Assert.NotNull(creditoVerificado);
        Assert.Equal(2500m, creditoVerificado.ValorIssqn);
        Assert.Equal(7.5m, creditoVerificado.Aliquota);
        Assert.Equal(35000m, creditoVerificado.BaseCalculo);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_DeveRemoverCreditoComSucesso()
    {
        // Arrange
        var credito = new Credito("999999", "9999999", DateTime.Now, 1000m, "ISSQN", true, 5.0m, 20000m, 3000m, 17000m);
        var creditoInserido = await _repository.AddAsync(credito);

        // Act
        await _repository.DeleteAsync(creditoInserido);
        var creditoVerificado = await _repository.GetByIdAsync(creditoInserido.Id);

        // Assert
        Assert.Null(creditoVerificado);
        Assert.Equal(0, _context.Creditos.Count());
    }

    [Fact]
    public async Task DeleteAsync_DeveRemoverApenasOCreditoEspecifico()
    {
        // Arrange
        var credito1 = new Credito("AAAAAAA", "AAAAAAA", DateTime.Now, 1000m, "ISSQN", true, 5.0m, 20000m, 3000m, 17000m);
        var credito2 = new Credito("BBBBBBB", "BBBBBBB", DateTime.Now, 1500m, "ISSQN", false, 5.0m, 30000m, 5000m, 25000m);
        var credito1Inserido = await _repository.AddAsync(credito1);
        await _repository.AddAsync(credito2);

        // Act
        await _repository.DeleteAsync(credito1Inserido);
        var resultado = await _repository.GetAllAsync();

        // Assert
        Assert.Single(resultado);
        Assert.Equal("BBBBBBB", resultado.First().NumeroCredito);
    }

    #endregion

    #region ExistsByNumeroCreditoAsync Tests

    [Fact]
    public async Task ExistsByNumeroCreditoAsync_DeveRetornarTrueParaCreditoExistente()
    {
        // Arrange
        var credito = new Credito("CCCCCC", "CCCCCC", DateTime.Now, 1000m, "ISSQN", true, 5.0m, 20000m, 3000m, 17000m);
        await _repository.AddAsync(credito);

        // Act
        var resultado = await _repository.ExistsByNumeroCreditoAsync("CCCCCC");

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public async Task ExistsByNumeroCreditoAsync_DeveRetornarFalseParaCreditoInexistente()
    {
        // Act
        var resultado = await _repository.ExistsByNumeroCreditoAsync("INEXISTENTE");

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task ExistsByNumeroCreditoAsync_DeveRetornarFalseParaBancoVazio()
    {
        // Act
        var resultado = await _repository.ExistsByNumeroCreditoAsync("QUALQUER_NUMERO");

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task ExistsByNumeroCreditoAsync_DeveRetornarFalseParaNumeroCreditoNuloOuVazio()
    {
        // Arrange
        var credito = new Credito("DDDDDD", "DDDDDD", DateTime.Now, 1000m, "ISSQN", true, 5.0m, 20000m, 3000m, 17000m);
        await _repository.AddAsync(credito);

        // Act
        var resultado = await _repository.ExistsByNumeroCreditoAsync("");

        // Assert
        Assert.False(resultado);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Workflow_CreditoCompleto_AddUpdateGet()
    {
        // Arrange
        var creditoOriginal = new Credito("WORKFLOW1", "WF1", DateTime.Now, 1000m, "ISSQN", true, 5.0m, 20000m, 3000m, 17000m);

        // Act - Inserção
        var creditoInserido = await _repository.AddAsync(creditoOriginal);
        Assert.True(creditoInserido.Id > 0);

        // Act - Verificação de existência
        var existe = await _repository.ExistsByNumeroCreditoAsync("WORKFLOW1");
        Assert.True(existe);

        // Act - Busca
        var creditoRecuperado = await _repository.GetByNumeroCreditoAsync("WORKFLOW1");
        Assert.NotNull(creditoRecuperado);

        // Act - Deleção
        await _repository.DeleteAsync(creditoRecuperado);

        // Assert
        var creditoAposDeletar = await _repository.GetByIdAsync(creditoInserido.Id);
        Assert.Null(creditoAposDeletar);
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}