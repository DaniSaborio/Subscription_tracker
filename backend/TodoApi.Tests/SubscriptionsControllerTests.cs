using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TodoApi.Controllers;
using TodoApi.Data;
using TodoApi.Models;
using Xunit;

namespace TodoApi.Tests;

public class SubscriptionsControllerTests
{
    [Fact]
    public async Task Share_ReturnsNoContent_WhenSuccessful()
    {
        var repo = new Mock<ISubscriptionRepository>();
        var users = new Mock<IUserRepository>();

        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var subId = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(ownerId, subId)).ReturnsAsync(new SubscriptionItem { Id = subId, UserId = ownerId, IsOwner = true });
        users.Setup(u => u.GetByEmailAsync("target@x.test")).ReturnsAsync(new UserAccount { Id = targetId, Email = "target@x.test" });
        repo.Setup(r => r.ShareAsync(ownerId, subId, targetId, "target@x.test")).ReturnsAsync(true);

        var controller = new SubscriptionsController(repo.Object, users.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, ownerId.ToString()) })) } };

        var result = await controller.Share(subId, new ShareSubscriptionRequest { Email = "target@x.test" });

        // We expect NoContent (204) when share succeeds
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Share_ReturnsNotFound_WhenTargetUserMissing()
    {
        var repo = new Mock<ISubscriptionRepository>();
        var users = new Mock<IUserRepository>();

        var ownerId = Guid.NewGuid();
        var subId = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(ownerId, subId)).ReturnsAsync(new SubscriptionItem { Id = subId, UserId = ownerId, IsOwner = true });
        users.Setup(u => u.GetByEmailAsync("noone@x.test")).ReturnsAsync((UserAccount?)null);

        var controller = new SubscriptionsController(repo.Object, users.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, ownerId.ToString()) })) } };

        var result = await controller.Share(subId, new ShareSubscriptionRequest { Email = "noone@x.test" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetShares_ReturnsForbidden_WhenNotOwner()
    {
        var repo = new Mock<ISubscriptionRepository>();
        var users = new Mock<IUserRepository>();

        var userId = Guid.NewGuid();
        var subId = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(userId, subId)).ReturnsAsync(new SubscriptionItem { Id = subId, UserId = userId, IsOwner = false });

        var controller = new SubscriptionsController(repo.Object, users.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) })) } };

        var result = await controller.GetShares(subId);

        Assert.IsType<ObjectResult>(result.Result);
        var obj = (ObjectResult)result.Result;
        Assert.Equal(403, obj.StatusCode);
    }

    [Fact]
    public async Task RevokeShare_ReturnsNoContent_WhenSuccessful()
    {
        var repo = new Mock<ISubscriptionRepository>();
        var users = new Mock<IUserRepository>();

        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var subId = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(ownerId, subId)).ReturnsAsync(new SubscriptionItem { Id = subId, UserId = ownerId, IsOwner = true });
        users.Setup(u => u.GetByEmailAsync("target@x.test")).ReturnsAsync(new UserAccount { Id = targetId, Email = "target@x.test" });
        repo.Setup(r => r.RevokeShareAsync(ownerId, subId, targetId)).ReturnsAsync(true);

        var controller = new SubscriptionsController(repo.Object, users.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, ownerId.ToString()) })) } };

        var result = await controller.RevokeShare(subId, "target@x.test");

        Assert.IsType<NoContentResult>(result);
    }
}
