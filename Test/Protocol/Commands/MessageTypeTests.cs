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

using DarkestBot.Protocol.Commands;
using System.Reflection;

namespace DarkestBotTests.Protocol.Commands
{
    public class MessageTypeTests
    {
        /// <summary>
        ///     Make sure all public message types have a matching code. For instance, MessageType.PIN should have a code "PIN".
        /// </summary>
        [Fact]
        public void AllDeclaredMessageTypes_ShouldHaveMatchingFieldNameAndCode()
        {
            var messageTypeType = typeof(MessageType);

            var fields = messageTypeType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

            foreach (var field in fields)
            {
                if (field.FieldType != typeof(MessageType))
                {
                    continue;
                }

                string fieldName = field.Name;
                string messageTypeCode = ((MessageType)field.GetValue(null)!).Code;

                Assert.Equal(fieldName, messageTypeCode);
            }
        }

        [Fact]
        public void GetType_NonExistent_ReturnsNull()
        {
            var wrongType = MessageType.Get("not a real code");
            Assert.Null(wrongType);
        }

        [Fact]
        public void GetType_String_GetsCorrectMessage()
        {
            var messageType = MessageType.Get("IDN");
            Assert.Equal(MessageType.IDN, messageType);
        }
    }
}
