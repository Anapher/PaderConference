﻿using AutoMapper;
using PaderConference.Core.Domain.Entities;
using PaderConference.Core.Dto.Services;
using PaderConference.Core.Services.Chat;
using PaderConference.Core.Services.Chat.Dto;

namespace PaderConference.Core
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<ChatMessage, ChatMessageDto>().ForMember(x => x.ParticipantId,
                options => options.PreCondition(x => !(x.Mode is SendAnonymously))).ForMember(x => x.IsPrivate,
                opt => opt.MapFrom(x => x.Mode is SendPrivately));
            CreateMap<Participant, ParticipantDto>();
        }
    }
}