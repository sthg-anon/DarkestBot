/*
 * Copyright (c) 2025 Aller
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using DarkestBot.Protocol;
using System.Text;

namespace DarkestBotTests.Protocol
{
    public class CommandFactoryTests
    {
        [Theory]
        [InlineData("Hello, world!", 100)] // well under limit
        [InlineData("Hello, world!", 5)]   // truncation mid-ASCII
        [InlineData("世界", 6)]            // each char = 3 bytes in UTF-8
        [InlineData("👋🏽", 4)]             // emoji is 4 bytes, skin tone adds more
        [InlineData("👋🏽", 10)]            // should capture full emoji
        [InlineData("", 10)]              // empty string
        public void TruncateUtfSafe_Output_Is_Valid_And_Under_Byte_Limit(string input, int maxBytes)
        {
            var result = CommandFactory.TruncateUtf8Safe(maxBytes, input);

            Assert.NotNull(result);
            int byteCount = Encoding.UTF8.GetByteCount(result);
            Assert.True(byteCount <= maxBytes, $"Output exceeds byte limit: {byteCount} > {maxBytes}");

            // Re-encode and decode to ensure valid UTF-8
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(result);
            string roundTrip = Encoding.UTF8.GetString(utf8Bytes);
            Assert.Equal(result, roundTrip);
        }

        [Fact]
        public void TruncateUtfSafe_Exact_Byte_Limit_Match_Should_Keep_String()
        {
            int maxBytes = 3;
            string input = "abc"; // ASCII: 3 bytes
            var result = CommandFactory.TruncateUtf8Safe(maxBytes, input);

            Assert.Equal("abc", result);
        }

        [Fact]
        public void TruncateUtfSafe_Truncates_MultiByte_Char_Safely()
        {
            int maxBytes = 5;
            string input = "éééé"; // each é = 2 bytes in UTF-8 → total 8 bytes
            string expected = "éé"; // 4 bytes total

            var result = CommandFactory.TruncateUtf8Safe(maxBytes, input);

            Assert.NotNull(result);
            int byteCount = Encoding.UTF8.GetByteCount(result);
            Assert.True(byteCount <= 5);
            Assert.True(result.Length < input.Length);
            Assert.Equal(expected, result);
        }
    }
}
