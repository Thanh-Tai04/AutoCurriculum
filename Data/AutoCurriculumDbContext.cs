using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // DÒNG NÀY ĐỂ KÍCH HOẠT IDENTITY
using AutoCurriculum.Models;

namespace AutoCurriculum.Models;

// 1. KẾ THỪA TỪ IdentityDbContext THAY VÌ DbContext
public partial class AutoCurriculumDbContext : IdentityDbContext<ApplicationUser>
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
    public virtual DbSet<Section> Sections { get; set; }
    public virtual DbSet<Source> Sources { get; set; }
    public virtual DbSet<Topic> Topics { get; set; }
    public DbSet<SystemLog> SystemLogs { get; set; }
    
    
    // ĐÃ XÓA: Bảng Users cũ. Từ nay Identity sẽ lo phần này.

    // ĐÃ XÓA: Hàm OnConfiguring chứa chuỗi kết nối và sinh lỗi Warning

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 2. DÒNG PHÉP THUẬT: Bắt buộc phải có để đẻ ra các bảng AspNetUsers, AspNetRoles...
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(e => e.ChapterId).HasName("PK__Chapters__0893A36A5338C271");
            entity.HasIndex(e => new { e.TopicId, e.ChapterOrder }, "IX_Chapters_Order");
            entity.HasIndex(e => e.TopicId, "IX_Chapters_TopicId");
            entity.Property(e => e.ChapterTitle).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.HasOne(d => d.Topic).WithMany(p => p.Chapters)
                .HasForeignKey(d => d.TopicId)
                .HasConstraintName("FK_Chapters_Topics");
        });

        modelBuilder.Entity<Content>(entity =>
        {
            entity.HasKey(e => e.ContentId).HasName("PK__Contents__2907A81EDF4EBC2B");
            entity.HasIndex(e => e.LessonId, "IX_Contents_LessonId");
            entity.Property(e => e.ContentOrder).HasDefaultValue(1);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.HasOne(d => d.Lesson).WithMany(p => p.Contents)
                .HasForeignKey(d => d.LessonId)
                .HasConstraintName("FK_Contents_Lessons");
        });

        modelBuilder.Entity<CurriculumHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__Curricul__4D7B4ABDB44AF841");
            entity.ToTable("CurriculumHistory");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);

            // Đã xóa cấu hình khóa ngoại tới bảng Users cũ để tránh lỗi đụng độ
            entity.HasOne(d => d.Topic).WithMany(p => p.CurriculumHistories)
                .HasForeignKey(d => d.TopicId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_History_Topics");
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.LessonId).HasName("PK__Lessons__B084ACD0B1413EB8");
            entity.HasIndex(e => e.ChapterId, "IX_Lessons_ChapterId");
            entity.HasIndex(e => e.LessonOrder, "IX_Lessons_Order");
            entity.HasIndex(e => e.SectionId, "IX_Lessons_SectionId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LessonTitle).HasMaxLength(255);
            entity.HasOne(d => d.Chapter).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.ChapterId)
                .HasConstraintName("FK_Lessons_Chapters");
            entity.HasOne(d => d.Section).WithMany(p => p.Lessons)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Lessons_Sections");
        });

        modelBuilder.Entity<Section>(entity =>
        {
            entity.HasKey(e => e.SectionId).HasName("PK__Sections__80EF0872724A1D50");
            entity.HasIndex(e => e.ChapterId, "IX_Sections_ChapterId");
            entity.HasIndex(e => new { e.ChapterId, e.SectionOrder }, "IX_Sections_Order");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SectionTitle).HasMaxLength(255);
            entity.HasOne(d => d.Chapter).WithMany(p => p.Sections)
                .HasForeignKey(d => d.ChapterId)
                .HasConstraintName("FK_Sections_Chapters");
        });

        modelBuilder.Entity<Source>(entity =>
        {
            entity.HasKey(e => e.SourceId).HasName("PK__Sources__16E01919CA632BF3");
            entity.Property(e => e.RetrievedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SourceName).HasMaxLength(100);
            entity.Property(e => e.SourceUrl).HasMaxLength(255);
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.HasKey(e => e.TopicId).HasName("PK__Topics__022E0F5DE2DC64EC");
            entity.HasIndex(e => e.CreatedAt, "IX_Topics_CreatedAt").IsDescending();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TopicName).HasMaxLength(255);

            // Đã xóa cấu hình khóa ngoại tới bảng Users cũ để tránh lỗi đụng độ
            entity.HasOne(d => d.Source).WithMany(p => p.Topics)
                .HasForeignKey(d => d.SourceId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Topics_Sources");
        });

        // ĐÃ XÓA: modelBuilder.Entity<User>(...)

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}