using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Strive.Core.Services.ConferenceControl.Gateways;
using Strive.Core.Services.ConferenceControl.Notifications;
using Strive.Core.Services.ConferenceControl.Requests;
using Strive.Core.Services.ConferenceControl.UseCases;
using Strive.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Strive.Core.Tests.Services.ConferenceControl.UseCases
{
    public class CloseConferenceHandlerTests
    {
        private readonly Mock<IMediator> _mediator;
        private readonly Mock<IOpenConferenceRepository> _openConferenceRepository;
        private readonly ILogger<CloseConferenceHandler> _logger;

        public CloseConferenceHandlerTests(ITestOutputHelper output)
        {
            _logger = output.CreateLogger<CloseConferenceHandler>();

            _mediator = new Mock<IMediator>();
            _openConferenceRepository = new Mock<IOpenConferenceRepository>();
        }

        private CloseConferenceHandler CreateHandler()
        {
            return new(_openConferenceRepository.Object, _mediator.Object, _logger);
        }

        private void SetConferenceIsOpen(string conferenceId)
        {
            _openConferenceRepository.Setup(x => x.Delete(conferenceId)).ReturnsAsync(true);
        }

        private void SetConferenceIsClosed(string conferenceId)
        {
            _openConferenceRepository.Setup(x => x.Delete(conferenceId)).ReturnsAsync(false);
        }

        [Fact]
        public async Task Handle_ConferenceIsClosed_DontThrowExceptionAndDontPublishNotification()
        {
            const string conferenceId = "test";

            // arrange
            SetConferenceIsClosed(conferenceId);
            var handler = CreateHandler();

            // act
            await handler.Handle(new CloseConferenceRequest(conferenceId), CancellationToken.None);

            // assert
            _mediator.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_ConferenceIsOpen_CloseAndPublishNotification()
        {
            const string conferenceId = "test";

            // arrange
            SetConferenceIsOpen(conferenceId);
            var handler = CreateHandler();

            var capturedNotification = _mediator.CaptureNotification<ConferenceClosedNotification>();

            // act
            await handler.Handle(new CloseConferenceRequest(conferenceId), CancellationToken.None);

            // assert
            var notification = capturedNotification.GetNotification();
            Assert.Equal(conferenceId, notification.ConferenceId);
        }
    }
}
