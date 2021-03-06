using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Options;
using Strive.Core.Domain.Entities;
using Strive.Core.IntegrationTests.Services.Base;
using Strive.Core.Services;
using Strive.Core.Services.Chat;
using Strive.Core.Services.Chat.Channels;
using Strive.Core.Services.Chat.Notifications;
using Strive.Core.Services.Chat.Requests;
using Strive.Core.Services.ConferenceControl;
using Strive.Core.Services.ConferenceControl.Notifications;
using Strive.Core.Services.ConferenceControl.Requests;
using Strive.Core.Services.ParticipantsList;
using Strive.Core.Services.Rooms;
using Strive.Core.Services.Rooms.Requests;
using Xunit;
using Xunit.Abstractions;

namespace Strive.Core.IntegrationTests.Services
{
    public class ChatTests : ServiceIntegrationTest
    {
        private const string ConferenceId = "123";
        private readonly Conference _conference = new(ConferenceId);

        private static readonly Participant TestParticipant1 = new(ConferenceId, "1");
        private static readonly Participant TestParticipant2 = new(ConferenceId, "2");

        private static readonly TestParticipantConnection TestParticipantConnection1 =
            new(TestParticipant1, "test", new ParticipantMetadata("Sven"));

        private static readonly TestParticipantConnection TestParticipantConnection2 =
            new(TestParticipant2, "test2", new ParticipantMetadata("Alfred"));

        public ChatTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            base.ConfigureContainer(builder);

            SetupConferenceControl(builder);
            AddConferenceRepo(builder, _conference);
            builder.RegisterType<TaskDelay>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ParticipantTypingTimer>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(new OptionsWrapper<RoomOptions>(new RoomOptions())).AsImplementedInterfaces()
                .SingleInstance();
        }

        protected override IEnumerable<Type> FetchServiceTypes()
        {
            return FetchTypesForSynchronizedObjects().Concat(FetchTypesOfNamespace(typeof(SynchronizedChat)))
                .Concat(FetchTypesOfNamespace(typeof(SynchronizedParticipants)))
                .Concat(FetchTypesOfNamespace(typeof(SynchronizedRooms)));
        }

        [Fact]
        public async Task SendChatMessage_SendToGlobalChannel_PublishChatMessageNotification()
        {
            const string message = "Hello World";

            var sender = TestParticipantConnection1;

            // arrange
            await JoinParticipant(TestParticipantConnection1);
            await JoinParticipant(TestParticipantConnection2);

            // act
            var messageOptions = new ChatMessageOptions();
            await Mediator.Send(new SendChatMessageRequest(sender.Participant, message, GlobalChatChannel.Instance,
                messageOptions));

            // assert
            NotificationCollector.AssertSingleNotificationIssued<ChatMessageReceivedNotification>(notification =>
            {
                Assert.Equal(2, notification.Participants.Count);
                Assert.Contains(TestParticipant1, notification.Participants);
                Assert.Contains(TestParticipant2, notification.Participants);
                Assert.Equal(ConferenceId, notification.ConferenceId);
                Assert.Equal(message, notification.ChatMessage.Message);
                Assert.Equal(sender.Participant.Id, notification.ChatMessage.Sender.ParticipantId);
                Assert.Equal(sender.Meta, notification.ChatMessage.Sender.Meta);
                Assert.Equal(1, notification.TotalMessagesInChannel);
                Assert.Equal(messageOptions, notification.ChatMessage.Options);
            });
        }

        [Fact]
        public async Task SendChatMessage_SendToRoomChannel_OnlyIncludeParticipantsFromTheSameRoom()
        {
            var sender = TestParticipantConnection1;

            // arrange
            await Mediator.Send(new OpenConferenceRequest(ConferenceId));

            await JoinParticipant(TestParticipantConnection1);
            await JoinParticipant(TestParticipantConnection2);

            var rooms = await Mediator.Send(new CreateRoomsRequest(ConferenceId,
                new[] {new RoomCreationInfo("Room1"), new RoomCreationInfo("Room2")}));

            await Mediator.Send(SetParticipantRoomRequest.MoveParticipant(TestParticipant1, rooms[0].RoomId));
            await Mediator.Send(SetParticipantRoomRequest.MoveParticipant(TestParticipant2, rooms[1].RoomId));

            // act
            await Mediator.Send(new SendChatMessageRequest(sender.Participant, "Hello World",
                new RoomChatChannel(rooms[0].RoomId), new ChatMessageOptions()));

            // assert
            NotificationCollector.AssertSingleNotificationIssued<ChatMessageReceivedNotification>(notification =>
            {
                Assert.Equal(TestParticipant1, Assert.Single(notification.Participants));
            });
        }

        [Fact]
        public async Task SendChatMessage_ParticipantIsTyping_RemoveParticipantTyping()
        {
            var sender = TestParticipantConnection1;

            // arrange
            await Mediator.Send(new OpenConferenceRequest(ConferenceId));

            await JoinParticipant(TestParticipantConnection1);
            await Mediator.Send(new SetParticipantTypingRequest(TestParticipant1, GlobalChatChannel.Instance, true));

            // act
            await Mediator.Send(new SendChatMessageRequest(sender.Participant, "Hello World",
                GlobalChatChannel.Instance, new ChatMessageOptions()));

            // assert
            var syncObjId = SynchronizedChat.SyncObjId(GlobalChatChannel.Instance);
            var syncObj =
                SynchronizedObjectListener.GetSynchronizedObject<SynchronizedChat>(TestParticipant1, syncObjId);
            Assert.Empty(syncObj.ParticipantsTyping);
        }

