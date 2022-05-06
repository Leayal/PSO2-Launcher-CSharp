using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.Shared
{
    public static class Sha1StringHelper
    {
        private static readonly bool IsManagedPossible;

        static Sha1StringHelper()
        {
            SHA1 sha1 = null;
            try
            {
                sha1 = new SHA1Managed();
                IsManagedPossible = true;
            }
            catch (InvalidOperationException)
            {
                IsManagedPossible = false;
            }
            finally
            {
                sha1?.Dispose();
            }
        }

        public static string GenerateFromString(string str)
        {
            using (var sha1 = IsManagedPossible ? new SHA1Managed() : SHA1.Create())
            {
                return Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(str)));
            }
        }
    }
}
