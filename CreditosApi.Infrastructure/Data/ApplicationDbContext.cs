using CreditosApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditosApi.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Credito> Creditos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Credito>(entity =>
        {
            entity.ToTable("credito");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .UseIdentityColumn();

            entity.Property(e => e.NumeroCredito)
                .HasColumnName("numero_credito")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.NumeroNfse)
                .HasColumnName("numero_nfse")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.DataConstituicao)
                .HasColumnName("data_constituicao")
                .IsRequired();

            entity.Property(e => e.ValorIssqn)
                .HasColumnName("valor_issqn")
                .HasColumnType("decimal(15,2)")
                .IsRequired();

            entity.Property(e => e.TipoCredito)
                .HasColumnName("tipo_credito")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.SimplesNacional)
                .HasColumnName("simples_nacional")
                .IsRequired();

            entity.Property(e => e.Aliquota)
                .HasColumnName("aliquota")
                .HasColumnType("decimal(5,2)")
                .IsRequired();

            entity.Property(e => e.ValorFaturado)
                .HasColumnName("valor_faturado")
                .HasColumnType("decimal(15,2)")
                .IsRequired();

            entity.Property(e => e.ValorDeducao)
                .HasColumnName("valor_deducao")
                .HasColumnType("decimal(15,2)")
                .IsRequired();

            entity.Property(e => e.BaseCalculo)
                .HasColumnName("base_calculo")
                .HasColumnType("decimal(15,2)")
                .IsRequired();
        });
    }
}