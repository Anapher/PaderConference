using System.Threading.Tasks;
using Autofac;
using Moq;
using Strive.Core.Services.ConferenceControl.Gateways;
using Strive.Hubs.Core.Services;
using Strive.Hubs.Core.Services.Middlewares;
using Xunit;

namespace Strive.Tests.Hubs.Core.Services.Middlewares
{
    public class ServiceInvokerConferenceMiddlewareTests : MiddlewareTestBase
    {
        protected override IServiceRequestBuilder<string> Execute(IServiceRequestBuilder<string> builder)
        {
            return builder.ConferenceMustBeOpen();
        }

        [Fact]
        public async Task ConferenceMustBeOpen_NotOpen_Failed()
        {
            // arrange
            var repo = new Mock<IOpenConferenceRepository>();
            repo.Setup(x => x.IsOpen(ConferenceId)).ReturnsAsync(false);

            var context = CreateContext(builder => builder.RegisterInstance(repo.Object).AsImplementedInterfaces());

            // act
            var result = await ServiceInvokerConferenceMiddleware.ValidateConferenceIsOpen(context);

            // assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task ConferenceMustBeOpen_IsOpen_Succeed()
        {
            // arrange
            var repo = new Mock<IOpenConferenceRepository>();
            repo.Setup(x => x.IsOpen(ConferenceId)).ReturnsAsync(true);

            var context = CreateContext(builder => builder.RegisterInstance(repo.Object).AsImplementedInterfaces());

            // act
            var result = await ServiceInvokerConferenceMiddleware.ValidateConferenceIsOpen(context);

            // assert
            Assert.True(result.Success);
        }
    }
}
