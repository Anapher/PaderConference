﻿using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using PaderConference.Core.Domain.Entities;
using PaderConference.Core.Interfaces.Gateways.Repositories;

#pragma warning disable 8619

namespace PaderConference.Infrastructure.Data.Repos
{
    public class ConferenceRepo : MongoRepo<Conference>, IConferenceRepo, IMongoIndexBuilder
    {
        static ConferenceRepo()
        {
            BsonClassMap.RegisterClassMap<Conference>(x =>
            {
                x.AutoMap();
                x.MapIdMember(x => x.ConferenceId);
            });
        }

        public ConferenceRepo(IOptions<MongoDbOptions> options) : base(options)
        {
        }

        public Task<Conference?> FindById(string conferenceId)
        {
            return Collection.Find(x => x.ConferenceId == conferenceId).FirstOrDefaultAsync();
        }

        public Task Create(Conference conference)
        {
            return Collection.InsertOneAsync(conference);
        }

        public Task Update(Conference conference)
        {
            return Collection.ReplaceOneAsync(c => c.ConferenceId == conference.ConferenceId, conference);
        }

        public Task CreateIndexes()
        {
            return Task.CompletedTask;
            //var indexKeysDefinition = Builders<Conference>.IndexKeys.Ascending(conference => conference.ConferenceId);
            //await Collection.Indexes.CreateOneAsync(new CreateIndexModel<Conference>(indexKeysDefinition,
            //    new CreateIndexOptions {Unique = true}));
        }
    }
}