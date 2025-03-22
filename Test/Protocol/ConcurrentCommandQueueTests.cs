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
using DarkestBot.Protocol.Commands;

namespace DarkestBotTests.Protocol
{
    public class ConcurrentCommandQueueTests
    {
        [Fact]
        public void TryGetCommand_ReturnsFalse_WhenQueueIsEmpty()
        {
            // Arrange
            var queue = new ConcurrentCommandQueue();

            // Act
            var result = queue.TryGetCommand(out Command command);

            // Assert
            Assert.False(result);
            Assert.Null(command);
        }

        [Fact]
        public void SendCommand_EnqueuesCommand_And_TryGetCommand_ReturnsSameCommand()
        {
            // Arrange
            var queue = new ConcurrentCommandQueue();
            var cmd = new Command(MessageType.PIN);

            // Act
            queue.SendCommand(cmd);
            var result = queue.TryGetCommand(out Command dequeuedCommand);

            // Assert
            Assert.True(result);
            Assert.Equal(cmd, dequeuedCommand);
        }

        [Fact]
        public void SendCommand_MaintainsOrder_ForMultipleCommands()
        {
            // Arrange
            var queue = new ConcurrentCommandQueue();
            var cmd1 = new Command(MessageType.PIN);
            var cmd2 = new Command(MessageType.MSG);

            // Act
            queue.SendCommand(cmd1);
            queue.SendCommand(cmd2);
            var firstResult = queue.TryGetCommand(out Command firstDequeued);
            var secondResult = queue.TryGetCommand(out Command secondDequeued);

            // Assert
            Assert.True(firstResult);
            Assert.True(secondResult);
            Assert.Equal(cmd1, firstDequeued);
            Assert.Equal(cmd2, secondDequeued);
        }
    }
}
