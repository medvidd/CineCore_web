using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CineCoreBack.Models;

public partial class DbConfig : DbContext
{
    public DbConfig()
    {
    }

    public DbConfig(DbContextOptions<DbConfig> options)
        : base(options)
    {
    }

    public virtual DbSet<Actor> Actors { get; set; }

    public virtual DbSet<CallSheet> CallSheets { get; set; }

    public virtual DbSet<Casting> Castings { get; set; }

    public virtual DbSet<Location> Locations { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectMember> ProjectMembers { get; set; }

    public virtual DbSet<Prop> Props { get; set; }

    public virtual DbSet<Resource> Resources { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Scene> Scenes { get; set; }

    public virtual DbSet<SceneResource> SceneResources { get; set; }

    public virtual DbSet<SceneSchedule> SceneSchedules { get; set; }

    public virtual DbSet<ScriptElement> ScriptElements { get; set; }

    public virtual DbSet<ShootDay> ShootDays { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Genre> Genres { get; set; }

    public virtual DbSet<ProjectGenre> ProjectGenres { get; set; }

    public virtual DbSet<ProjectInvitation> ProjectInvitations { get; set; }

    //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //        => optionsBuilder.UseNpgsql("Host=localhost;Database=cine_core_dbfirst;Username=postgres;Password=anavaz357");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("enm_acquisition_type", new[] { "buy", "rent" })
            .HasPostgresEnum("enm_cast_status", new[] { "pending", "approved", "hold", "declined" })
            .HasPostgresEnum("enm_gender", new[] { "f", "m", "any" })
            .HasPostgresEnum("enm_location_type", new[] { "interior", "exterior", "studio" })
            .HasPostgresEnum("enm_member_status", new[] { "pending", "active", "declined" })
            .HasPostgresEnum("enm_prop_status", new[] { "available", "leased", "unavailable" })
            .HasPostgresEnum("enm_prop_type", new[] { "action", "scenography", "functional" })
            .HasPostgresEnum("enm_resource_type", new[] { "ROLE", "LOCATION", "PROP" })
            .HasPostgresEnum("enm_role_type", new[] { "lead", "supporting", "extra" })
            .HasPostgresEnum("enm_scene_status", new[] { "draft", "complete" })
            .HasPostgresEnum("enm_script_element", new[] { "action", "character", "dialogue", "parenthetical", "transition", "shot" })
            .HasPostgresEnum("enm_shoot_day_status", new[] { "draft", "published", "completed", "cancelled" })
            .HasPostgresEnum("enm_system_role", new[] { "owner", "manager", "actor" });

        modelBuilder.Entity<Actor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("actors_pkey");

            entity.ToTable("actors");
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");

            entity.Property(e => e.Characteristics)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("characteristics");

            entity.HasOne(d => d.User)
                .WithOne() 
                .HasForeignKey<Actor>(d => d.Id)
                .OnDelete(DeleteBehavior.Cascade) 
                .HasConstraintName("actors_user_id_fkey");
        });

        modelBuilder.Entity<CallSheet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("call_sheets_pkey");

            entity.ToTable("call_sheets");

            entity.HasIndex(e => new { e.ShootDayId, e.VersionNum }, "unq_shoot_day_version").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PublishedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("published_at");
            entity.Property(e => e.PublishedByUserId).HasColumnName("published_by_user_id");
            entity.Property(e => e.ShootDayId).HasColumnName("shoot_day_id");
            entity.Property(e => e.SnapshotData)
                .HasColumnType("jsonb")
                .HasColumnName("snapshot_data");
            entity.Property(e => e.VersionNum)
                .HasDefaultValue(1)
                .HasColumnName("version_num");

            entity.HasOne(d => d.PublishedByUser).WithMany(p => p.CallSheets)
                .HasForeignKey(d => d.PublishedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("call_sheets_published_by_user_id_fkey");

            entity.HasOne(d => d.ShootDay).WithMany(p => p.CallSheets)
                .HasForeignKey(d => d.ShootDayId)
                .HasConstraintName("call_sheets_shoot_day_id_fkey");
        });

