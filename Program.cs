using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;

namespace DiscordDMLogitechLCD
{
    class Program
    {
        static DiscordClient discord;
        static int window = 0;
        static List<DSharpPlus.Entities.DiscordDmChannel> dmChannels = new List<DSharpPlus.Entities.DiscordDmChannel>();
        static DSharpPlus.Entities.DiscordDmChannel selectedChannel;
        static string cfgToken;

        static string line0 = "";
        static string line1 = "";
        static string line2 = "";
        static string line3 = "";

        static void Main(string[] args)
        {
            FileStream fs = new FileStream("token.cfg", FileMode.OpenOrCreate, FileAccess.Read);

            using (var streamReader = new StreamReader(fs, Encoding.UTF8))
            {
                cfgToken = streamReader.ReadToEnd();
            }
            Console.WriteLine("Connecting with " + cfgToken);

            LCDWrapper.LogiLcdInit("Discord", LCDWrapper.LOGI_LCD_TYPE_MONO);
            Console.WriteLine("Is mono screen connected? " + LCDWrapper.LogiLcdIsConnected(LCDWrapper.LOGI_LCD_TYPE_MONO));
            LCDWrapper.LogiLcdMonoSetText(0, "DISCORD LOADED");
            LCDWrapper.LogiLcdUpdate();

            ThreadStart buttonChecker = new ThreadStart(checkButtonPress);
            Thread buttonCheckerThread = new Thread(buttonChecker);
            buttonCheckerThread.Start();
            Console.WriteLine("[Background] Checking for function button presses...");

            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

            LCDWrapper.LogiLcdShutdown();
        }

        public static void checkButtonPress()
        {
            while (true)
            {
                Thread.Sleep(100);
                if (LCDWrapper.LogiLcdIsButtonPressed(LCDWrapper.LOGI_LCD_MONO_BUTTON_0))
                {
                    if (window != 0)
                    {
                        changeWindow(0);
                    }
                }
                if (LCDWrapper.LogiLcdIsButtonPressed(LCDWrapper.LOGI_LCD_MONO_BUTTON_1))
                {
                    if (window != 1)
                    {
                        changeWindow(1);
                        Thread.Sleep(300);
                    } else if (window == 1)
                    {
                        dmSelect();
                        Thread.Sleep(300);
                    }
                }
                if (LCDWrapper.LogiLcdIsButtonPressed(LCDWrapper.LOGI_LCD_MONO_BUTTON_2))
                {
                    Console.WriteLine("Button 2 pressed!");
                }
                if (LCDWrapper.LogiLcdIsButtonPressed(LCDWrapper.LOGI_LCD_MONO_BUTTON_3))
                {
                    Console.WriteLine("Button 3 pressed!");
                }
            }
        }

        static void changeWindow(int windowID)
        {
            if (windowID == 1)
            {
                DSharpPlus.Entities.DiscordDmChannel[] tempChannels = dmChannels.ToArray();

                for (int i = 0; i < tempChannels.Length; i++)
                {
                    if (tempChannels[i] == selectedChannel)
                    {
                        DSharpPlus.Entities.DiscordUser[] tempUsers = tempChannels[i].Recipients.ToArray();
                        if (tempUsers.Length == 1)
                        {
                            string channelName = tempUsers[0].Username + " <--";
                            LCDWrapper.LogiLcdMonoSetText(0, channelName);
                        } else if (tempUsers.Length > 1)
                        {
                            string channelName = tempUsers[0].Username + " + " + tempUsers.Length + " More <--";
                            LCDWrapper.LogiLcdMonoSetText(0, channelName);
                        }

                        for (int c = 1; c < tempChannels.Length; c++)
                        {
                            if (c <= 3)
                            {
                                if ((i + c) < tempChannels.Length)
                                {
                                    tempUsers = tempChannels[i + c].Recipients.ToArray();
                                    if (tempUsers.Length == 1)
                                    {
                                        LCDWrapper.LogiLcdMonoSetText(c, tempUsers[0].Username);
                                    }
                                    else if (tempUsers.Length > 1)
                                    {
                                        LCDWrapper.LogiLcdMonoSetText(c, tempUsers[0].Username + " + " + tempUsers.Length + " More");
                                    }
                                } else
                                {
                                    LCDWrapper.LogiLcdMonoSetText(c, "");
                                }
                            }
                        }
                    }
                }
                LCDWrapper.LogiLcdUpdate();
                Console.WriteLine("Set window to 1");
                window = 1;
            } else if (windowID == 0)
            {
                DSharpPlus.Entities.DiscordUser[] tempUsers = selectedChannel.Recipients.ToArray();
                if (tempUsers.Length == 1)
                {
                    LCDWrapper.LogiLcdMonoSetText(0, "Channel: " + tempUsers[0].Username);
                } else if (tempUsers.Length > 1)
                {
                    LCDWrapper.LogiLcdMonoSetText(0, "Channel: " + tempUsers[0].Username + " + " + tempUsers.Length + " More");
                }

                line0 = "";
                line1 = "";
                line2 = "";
                line3 = "";
                LCDWrapper.LogiLcdMonoSetText(1, "");
                LCDWrapper.LogiLcdMonoSetText(2, "");
                LCDWrapper.LogiLcdMonoSetText(3, "");
                LCDWrapper.LogiLcdUpdate();

                getPrevMessages();

                Console.WriteLine("Set window to 0");
                window = 0;
            }
        }