        [Fact]
        public async Task SetParticipantIsTyping_SetTypingToTrue_UpdateSynchronizedObject()
        {
            // arrange
            await JoinParticipant(TestParticipantConnection1);

            // act
            var channel = GlobalChatChannel.Instance;
            await Mediator.Send(new SetParticipantTypingRequest(TestParticipant1, channel, true));

            // assert
            var syncObjId = SynchronizedChat.SyncObjId(channel);
            var synchronizedObject =
                SynchronizedObjectListener.GetSynchronizedObject<SynchronizedChat>(TestParticipant1, syncObjId);

            var entry = Assert.Single(synchronizedObject.ParticipantsTyping);
            Assert.Equal(entry.Key, TestParticipant1.Id);
        }

        [Fact]
        public async Task SetParticipantIsTyping_SetTypingToBackToFalse_UpdateSynchronizedObject()
        {
            // arrange
            await JoinParticipant(TestParticipantConnection1);

            // act
            var channel = GlobalChatChannel.Instance;
            await Mediator.Send(new SetParticipantTypingRequest(TestParticipant1, channel, true));
            await Mediator.Send(new SetParticipantTypingRequest(TestParticipant1, channel, false));

            // assert
            var syncObjId = SynchronizedChat.SyncObjId(channel);
            var synchronizedObject =
                SynchronizedObjectListener.GetSynchronizedObject<SynchronizedChat>(TestParticipant1, syncObjId);

            Assert.Empty(synchronizedObject.ParticipantsTyping);
        }

        [Fact]
        public async Task FetchMessages_NoMessagesExist_ReturnEmptyList()
        {
            // arrange
            await Mediator.Send(new OpenConferenceRequest(ConferenceId));

            // act
            var messages =
                await Mediator.Send(new FetchMessagesRequest(ConferenceId, GlobalChatChannel.Instance, -50, -1));

            // assert
            Assert.Equal(0, messages.TotalLength);
            Assert.Empty(messages.Result);
        }

        [Fact]
        public async Task FetchMessages_SingleMessageSent_ReturnMessage()
        {
            // arrange
            await Mediator.Send(new OpenConferenceRequest(ConferenceId));
            await JoinParticipant(TestParticipantConnection1);

            await Mediator.Send(new SendChatMessageRequest(TestParticipant1, "Hello World", GlobalChatChannel.Instance,
                new ChatMessageOptions()));

            // act
            var messages =
                await Mediator.Send(new FetchMessagesRequest(ConferenceId, GlobalChatChannel.Instance, -50, -1));

            // assert
            Assert.Equal(1, messages.TotalLength);
            Assert.Single(messages.Result);
        }

        [Fact]
        public async Task ParticipantLeft_IsStillTyping_RemoveTypingStatus()
        {
            var channel = GlobalChatChannel.Instance;

            // arrange
            await Mediator.Send(new OpenConferenceRequest(ConferenceId));
            await JoinParticipant(TestParticipantConnection1);
            await JoinParticipant(TestParticipantConnection2);

            await Mediator.Send(new SetParticipantTypingRequest(TestParticipant2, channel, true));

            // act
            await Mediator.Publish(new ParticipantLeftNotification(TestParticipant2,
                TestParticipantConnection2.ConnectionId));

            // assert
            var syncObjId = SynchronizedChat.SyncObjId(channel);
            var syncObj =
                SynchronizedObjectListener.GetSynchronizedObject<SynchronizedChat>(TestParticipant1, syncObjId);

            Assert.Empty(syncObj.ParticipantsTyping);
        }

        [Fact]
        public async Task ParticipantRoomChanged_IsStillTyping_RemoveTypingStatus()
        {
            // arrange
            await Mediator.Send(new OpenConferenceRequest(ConferenceId));
            await JoinParticipant(TestParticipantConnection1);
            await JoinParticipant(TestParticipantConnection2);

            var rooms = await Mediator.Send(new CreateRoomsRequest(ConferenceId,
                new[] {new RoomCreationInfo("Room1"), new RoomCreationInfo("Room2")}));

            await Mediator.Send(SetParticipantRoomRequest.MoveParticipant(TestParticipant1, rooms[0].RoomId));
            await Mediator.Send(SetParticipantRoomRequest.MoveParticipant(TestParticipant2, rooms[0].RoomId));

            var channel = new RoomChatChannel(rooms[0].RoomId);
            await Mediator.Send(new SetParticipantTypingRequest(TestParticipant2, channel, true));

            // act
            await Mediator.Send(SetParticipantRoomRequest.MoveParticipant(TestParticipant2, rooms[1].RoomId));

            // assert
            var syncObjId = SynchronizedChat.SyncObjId(channel);
            var syncObj =
                SynchronizedObjectListener.GetSynchronizedObject<SynchronizedChat>(TestParticipant1, syncObjId);

            Assert.Empty(syncObj.ParticipantsTyping);
        }
    }
}
