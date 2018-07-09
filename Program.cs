using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;

namespace DiscordDMLogitechLCD
{
    class Program
    {
        static DiscordClient discord;
        static string cfgToken;

        static void Main(string[] args)
        {
            cfgToken = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\token.cfg");
            Console.WriteLine("Connecting with " + cfgToken);

            LCDWrapper.LogiLcdInit("Discord", LCDWrapper.LOGI_LCD_TYPE_MONO);
            Console.WriteLine("Is mono screen connected? " + LCDWrapper.LogiLcdIsConnected(LCDWrapper.LOGI_LCD_TYPE_MONO));
            LCDWrapper.LogiLcdMonoSetText(0, "DISCORD LOADED");
            LCDWrapper.LogiLcdUpdate();

            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

            LCDWrapper.LogiLcdShutdown();
        }

        static async Task MainAsync(string[] args)
        {
            string line0 = "";
            string line1 = "";
            string line2 = "";
            string line3 = "";

            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = cfgToken,
                TokenType = TokenType.User
            });

            discord.MessageCreated += async e => {
                string extraInfo = "";
                int attachmentCount = e.Message.Attachments.Count();

                if (attachmentCount > 0)
                {
                    if (attachmentCount == 1)
                    {
                        extraInfo = (" [" + attachmentCount + " Attachment]");
                    } else if (attachmentCount > 1)
                    {
                        extraInfo = (" [" + attachmentCount + " Attachments]");
                    }
                }

                if (e.Channel.IsPrivate)
                {
                    Console.WriteLine(e.Author.Username.ToUpper() + ": " + e.Message.Content + extraInfo);
                    var messageParts = (e.Author.Username.ToUpper() + ": " + e.Message.Content + extraInfo).SplitInParts(27);
                    string[] stringArray = messageParts.Select(p => p).ToArray();

                    int parts = stringArray.Length;

                    for (int i = 0; i < parts; i++)
                    {
                        line0 = line1;
                        LCDWrapper.LogiLcdMonoSetText(0, line1);

                        line1 = line2;
                        LCDWrapper.LogiLcdMonoSetText(1, line2);

                        line2 = line3;
                        LCDWrapper.LogiLcdMonoSetText(2, line3);

                        LCDWrapper.LogiLcdMonoSetText(3, stringArray[i]);
                        line3 = stringArray[i];

                        LCDWrapper.LogiLcdUpdate();
                    }
                }
            };

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

    }
    static class StringExtensions
    {
        public static IEnumerable<String> SplitInParts(this String s, Int32 partLength)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            for (var i = 0; i < s.Length; i += partLength)
            {
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
            }
        }
    }
}
