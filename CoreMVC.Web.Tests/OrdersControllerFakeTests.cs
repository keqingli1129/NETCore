using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoreMVC.Web.Controllers;
using CoreMVC.Infrastructure.Data;
using CoreMVC.Domain.Entities;
using Xunit;

namespace CoreMVC.Web.Tests;

public class OrdersControllerFakeTests
{
    private static DbSet<T> CreateFakeDbSet<T>(IEnumerable<T> data) where T : class
    {
        // Backing list so tests can mutate the set if needed
        var backingList = data?.ToList() ?? new List<T>();

        var fakeSet = A.Fake<DbSet<T>>(o => o.Implements(typeof(IQueryable<T>)).Implements(typeof(IAsyncEnumerable<T>)));

        // Provide IQueryable/IAsyncEnumerable behavior backed by the mutable list
        A.CallTo(() => ((IQueryable<T>)fakeSet).Provider).ReturnsLazily(() => new TestAsyncQueryProvider<T>(backingList.AsQueryable().Provider));
        A.CallTo(() => ((IQueryable<T>)fakeSet).Expression).ReturnsLazily(() => backingList.AsQueryable().Expression);
        A.CallTo(() => ((IQueryable<T>)fakeSet).ElementType).ReturnsLazily(() => backingList.AsQueryable().ElementType);
        A.CallTo(() => ((IQueryable<T>)fakeSet).GetEnumerator()).ReturnsLazily(() => backingList.GetEnumerator());
        A.CallTo(() => ((IAsyncEnumerable<T>)fakeSet).GetAsyncEnumerator(A<System.Threading.CancellationToken>._)).ReturnsLazily(() => new TestAsyncEnumerator<T>(backingList.GetEnumerator()));

        // Support FindAsync by attempting to match the first key to a common PK property (e.g. Id, {Type}Id)
        var pkProp = typeof(T).GetProperties().FirstOrDefault(p => string.Equals(p.Name, typeof(T).Name + "Id", StringComparison.OrdinalIgnoreCase))
                     ?? typeof(T).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

        A.CallTo(() => fakeSet.FindAsync(A<object[]>._)).ReturnsLazily((object[] keys) =>
        {
            if (keys is null || keys.Length == 0 || pkProp is null)
            {
                return new ValueTask<T?>((T?)null!);
            }

            var key = keys[0];
            T? found = backingList.FirstOrDefault(e =>
            {
                var val = pkProp.GetValue(e);
                if (val is null) return false;
                try
                {
                    var converted = Convert.ChangeType(key, val.GetType());
                    return Equals(val, converted);
                }
                catch
                {
                    return Equals(val, key);
                }
            });

            return new ValueTask<T?>(found);
        });

        // Allow Add/Remove to mutate the backing list so tests can exercise controller behavior that modifies sets
        A.CallTo(() => fakeSet.Add(A<T>._)).Invokes((T entity) => backingList.Add(entity));
        A.CallTo(() => fakeSet.Remove(A<T>._)).Invokes((T entity) => backingList.Remove(entity));

        return fakeSet;
    }

    [Fact]
    public async Task Index_ReturnsViewWithOrdersOrderedByDateDesc_UsingFakeContext()
    {
        // Arrange
        var order1 = new Order { OrderId = 1, OrderDate = new DateTime(2023, 1, 1), ShipCity = "A" };
        var order2 = new Order { OrderId = 2, OrderDate = new DateTime(2024, 1, 1), ShipCity = "B" };
        var orders = new[] { order1, order2 };

        var ordersSet = CreateFakeDbSet(orders);

        var context = A.Fake<ApplicationDbContext>();
        A.CallTo(() => context.Orders).Returns(ordersSet);

        var controller = new OrdersController(context);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<IEnumerable<Order>>().Subject;
        model.Select(o => o.OrderId).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Details_WithValidId_ReturnsViewWithOrder_UsingFakeContext()
    {
        // Arrange
        var order = new Order
        {
            OrderId = 42,
            OrderDate = new DateTime(2024, 4, 2),
            ShipCity = "City",
            Customer = new Customer { CustomerId = "CUST42", CompanyName = "Acme" },
            Employee = new Employee { EmployeeId = 1, FirstName = "John", LastName = "Doe" },
            ShipViaNavigation = new Shipper { ShipperId = 1, CompanyName = "FastShip" }
        };

        var orders = new[] { order };
        var ordersSet = CreateFakeDbSet(orders);

        var context = A.Fake<ApplicationDbContext>();
        A.CallTo(() => context.Orders).Returns(ordersSet);

        var controller = new OrdersController(context);

        // Act
        var result = await controller.Details(order.OrderId);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<Order>().Subject;
        model.OrderId.Should().Be(order.OrderId);
        model.Customer.CompanyName.Should().Be("Acme");
        model.Employee.LastName.Should().Be("Doe");
        model.ShipViaNavigation.CompanyName.Should().Be("FastShip");
    }
}
