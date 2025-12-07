using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain;

namespace OrderService.Infrastructure;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasConversion(
                id => id.Value,
                value => new OrderId(value))
            .ValueGeneratedNever();

        builder.Property(o => o.UserId)
            .HasConversion(
                id => id.Value,
                value => new UserId(value))
            .IsRequired();

        builder.OwnsMany(o => o.OrderItems, navigationBuilder =>
        {
            navigationBuilder.ToTable("OrderItems");

            navigationBuilder.WithOwner().HasForeignKey("OrderId");

            navigationBuilder.HasKey("Id");

            navigationBuilder.Property<int>("Id")
                .ValueGeneratedOnAdd();

            navigationBuilder.Property(i => i.ProductId)
                .HasConversion(
                    id => id.Value,
                    value => new ProductId(value))
                .IsRequired();

            navigationBuilder.Property(i => i.Quantity)
                .IsRequired();
        });

        builder.Navigation(o => o.OrderItems).AutoInclude(false);
    }
}