        modelBuilder.Entity<Casting>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.ActorId }).HasName("casting_pkey");

            entity.ToTable("casting");

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.CastDate)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnName("cast_date");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CastStatus)
                .HasColumnName("cast_status")
                .HasDefaultValueSql("'pending'::enm_cast_status");

            entity.HasOne(d => d.Actor).WithMany(p => p.Castings)
                .HasForeignKey(d => d.ActorId)
                .HasConstraintName("casting_actor_id_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.Castings)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("casting_role_id_fkey");
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("locations_pkey");

            entity.ToTable("locations");

            entity.HasIndex(e => e.LocationName, "locations_location_name_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.ContactName)
                .HasMaxLength(200)
                .HasColumnName("contact_name");
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(30)
                .HasColumnName("contact_phone");
            entity.Property(e => e.LocationName)
                .HasMaxLength(255)
                .HasColumnName("location_name");
            entity.Property(e => e.Street)
                .HasMaxLength(255)
                .HasColumnName("street");
            entity.Property(e => e.LocationType)
                .HasColumnName("location_type");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Location)
                .HasForeignKey<Location>(d => d.Id)
                .HasConstraintName("locations_id_fkey");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("projects_pkey");

            entity.ToTable("projects");

            entity.HasIndex(e => e.Title, "projects_title_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Synopsis).HasColumnName("synopsis");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");

            entity.HasOne(d => d.Owner).WithMany(p => p.Projects)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("projects_owner_id_fkey");
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(e => new { e.ProjectId, e.InvitedEmail }).HasName("project_members_pkey");

            entity.ToTable("project_members");

            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.InvitedEmail)
                .HasMaxLength(255)
                .HasColumnName("invited_email");
            entity.Property(e => e.SysRole)
                .HasColumnName("sys_role")
                .HasMaxLength(50)
                .HasDefaultValue("manager");
            entity.Property(e => e.Department)
                .HasMaxLength(50)
                .HasColumnName("department");
            entity.Property(e => e.InvitedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("invited_at");
            entity.Property(e => e.InvitedByUserId).HasColumnName("invited_by_user_id");
            entity.Property(e => e.JobTitle)
                .HasMaxLength(100)
                .HasColumnName("job_title");
            entity.Property(e => e.JoinedAt).HasColumnName("joined_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.InvitedByUser).WithMany(p => p.ProjectMemberInvitedByUsers)
                .HasForeignKey(d => d.InvitedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("project_members_invited_by_user_id_fkey");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectMembers)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("project_members_project_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ProjectMemberUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("project_members_user_id_fkey");
        });

        modelBuilder.Entity<Prop>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("props_pkey");

            entity.ToTable("props");

            entity.HasIndex(e => e.PropName, "props_prop_name_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PropName)
                .HasMaxLength(150)
                .HasColumnName("prop_name");
            entity.Property(e => e.AcquisitionType)
                .HasColumnName("acquisition_type");
            entity.Property(e => e.PropStatus)
                .HasColumnName("prop_status");
            entity.Property(e => e.PropType)
                .HasColumnName("prop_type");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Prop)
                .HasForeignKey<Prop>(d => d.Id)
                .HasConstraintName("props_id_fkey");
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("resources_pkey");
            entity.ToTable("resources");
            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.ProjectId).HasColumnName("project_id");

            entity.HasOne(d => d.Project)
                .WithMany(p => p.Resources)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade) 
                .HasConstraintName("resources_project_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.Characteristics)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("characteristics");
            entity.Property(e => e.ColorHex)
                .HasMaxLength(7)
                .HasDefaultValueSql("'#333333'::character varying")
                .HasColumnName("color_hex");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(150)
                .HasColumnName("role_name");
            entity.Property(e => e.IsAutoGenerated)
                    .HasColumnName("is_auto_generated")
                    .HasDefaultValue(true);
            entity.Property(e => e.RoleType)
                .HasColumnName("role_type")
                .HasDefaultValueSql("'supporting'::enm_role_type");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Role)
                .HasForeignKey<Role>(d => d.Id)
                .HasConstraintName("roles_id_fkey");

            entity.HasOne(d => d.Project).WithMany(p => p.Roles)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("roles_project_id_fkey");
        });

        modelBuilder.Entity<Scene>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("scenes_pkey");

            entity.ToTable("scenes");

            entity.HasIndex(e => new { e.ProjectId, e.SequenceNum }, "uq_project_scene_num").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EstimatedDuration).HasColumnName("estimated_duration");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.SequenceNum).HasColumnName("sequence_num");
            entity.Property(e => e.SluglineText)
                .HasMaxLength(255)
                .HasColumnName("slugline_text");
            entity.Property(e => e.Notes)
                .HasColumnName("notes")
                .HasColumnType("text");

            entity.HasOne(d => d.Project).WithMany(p => p.Scenes)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("scenes_project_id_fkey");
        });

        modelBuilder.Entity<SceneResource>(entity =>
        {
            entity.HasKey(e => new { e.SceneId, e.ResourceId }).HasName("scene_resource_pkey");

            entity.ToTable("scene_resource");

            entity.Property(e => e.SceneId).HasColumnName("scene_id");
            entity.Property(e => e.ResourceId).HasColumnName("resource_id");
            entity.Property(e => e.Notes).HasColumnName("notes");

            entity.HasOne(d => d.Resource).WithMany(p => p.SceneResources)
                .HasForeignKey(d => d.ResourceId)
                .HasConstraintName("scene_resource_resource_id_fkey");

            entity.HasOne(d => d.Scene).WithMany(p => p.SceneResources)
                .HasForeignKey(d => d.SceneId)
                .HasConstraintName("scene_resource_scene_id_fkey");
            entity.HasOne(d => d.Scene).WithMany(p => p.SceneResources)
                .HasForeignKey(d => d.SceneId)
                .OnDelete(DeleteBehavior.Cascade) // Видаляємо зв'язки з ресурсами при видаленні сцени
                .HasConstraintName("scene_resource_scene_id_fkey");

        });

        modelBuilder.Entity<SceneSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("scene_schedule_pkey");

            entity.ToTable("scene_schedule");

            entity.HasIndex(e => new { e.ShootDayId, e.SceneId }, "unq_scene_in_day").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PrepTimeEstimate).HasColumnName("prep_time_estimate");
            entity.Property(e => e.SceneId).HasColumnName("scene_id");
            entity.Property(e => e.SceneOrder).HasColumnName("scene_order");
            entity.Property(e => e.ScheduledTime).HasColumnName("scheduled_time");
            entity.Property(e => e.ShootDayId).HasColumnName("shoot_day_id");
            entity.Property(e => e.ShootTimeEstimate).HasColumnName("shoot_time_estimate");

            entity.HasOne(d => d.Scene).WithMany(p => p.SceneSchedules)
                .HasForeignKey(d => d.SceneId)
                .HasConstraintName("scene_schedule_scene_id_fkey");

            entity.HasOne(d => d.ShootDay).WithMany(p => p.SceneSchedules)
                .HasForeignKey(d => d.ShootDayId)
                .HasConstraintName("scene_schedule_shoot_day_id_fkey");
            entity.HasOne(d => d.Scene).WithMany(p => p.SceneSchedules)
                .HasForeignKey(d => d.SceneId)
                .OnDelete(DeleteBehavior.Cascade) // Видаляємо записи з графіку при видаленні сцени
                .HasConstraintName("scene_schedule_scene_id_fkey");
        });

        modelBuilder.Entity<ScriptElement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("script_elements_pkey");

            entity.ToTable("script_elements");

            entity.HasIndex(e => new { e.SceneId, e.OrderIndex }, "uq_scene_element_order").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.OrderIndex).HasColumnName("order_index");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.SceneId).HasColumnName("scene_id");
            entity.Property(e => e.ElementType).HasColumnName("element_type");

            entity.HasOne(d => d.Role).WithMany(p => p.ScriptElements)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("script_elements_role_id_fkey");

            entity.HasOne(d => d.Scene).WithMany(p => p.ScriptElements)
                .HasForeignKey(d => d.SceneId)
                .HasConstraintName("script_elements_scene_id_fkey");
            entity.HasOne(d => d.Scene).WithMany(p => p.ScriptElements)
                .HasForeignKey(d => d.SceneId)
                .OnDelete(DeleteBehavior.Cascade) // Додаємо каскадне видалення елементів сценарію
                .HasConstraintName("script_elements_scene_id_fkey");
        });

        modelBuilder.Entity<ShootDay>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shoot_days_pkey");

            entity.ToTable("shoot_days");

            entity.HasIndex(e => new { e.ProjectId, e.UnitName, e.ShiftStart }, "unq_project_unit_shift").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BaseLocationId).HasColumnName("base_location_id");
            entity.Property(e => e.GeneralNotes).HasColumnName("general_notes");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ShiftEnd).HasColumnName("shift_end");
            entity.Property(e => e.ShiftStart).HasColumnName("shift_start");
            entity.Property(e => e.UnitName)
                .HasMaxLength(100)
                .HasDefaultValueSql("'Main Unit'::character varying")
                .HasColumnName("unit_name");

            entity.HasOne(d => d.BaseLocation).WithMany(p => p.ShootDays)
                .HasForeignKey(d => d.BaseLocationId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("shoot_days_base_location_id_fkey");

            entity.HasOne(d => d.Project).WithMany(p => p.ShootDays)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("shoot_days_project_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.PhoneNum)
                .HasMaxLength(30)
                .HasColumnName("phone_num");
            entity.Property(e => e.RegisteredAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("registered_at");
            entity.Property(e => e.Birthday)
                .HasColumnName("birthday");
            entity.Property(e => e.AvatarTheme)
                .HasMaxLength(50)
                .HasColumnName("avatar_theme")
                .HasDefaultValueSql("'theme-teal'::character varying");
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("genres_pkey");

            entity.ToTable("genres");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<ProjectGenre>(entity =>
        {
            entity.HasKey(e => new { e.ProjectId, e.GenreId }).HasName("project_genres_pkey");

            entity.ToTable("project_genres");

            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.GenreId).HasColumnName("genre_id");

            entity.HasOne(d => d.Genre).WithMany(p => p.ProjectGenres)
                .HasForeignKey(d => d.GenreId)
                .HasConstraintName("project_genres_genre_id_fkey")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectGenres)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("project_genres_project_id_fkey")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectInvitation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("project_invitations_pkey");
            entity.ToTable("project_invitations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100);
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100);
            entity.Property(e => e.SysRole).HasColumnName("sys_role").HasMaxLength(50);
            entity.Property(e => e.JobTitle).HasColumnName("job_title").HasMaxLength(100);
            entity.Property(e => e.Department).HasColumnName("department").HasMaxLength(100);
            entity.Property(e => e.Message).HasColumnName("message").HasColumnType("text");
            entity.Property(e => e.InvitedById).HasColumnName("invited_by_id");
            entity.Property(e => e.DateSent).HasColumnName("date_sent").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Token).HasColumnName("token");

            // Зв'язок з Проектом
            entity.HasOne(d => d.Project)
                .WithMany(p => p.ProjectInvitations)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade) // Якщо видаляють проект, видаляються і всі запрошення
                .HasConstraintName("project_invitations_project_id_fkey");

            // Зв'язок з Користувачем (тим, хто запросив)
            entity.HasOne(d => d.InvitedBy)
                .WithMany()
                .HasForeignKey(d => d.InvitedById)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("project_invitations_invited_by_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
