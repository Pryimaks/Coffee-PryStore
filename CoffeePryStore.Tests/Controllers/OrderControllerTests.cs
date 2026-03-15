using Coffee_PryStore.Controllers;
using Coffee_PryStore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CoffeePryStore.Tests.Controllers
{
    public class OrderControllerTests
    {
        private readonly DataBaseHome _context;
        private readonly OrderController _controller;
        private readonly DefaultHttpContext _httpContext;

        public OrderControllerTests()
        {
            var options = new DbContextOptionsBuilder<DataBaseHome>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new DataBaseHome(options);

            _httpContext = new DefaultHttpContext();
            _httpContext.Session = new FakeSession();

            _controller = new OrderController(_context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _httpContext
                }
            };

            var tempDataProviderMock = new Mock<ITempDataProvider>();
            _controller.TempData = new TempDataDictionary(_httpContext, tempDataProviderMock.Object);
        }

        [Fact]
        public async Task Order_RedirectsToPersonRegistration_IfUserNotLoggedIn()
        {
            var result = await _controller.Order(new OrderViewModel());

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("PersonRegistration", redirectResult.ActionName);
            Assert.Equal("PersonRegistration", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Order_DoesNotCreateOrder_WhenBasketIsEmpty()
        {
            var userId = 1;
            _httpContext.Session.SetInt32("UserId", userId);

            await _context.SaveChangesAsync();

            var result = await _controller.Order(new OrderViewModel
            {
                FullName = "John Doe",
                City = "Kyiv",
                Address = "Street 1",
                PhoneNumber = "1234567890"
            });

            var order = _context.Orders.FirstOrDefault();

            Assert.Null(order);
        }

        [Fact]
        public async Task Order_ReturnsView_IfModelStateIsInvalid()
        {
            _httpContext.Session.SetInt32("UserId", 1);

            _controller.ModelState.AddModelError("FullName", "Required");

            var result = await _controller.Order(new OrderViewModel());

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<OrderViewModel>(viewResult.Model);
        }

        [Fact]
        public async Task Order_CreatesOrder_WhenValidDataProvided()
        {
            var userId = 1;
            _httpContext.Session.SetInt32("UserId", userId);

            var coffee = new Table
            {
                CofId = 1,
                CofName = "Latte",
                CofPrice = 50,
                CofAmount = 10
            };

            _context.Table.Add(coffee);

            _context.Basket.Add(new Basket
            {
                Id = userId,
                CofId = coffee.CofId,
                Quantity = 2,
                Cof = coffee
            });

            await _context.SaveChangesAsync();

            var result = await _controller.Order(new OrderViewModel
            {
                FullName = "John Doe",
                City = "Kyiv",
                Address = "123 Main St",
                PhoneNumber = "1234567890"
            });

            var order = _context.Orders.FirstOrDefault();

            Assert.NotNull(order);
            Assert.Equal("John Doe", order.FullName);
            Assert.Equal(100, order.TotalAmount);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("OrderSuccess", redirectResult.ActionName);
        }

        [Fact]
        public async Task Order_CalculatesTotalAmount_Correctly()
        {
            var userId = 1;
            _httpContext.Session.SetInt32("UserId", userId);

            var coffee = new Table
            {
                CofId = 2,
                CofName = "Espresso",
                CofPrice = 40,
                CofAmount = 10
            };

            _context.Table.Add(coffee);

            _context.Basket.Add(new Basket
            {
                Id = userId,
                CofId = coffee.CofId,
                Quantity = 3,
                Cof = coffee
            });

            await _context.SaveChangesAsync();

            await _controller.Order(new OrderViewModel
            {
                FullName = "John Doe",
                City = "Kyiv",
                Address = "Street 1",
                PhoneNumber = "1234567890"
            });

            var order = _context.Orders.FirstOrDefault();

            Assert.NotNull(order);
            Assert.Equal(120, order.TotalAmount);
        }

        public class FakeSession : ISession
        {
            private readonly Dictionary<string, byte[]> _sessionStorage = new Dictionary<string, byte[]>();

            public IEnumerable<string> Keys => _sessionStorage.Keys;

            public string Id => "FakeSessionId";

            public bool IsAvailable => true;

            public void Clear() => _sessionStorage.Clear();

            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public void Remove(string key) => _sessionStorage.Remove(key);

            public void Set(string key, byte[] value) => _sessionStorage[key] = value;

            public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value);
        }
    }
}