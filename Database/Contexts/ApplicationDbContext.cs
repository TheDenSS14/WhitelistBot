using Microsoft.EntityFrameworkCore;
using WhitelistBot.Database.Entities;

namespace WhitelistBot.Database.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users { get; set; } = null!;
}