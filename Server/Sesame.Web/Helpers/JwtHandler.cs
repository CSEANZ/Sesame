﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sesame.Web.Models;

namespace Sesame.Web.Helpers
{
    public class JwtHandler : IJwtHandler
    {
        private readonly JwtSettings _settings;
        private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        private SecurityKey _issuerSigningKey;
        private SigningCredentials _signingCredentials;
        private JwtHeader _jwtHeader;
        public TokenValidationParameters Parameters { get; private set; }

        public JwtHandler(IOptions<JwtSettings> settings)
        {
            _settings = settings.Value;
            if (_settings.UseRsa)
            {
                InitializeRsa();
            }
            else
            {
                InitializeHmac();
            }

            InitializeJwtParameters();
        }

        private void InitializeRsa()
        {
            using (var publicRsa = RSA.Create())
            {
                var publicKeyXml = File.ReadAllText($@"{Directory.GetCurrentDirectory()}/Keys/{_settings.RsaPublicKeyXml}");
                Xmlhelper.FromXmlString(publicRsa, publicKeyXml);
                _issuerSigningKey = new RsaSecurityKey(publicRsa);
            }

            if (string.IsNullOrWhiteSpace(_settings.RsaPrivateKeyXml))
            {
                return;
            }

            using (RSA privateRsa = RSA.Create())
            {
                var privateKeyXml = File.ReadAllText($@"{Directory.GetCurrentDirectory()}/Keys/{_settings.RsaPrivateKeyXml}");
                Xmlhelper.FromXmlString(privateRsa, privateKeyXml);
                var privateKey = new RsaSecurityKey(privateRsa);
                _signingCredentials = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256);
            }
        }

        private void InitializeHmac()
        {
            _issuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.HmacSecretKey));
            _signingCredentials = new SigningCredentials(_issuerSigningKey, SecurityAlgorithms.HmacSha256);
        }

        private void InitializeJwtParameters()
        {
            _jwtHeader = new JwtHeader(_signingCredentials);
            Parameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidIssuer = _settings.Issuer,
                IssuerSigningKey = _issuerSigningKey
            };
        }

        public JWT Create(IDictionary<string, string> claims)
        {
            var nowUtc = DateTime.UtcNow;
            var expires = nowUtc.AddDays(_settings.ExpiryDays);
            var centuryBegin = new DateTime(1970, 1, 1);
            var exp = (long)(new TimeSpan(expires.Ticks - centuryBegin.Ticks).TotalSeconds);
            var now = (long)(new TimeSpan(nowUtc.Ticks - centuryBegin.Ticks).TotalSeconds);
            var issuer = _settings.Issuer ?? string.Empty;
            var payload = new JwtPayload();
            foreach (var claimDict in claims)
            {
                payload.AddClaim(new Claim(claimDict.Key, claimDict.Value));
            }

            var jwt = new JwtSecurityToken(_jwtHeader, payload);
            var token = _jwtSecurityTokenHandler.WriteToken(jwt);




            return new JWT
            {
                Token = token,
                Expires = exp
            };
        }

        public RsaSecurityKey GetPublicRsaSecurityKey()
        {
            var publicRsa = RSA.Create();
            var publicKeyXml = File.ReadAllText(_settings.RsaPublicKeyXml);
            Xmlhelper.FromXmlString(publicRsa, publicKeyXml);
            var publicRsaSecurityKey = new RsaSecurityKey(publicRsa);
            return publicRsaSecurityKey;
        }
        public RsaSecurityKey GetPrivateRsaSecurityKey()
        {
            var privateRsa = RSA.Create();
            var privateKeyXml = File.ReadAllText(_settings.RsaPrivateKeyXml);
            Xmlhelper.FromXmlString(privateRsa, privateKeyXml);
            var priavteRsaSecurityKey = new RsaSecurityKey(privateRsa);
            return priavteRsaSecurityKey;
        }

    }
}