        public static async void getPrevMessages()
        {
            IReadOnlyList<DSharpPlus.Entities.DiscordMessage> messages = await selectedChannel.GetMessagesAsync(4);

            DSharpPlus.Entities.DiscordMessage[] messageArray = messages.ToArray();

            Array.Reverse(messageArray);

            for (int i = 0; i < messageArray.Length; i++)
            {
                string extraInfo = "";
                int attachmentCount = messageArray[i].Attachments.Count();

                if (attachmentCount > 0)
                {
                    if (attachmentCount == 1)
                    {
                        extraInfo = (" [" + attachmentCount + " Attachment]");
                    }
                    else if (attachmentCount > 1)
                    {
                        extraInfo = (" [" + attachmentCount + " Attachments]");
                    }
                }

                Console.WriteLine("Retrieved message: " + messageArray[i].Author.Username + ":" + messageArray[i].Content + extraInfo);
                var messageParts = (messageArray[i].Author.Username + ":" + messageArray[i].Content + extraInfo).SplitInParts(27);
                string[] stringArray = messageParts.Select(p => p).ToArray();

                int parts = stringArray.Length;

                for (int m = 0; m < parts; m++)
                {
                    line0 = line1;
                    LCDWrapper.LogiLcdMonoSetText(0, line1);

                    line1 = line2;
                    LCDWrapper.LogiLcdMonoSetText(1, line2);

                    line2 = line3;
                    LCDWrapper.LogiLcdMonoSetText(2, line3);

                    LCDWrapper.LogiLcdMonoSetText(3, stringArray[m]);
                    line3 = stringArray[m];

                    LCDWrapper.LogiLcdUpdate();
                }
            }
        }

    static void dmSelect()
    {
        DSharpPlus.Entities.DiscordDmChannel[] tempChannels = dmChannels.ToArray();
        for (int i = 0; i < tempChannels.Length; i++)
        {
            if (tempChannels[i] == selectedChannel)
            {
                if (tempChannels.Length > 1) {
                    if ((i + 1) < tempChannels.Length)
                    {
                        selectedChannel = tempChannels[i + 1];
                        Console.WriteLine("Selected channel: " + selectedChannel.Recipients[0].Username);
                    } else if ((i + 1) >= tempChannels.Length)
                    {
                        selectedChannel = tempChannels[0];
                        Console.WriteLine("Selected channel: " + selectedChannel.Recipients[0].Username);
                    }
                }
                break;
            }
        }

        for (int i = 0; i < tempChannels.Length; i++)
        {
            if (tempChannels[i] == selectedChannel)
            {
                DSharpPlus.Entities.DiscordUser[] tempUsers = tempChannels[i].Recipients.ToArray();
                if (tempUsers.Length == 1)
                {
                    LCDWrapper.LogiLcdMonoSetText(0, tempUsers[0].Username + " <--");
                } else if (tempUsers.Length > 1)
                {
                    LCDWrapper.LogiLcdMonoSetText(0, tempUsers[0].Username + " + " + tempUsers.Length + " More <--");
                }

                for (int c = 1; c < tempChannels.Length; c++)
                {
                    if (c <= 3) {
                        if ((i + c) < tempChannels.Length)
                        {
                            tempUsers = tempChannels[i + c].Recipients.ToArray();
                            if (tempUsers.Length == 1)
                            {
                                LCDWrapper.LogiLcdMonoSetText(c, tempUsers[0].Username);
                            } else if (tempUsers.Length > 1)
                            {
                                LCDWrapper.LogiLcdMonoSetText(c, tempUsers[0].Username + " + " + tempUsers.Length + " More");
                            }
                        } else
                        {
                            LCDWrapper.LogiLcdMonoSetText(c, "");
                        }
                    }
                }
            }
        }

        LCDWrapper.LogiLcdUpdate();
    }


    static async Task MainAsync(string[] args)
    {
        discord = new DiscordClient(new DiscordConfiguration
        {
            Token = cfgToken,
            TokenType = TokenType.User
        });

        discord.Ready += async r =>
        {
            Console.WriteLine("DM Channels: " + discord.PrivateChannels.Count());
            foreach (DSharpPlus.Entities.DiscordDmChannel channel in discord.PrivateChannels)
            {
                dmChannels.Add(channel);
            }
            selectedChannel = dmChannels[0];
            DSharpPlus.Entities.DiscordUser[] tempUsers = dmChannels[0].Recipients.ToArray();
            if (tempUsers.Length == 1)
            {
                LCDWrapper.LogiLcdMonoSetText(0, "Channel: " + tempUsers[0].Username);
            }
            else if (tempUsers.Length > 1)
            {
                LCDWrapper.LogiLcdMonoSetText(0, "Channel: " + tempUsers[0].Username + " + " + tempUsers.Length + " More");
            }
            LCDWrapper.LogiLcdUpdate();
            Console.WriteLine("Selected channel: " + dmChannels[0].Recipients[0].Username);
        };

        discord.MessageCreated += async e => {
            if (window == 0)
            {

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

                if (e.Channel == selectedChannel)
                {
                    Console.WriteLine(e.Author.Username.ToUpper() + ":" + e.Message.Content + extraInfo);
                    var messageParts = (e.Author.Username.ToUpper() + ":" + e.Message.Content + extraInfo).SplitInParts(27);
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
