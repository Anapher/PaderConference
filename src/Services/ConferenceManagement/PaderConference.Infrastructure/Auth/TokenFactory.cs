﻿using System;
using System.Security.Cryptography;
using PaderConference.Core.Interfaces.Services;
using PaderConference.Infrastructure.Extensions;

namespace PaderConference.Infrastructure.Auth
{
    public class TokenFactory : ITokenFactory
    {
        public string GenerateToken(int size = 32)
        {
            var randomNumber = new byte[size];
            using var rng = RandomNumberGenerator.Create();

            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber).ToUrlBase64().Substring(0, size);
        }
    }
}