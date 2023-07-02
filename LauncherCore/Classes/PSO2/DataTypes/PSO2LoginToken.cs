using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes
{
    public sealed class PSO2LoginToken : IDisposable
    {
        public readonly string UserId;
        public readonly string Token;
        public readonly bool RequireOTP;

        private bool disposed;

        public PSO2LoginToken(in JsonElement element)
        {
            this.disposed = false;
            if (!element.TryGetProperty("userId", out var prop_userId) || prop_userId.ValueKind != JsonValueKind.String ||
                !element.TryGetProperty("token", out var prop_token) || prop_token.ValueKind != JsonValueKind.String ||
                !element.TryGetProperty("otpRequired", out var prop_otpRequired))
            {
                throw new UnexpectedDataFormatException();
            }
            var valTest = prop_userId.GetString();
            if (string.IsNullOrEmpty(valTest)) throw new UnexpectedDataFormatException();
            this.UserId = valTest;

            valTest = prop_token.GetString();
            if (string.IsNullOrEmpty(valTest)) throw new UnexpectedDataFormatException();
            this.Token = valTest;
            if (prop_otpRequired.ValueKind == JsonValueKind.True || prop_otpRequired.ValueKind == JsonValueKind.False)
            {
                this.RequireOTP = prop_otpRequired.GetBoolean();
            }
            else if (prop_otpRequired.ValueKind == JsonValueKind.Null)
            {
                this.RequireOTP = false;
            }
            else
            {
                this.RequireOTP = true;
            }
        }

        [Obsolete("Not recommended to use", false)]
        public string GetSGToken() => $"{this.Token}@{this.UserId}";

        public void AppendToStartInfo(System.Diagnostics.ProcessStartInfo startInfo)
        {
            startInfo.ArgumentList.Add("-sgtoken");
            startInfo.ArgumentList.Add($"{this.Token}@{this.UserId}");
        }

        public void Dispose()
        {
            if (this.disposed) return;
            this.disposed = true;

            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Not really matter since token are one-time use.</summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                unsafe
                {
                    if (this.UserId != null && this.UserId.Length != 0)
                    {
                        fixed (char* c = this.UserId)
                        {
                            for (int i = 0; i < this.UserId.Length; i++)
                            {
                                c[i] = '\0';
                            }
                        }
                    }

                    if (this.Token != null && this.Token.Length != 0)
                    {
                        fixed (char* c = this.Token)
                        {
                            for (int i = 0; i < this.Token.Length; i++)
                            {
                                c[i] = '\0';
                            }
                        }
                    }
                }
            }
        }

        ~PSO2LoginToken()
        {
            this.Dispose();
        }
    }
}
