using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AutoCurriculum.Models;

public partial class AutoCurriculumDbContext : DbContext
{
    public AutoCurriculumDbContext()
    {
    }

    public AutoCurriculumDbContext(DbContextOptions<AutoCurriculumDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Chapter> Chapters { get; set; }

    public virtual DbSet<Content> Contents { get; set; }

    public virtual DbSet<CurriculumHistory> CurriculumHistories { get; set; }

    public virtual DbSet<Lesson> Lessons { get; set; }

    public virtual DbSet<Source> Sources { get; set; }

    public virtual DbSet<Topic> Topics { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-E36PGI1\\SQLEXPRESS;Database=AutoCurriculumDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(e => e.ChapterId).HasName("PK__Chapters__0893A36ABFBCA2D9");

            entity.Property(e => e.ChapterTitle).HasMaxLength(255);

            entity.HasOne(d => d.Topic).WithMany(p => p.Chapters)
                .HasForeignKey(d => d.TopicId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chapters_Topics");
        });

        modelBuilder.Entity<Content>(entity =>
        {
            entity.HasKey(e => e.ContentId).HasName("PK__Contents__2907A81E0A3B4FE5");

            entity.HasOne(d => d.Lesson).WithMany(p => p.Contents)
                .HasForeignKey(d => d.LessonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Contents_Lessons");
        });

        modelBuilder.Entity<CurriculumHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__Curricul__4D7B4ABD8D5921A2");

            entity.ToTable("CurriculumHistory");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CurriculumHistories)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_History_Users");

            entity.HasOne(d => d.Topic).WithMany(p => p.CurriculumHistories)
                .HasForeignKey(d => d.TopicId)
                .HasConstraintName("FK_History_Topics");
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.LessonId).HasName("PK__Lessons__B084ACD0D36A0E80");

            entity.Property(e => e.LessonTitle).HasMaxLength(255);

            entity.HasOne(d => d.Chapter).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.ChapterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lessons_Chapters");
        });

        modelBuilder.Entity<Source>(entity =>
        {
            entity.HasKey(e => e.SourceId).HasName("PK__Sources__16E019192182FF0F");

            entity.Property(e => e.RetrievedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SourceName).HasMaxLength(100);
            entity.Property(e => e.SourceUrl).HasMaxLength(255);
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.HasKey(e => e.TopicId).HasName("PK__Topics__022E0F5DCDDE2EE6");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TopicName).HasMaxLength(255);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Topics)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Topics_Users");

            entity.HasOne(d => d.Source).WithMany(p => p.Topics)
                .HasForeignKey(d => d.SourceId)
                .HasConstraintName("FK_Topics_Sources");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C3E1B4E0E");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E464C1147E").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
