﻿/*
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

using Serilog;

namespace DarkestBot.UserCommands.Commands
{
    internal sealed class DiceBotRefusePotionCommand(Queue<string> potionBuyers) : IUserCommand
    {
        private const string CommandPrefix = "Failed: You could not afford to buy a potion for";
        private const string ExpectedPotionGiver = "Dice Bot";

        public UserCommandMode AllowedModes => UserCommandMode.Public;

        public void TryExecute(string commandSender, string message, IChatResponder responder)
        {
            if (!string.Equals(commandSender, ExpectedPotionGiver, StringComparison.Ordinal))
            {
                return;
            }

            if (!message.StartsWith(CommandPrefix, StringComparison.Ordinal))
            {
                return;
            }

            if (potionBuyers.TryDequeue(out var potionBuyer))
            {
                Log.Information("{character} cannot afford a potion!", potionBuyer);
            }
            else
            {
                Log.Warning("I don't know who can't afford a potion!");
            }
        }
    }
}
