using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using assestment.Models;

namespace assestment.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Favorito> Favoritos { get; set; }

    public virtual DbSet<Inmueble> Inmuebles { get; set; }

    public virtual DbSet<KycVerification> KycVerifications { get; set; }

    public virtual DbSet<Reserva> Reservas { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("btree_gist")
            .HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Favorito>(entity =>
        {
            entity.HasKey(e => new { e.UsuarioId, e.InmuebleId }).HasName("favoritos_pkey");

            entity.ToTable("favoritos");

            entity.HasIndex(e => e.UsuarioId, "idx_favoritos_usuario");

            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.InmuebleId).HasColumnName("inmueble_id");
            entity.Property(e => e.FechaAgregado)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_agregado");

            entity.HasOne(d => d.Inmueble).WithMany(p => p.Favoritos)
                .HasForeignKey(d => d.InmuebleId)
                .HasConstraintName("fk_favorito_inmueble");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Favoritos)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("fk_favorito_usuario");
        });

        modelBuilder.Entity<Inmueble>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("inmuebles_pkey");

            entity.ToTable("inmuebles");

            entity.HasIndex(e => e.PropietarioId, "idx_inmuebles_propietario");

            entity.HasIndex(e => e.Ubicacion, "idx_inmuebles_ubicacion");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.Latitud)
                .HasPrecision(9, 6)
                .HasColumnName("latitud");
            entity.Property(e => e.Longitud)
                .HasPrecision(9, 6)
                .HasColumnName("longitud");
            entity.Property(e => e.PropietarioId).HasColumnName("propietario_id");
            entity.Property(e => e.TarifaPorNoche)
                .HasPrecision(12, 2)
                .HasColumnName("tarifa_por_noche");
            entity.Property(e => e.Titulo)
                .HasMaxLength(200)
                .HasColumnName("titulo");
            entity.Property(e => e.Ubicacion)
                .HasMaxLength(255)
                .HasColumnName("ubicacion");
            entity.Property(e => e.UrlImagen)
                .HasMaxLength(500)
                .HasColumnName("url_imagen");

            entity.HasOne(d => d.Propietario).WithMany(p => p.Inmuebles)
                .HasForeignKey(d => d.PropietarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_inmueble_propietario");
        });

        modelBuilder.Entity<KycVerification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("kyc_verifications_pkey");

            entity.ToTable("kyc_verifications");

            entity.HasIndex(e => e.Estado, "idx_kyc_estado");

            entity.HasIndex(e => e.UsuarioId, "idx_kyc_usuario_id");

            entity.HasIndex(e => e.UsuarioId, "kyc_verifications_usuario_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.ApellidosExtraidos)
                .HasMaxLength(150)
                .HasColumnName("apellidos_extraidos");
            entity.Property(e => e.ConfianzaOcr)
                .HasPrecision(4, 3)
                .HasColumnName("confianza_ocr");
            entity.Property(e => e.DocumentoCoincide).HasColumnName("documento_coincide");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Pendiente'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FechaNacimientoCoincide).HasColumnName("fecha_nacimiento_coincide");
            entity.Property(e => e.FechaNacimientoExtraida).HasColumnName("fecha_nacimiento_extraida");
            entity.Property(e => e.FechaVerificacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_verificacion");
            entity.Property(e => e.NombreCoincide).HasColumnName("nombre_coincide");
            entity.Property(e => e.NombresExtraidos)
                .HasMaxLength(150)
                .HasColumnName("nombres_extraidos");
            entity.Property(e => e.NumeroDocumentoExtraido)
                .HasMaxLength(50)
                .HasColumnName("numero_documento_extraido");
            entity.Property(e => e.Razones)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("razones");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Usuario).WithOne(p => p.KycVerification)
                .HasForeignKey<KycVerification>(d => d.UsuarioId)
                .HasConstraintName("fk_kyc_usuario");
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("reservas_pkey");

            entity.ToTable("reservas");

            entity.HasIndex(e => e.InmuebleId, "idx_reservas_inmueble");

            entity.HasIndex(e => new { e.InmuebleId, e.FechaInicio, e.FechaFin }, "idx_reservas_rango_fechas");

            entity.HasIndex(e => e.UsuarioId, "idx_reservas_usuario");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Pendiente'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.HoraCheckin)
                .HasDefaultValueSql("'14:00:00'::time without time zone")
                .HasColumnName("hora_checkin");
            entity.Property(e => e.HoraCheckout)
                .HasDefaultValueSql("'12:00:00'::time without time zone")
                .HasColumnName("hora_checkout");
            entity.Property(e => e.InmuebleId).HasColumnName("inmueble_id");
            entity.Property(e => e.PrecioTotal)
                .HasPrecision(12, 2)
                .HasColumnName("precio_total");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Inmueble).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.InmuebleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_reserva_inmueble");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_reserva_usuario");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("usuarios_pkey");

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.Email, "idx_usuarios_email");

            entity.HasIndex(e => e.NumeroDocumento, "idx_usuarios_numero_documento");

            entity.HasIndex(e => e.NumeroDocumento, "uq_usuarios_numero_documento").IsUnique();

            entity.HasIndex(e => e.Email, "usuarios_email_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Apellidos)
                .HasMaxLength(150)
                .HasColumnName("apellidos");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FechaNacimiento).HasColumnName("fecha_nacimiento");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.KycAprobado).HasColumnName("kyc_aprobado");
            entity.Property(e => e.Nombres)
                .HasMaxLength(150)
                .HasColumnName("nombres");
            entity.Property(e => e.NumeroDocumento)
                .HasMaxLength(50)
                .HasColumnName("numero_documento");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Rol)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Huesped'::character varying")
                .HasColumnName("rol");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
