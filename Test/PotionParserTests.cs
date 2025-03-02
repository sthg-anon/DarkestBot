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

using DarkestBot;

namespace DarkestBotTests
{
    public class PotionParserTests
    {
        [Fact]
        public void TryParse_Invalid_ReturnsFalse()
        {
            var message = "not potion stuff!";
            var result = PotionParser.TryParse(message, out var potion);
            Assert.Null(potion);
            Assert.False(result);
        }

        [Fact]
        public void TryParse_Empty_ReturnsFalse()
        {
            var result = PotionParser.TryParse(string.Empty, out var potion);
            Assert.Null(potion);
            Assert.False(result);
        }

        [Fact]
        public void TryParse_Whitespace_ReturnsFalse()
        {
            var result = PotionParser.TryParse(" ", out var potion);
            Assert.Null(potion);
            Assert.False(result);
        }

        [Fact]
        public void TryParse_Valid_ReturnsTrueWithPotionData()
        {
            string message = "[sub]Paying 250 rings and buying a potion...[/sub]\r\nHopefully this helps.\r\nHuge flask of [b]Skin Color[/b][color=orange] ( ☆☆☆☆ strength )[/color][eicon]potion11[/eicon] \r\n[sub]Changes the color of your skin to dark red.[/sub]\r\n[sub]Mead flavored. Common[/sub]";
            //string message = "[sub]Paying 250 rings and buying a potion...[/sub] Hopefully this helps. Huge flask of [b]Skin Color[/b][color=orange] ( ☆☆☆☆ strength )[/color][eicon]potion11[/eicon] [sub]Changes the color of your skin to dark red.[/sub] [sub]Mead flavored. Common[/sub]";


            var result = PotionParser.TryParse(message, out var potion);
            Assert.NotNull(potion);
            Assert.True(result);

            Assert.Equal("Skin Color", potion.Name);
            Assert.Equal("potion11", potion.Eicon);
            Assert.Equal("Changes the color of your skin to dark red.", potion.Descritpion);
        }
    }
}
