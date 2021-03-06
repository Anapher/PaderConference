using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Options;
using Strive.Core.Domain.Entities;
using Strive.Core.IntegrationTests.Services.Base;
using Strive.Core.Interfaces.Gateways.Repositories;
using Strive.Core.Services;
using Strive.Core.Services.ConferenceControl;
using Strive.Core.Services.ConferenceControl.Notifications;
using Strive.Core.Services.ConferenceControl.Requests;
using Strive.Core.Services.Rooms;
using Strive.Core.Services.Rooms.Notifications;
using Strive.Core.Services.Rooms.Requests;
using Strive.Core.Services.Synchronization;
using Xunit;
using Xunit.Abstractions;

namespace Strive.Core.IntegrationTests.Services
{
    public class RoomTests : ServiceIntegrationTest
    {
        private const string ConferenceId = "123";
        private const string ConnectionId = "connId";

        private readonly Participant _testParticipant = new(ConferenceId, "af");
        private readonly SynchronizedObjectId _synchronizedRoomsId = SynchronizedRooms.SyncObjId;

        public RoomTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected override IEnumerable<Type> FetchServiceTypes()
        {
            return FetchTypesOfNamespace(typeof(Room)).Concat(FetchTypesForSynchronizedObjects())
                .Concat(FetchTypesOfNamespace(typeof(SynchronizedConferenceInfo)));
        }

        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            base.ConfigureContainer(builder);

            builder.RegisterInstance(new OptionsWrapper<RoomOptions>(new RoomOptions())).AsImplementedInterfaces();
            AddConferenceRepo(builder, new Conference(ConferenceId));
            SetupConferenceControl(builder);
        }

        private SynchronizedRooms GetSyncRoomsForTestParticipant()
        {
            return SynchronizedObjectListener.GetSynchronizedObject<SynchronizedRooms>(_testParticipant,
                _synchronizedRoomsId);
        }

        private JoinConferenceRequest CreateTestParticipantJoinRequest()
        {
            return new(_testParticipant, ConnectionId, new ParticipantMetadata("Vincent"));
        }

        private void AssertSyncObjParticipantIsInRoom(string roomId)
        {
            var syncObj = GetSyncRoomsForTestParticipant();
            var mappedParticipant = Assert.Single(syncObj.Participants);
            Assert.Equal(_testParticipant.Id, mappedParticipant.Key);
            Assert.Equal(roomId, mappedParticipant.Value);
        }

        [Fact]
        public async Task CreateRoomsRequest_ConferenceNotOpen_ConcurrencyException()
        {
            // act
            var room = new RoomCreationInfo("Test");
            await Assert.ThrowsAsync<ConcurrencyException>(async () =>
            {
                await Mediator.Send(new CreateRoomsRequest(ConferenceId, new[] {room}));
            });

            // assert
            Assert.Empty(Data.Data);
            NotificationCollector.AssertNoMoreNotifications();
        }

        [Fact]
        public async Task OpenConference_ConferenceIsOpen_PublishRoomCreatedNotification()
        {
            // act
            await Mediator.Send(new OpenConferenceRequest(ConferenceId));

            // assert
            NotificationCollector.AssertSingleNotificationIssued<RoomsCreatedNotification>();
        }

        [Fact]
        public async Task CreateRoomsRequest_ConferenceIsOpen_PublishRoomCreatedNotification()
        {
            const string roomDisplayName = "Test";

            // arrange
            await Mediator.Send(new OpenConferenceRequest(ConferenceId));

            NotificationCollector.Reset();

            // act
            var room = new RoomCreationInfo(roomDisplayName);
            await Mediator.Send(new CreateRoomsRequest(ConferenceId, new[] {room}));

            // assert
            NotificationCollector.AssertSingleNotificationIssued<RoomsCreatedNotification>();
        }

        [Fact]
        public async Task CreateRoomsRequest_ConferenceIsOpen_UpdateSynchronizedObject()
        {
            const string roomDisplayName = "Test";

            // arrange
            await Mediator.Send(new OpenConferenceRequest(ConferenceId));
            await Mediator.Send(CreateTestParticipantJoinRequest());

            // act
            var room = new RoomCreationInfo(roomDisplayName);
            await Mediator.Send(new CreateRoomsRequest(ConferenceId, new[] {room}));

            // assert
            var syncObj = GetSyncRoomsForTestParticipant();
            Assert.Contains(syncObj.Rooms, x => x.DisplayName == roomDisplayName);
        }

        [Fact]
        public async Task ParticipantJoined_ConferenceNotOpen_DoNothing()
        {
            // act
            await Mediator.Send(CreateTestParticipantJoinRequest());

            // assert
            NotificationCollector.AssertNoNotificationOfType<ParticipantsRoomChangedNotification>();

            var syncObj = GetSyncRoomsForTestParticipant();
            Assert.Empty(syncObj.Participants);
        }

