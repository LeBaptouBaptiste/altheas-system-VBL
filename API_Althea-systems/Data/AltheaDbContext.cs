using Microsoft.EntityFrameworkCore;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Models.Products;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Invoices;
using API_Althea_systems.Models.Messaging;
using API_Althea_systems.Models.Content;
using API_Althea_systems.Models.Analytics;

namespace API_Althea_systems.Data;

public class AltheaDbContext : DbContext
{
    public AltheaDbContext(DbContextOptions<AltheaDbContext> options) : base(options)
    {
    }

    // Users
    public DbSet<User> Users => Set<User>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<UserPaymentMethod> UserPaymentMethods => Set<UserPaymentMethod>();
    public DbSet<UserRecoveryCode> UserRecoveryCodes => Set<UserRecoveryCode>();

    // Products
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductSpec> ProductSpecs => Set<ProductSpec>();

    // Orders
    public DbSet<Models.Order.Order> Orders => Set<Models.Order.Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusChange> OrderStatusChanges => Set<OrderStatusChange>();

    // Invoices
    public DbSet<Invoice> Invoices => Set<Invoice>();

    // Messaging
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    // Content
    public DbSet<HeroSlide> HeroSlides => Set<HeroSlide>();
    public DbSet<StaticPage> StaticPages => Set<StaticPage>();

    // Analytics
    public DbSet<SalesAnalytics> SalesAnalytics => Set<SalesAnalytics>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AltheaDbContext).Assembly);
    }
}
