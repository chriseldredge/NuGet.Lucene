using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NuGet.Lucene.Tests
{
    public static class AssertEx
    {
        public static async Task<T> AssertThrowsAsync<T>(this Func<Task> testCode) where T : Exception
        {
            try
            {
                await testCode();
                Assert.Throws<T>(() => { });

                // Never reached. Compiler doesn't know Assert.Throws above always throws.
                return null;
            }
            catch (T exception)
            {
                return exception;
            }
        }
    }
}