        [Fact]
        public async Task ParticipantJoined_ConferenceOpen_JoinParticipantToDefaultRoom()
        {
            // arrange
            await Mediator.Send(new OpenConferenceRequest(ConferenceId));

            // act
            await Mediator.Send(CreateTestParticipantJoinRequest());

            // assert
            AssertSyncObjParticipantIsInRoom(RoomOptions.DEFAULT_ROOM_ID);

            NotificationCollector.AssertSingleNotificationIssued<ParticipantsRoomChangedNotification>(notification =>
            {
                Assert.Equal(ConferenceId, notification.ConferenceId);
                Assert.Single(notification.Participants,
                    new KeyValuePair<Participant, ParticipantRoomChangeInfo>(_testParticipant,
                        ParticipantRoomChangeInfo.Joined(RoomOptions.DEFAULT_ROOM_ID)));
            });
        }

        [Fact]
        public async Task ParticipantJoined_SwitchRoom_UpdateSynchronizedObjectAndPublishNotification()
        {
            // arrange
            await Mediator.Send(new OpenConferenceRequest(ConferenceId));
            await Mediator.Send(CreateTestParticipantJoinRequest());
            var createdRooms =
                await Mediator.Send(new CreateRoomsRequest(ConferenceId, new[] {new RoomCreationInfo("test")}));
            var createdRoom = Assert.Single(createdRooms);

            // act
            await Mediator.Send(SetParticipantRoomRequest.MoveParticipant(_testParticipant, createdRoom.RoomId));

            // assert
            AssertSyncObjParticipantIsInRoom(createdRoom.RoomId);

            NotificationCollector.AssertLastNotificationIssued<ParticipantsRoomChangedNotification>(notification =>
            {
                Assert.Equal(ConferenceId, notification.ConferenceId);
                Assert.Single(notification.Participants,
                    new KeyValuePair<Participant, ParticipantRoomChangeInfo>(_testParticipant,
                        ParticipantRoomChangeInfo.Switched(RoomOptions.DEFAULT_ROOM_ID, createdRoom.RoomId)));
            });
        }

        [Fact]
        public async Task CloseConference_ParticipantInDefaultRoom_RemoveParticipantFromRoom()
        {
            // arrange
            await Mediator.Send(new OpenConferenceRequest(ConferenceId));
            await Mediator.Send(CreateTestParticipantJoinRequest());

            NotificationCollector.Reset();

            // act
            await Mediator.Send(new CloseConferenceRequest(ConferenceId));

            // assert
            var syncObj = GetSyncRoomsForTestParticipant();
            Assert.Empty(syncObj.Participants);

            NotificationCollector.AssertSingleNotificationIssued<ParticipantsRoomChangedNotification>(notification =>
            {
                Assert.Equal(ConferenceId, notification.ConferenceId);
                Assert.Single(notification.Participants,
                    new KeyValuePair<Participant, ParticipantRoomChangeInfo>(_testParticipant,
                        ParticipantRoomChangeInfo.Left(RoomOptions.DEFAULT_ROOM_ID)));
            });
        }

        [Fact]
        public async Task OpenConference_ParticipantAlreadyJoined_MoveParticipantToDefaultRoom()
        {
            // arrange
            await Mediator.Send(CreateTestParticipantJoinRequest());

            // act
            await Mediator.Send(new OpenConferenceRequest(ConferenceId));

            // assert
            AssertSyncObjParticipantIsInRoom(RoomOptions.DEFAULT_ROOM_ID);

            NotificationCollector.AssertSingleNotificationIssued<ParticipantsRoomChangedNotification>(notification =>
            {
                Assert.Equal(ConferenceId, notification.ConferenceId);
                Assert.Single(notification.Participants,
                    new KeyValuePair<Participant, ParticipantRoomChangeInfo>(_testParticipant,
                        ParticipantRoomChangeInfo.Joined(RoomOptions.DEFAULT_ROOM_ID)));
            });
        }

        [Fact]
        public async Task ParticipantLeft_ParticipantWasInRoom_RemoveParticipantFromRoom()
        {
            // arrange
            await Mediator.Send(new OpenConferenceRequest(ConferenceId));
            await Mediator.Send(CreateTestParticipantJoinRequest());

            NotificationCollector.Reset();

            // act
            await Mediator.Publish(new ParticipantLeftNotification(_testParticipant, ConnectionId));

            // assert
            var syncObj = GetSyncRoomsForTestParticipant();
            Assert.Empty(syncObj.Participants);

            NotificationCollector.AssertSingleNotificationIssued<ParticipantsRoomChangedNotification>(notification =>
            {
                Assert.Equal(ConferenceId, notification.ConferenceId);
                Assert.Single(notification.Participants,
                    new KeyValuePair<Participant, ParticipantRoomChangeInfo>(_testParticipant,
                        ParticipantRoomChangeInfo.Left(RoomOptions.DEFAULT_ROOM_ID)));
            });
        }
    }
}
