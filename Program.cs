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

        static int typingIndicatorAmount = 4;

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
                if (messageArray[i].MessageType == DSharpPlus.Entities.MessageType.Call)
                {
                    newMessage(messageArray[i].Author.Username.ToUpper() + " started a call.");
                }
                else if (messageArray[i].MessageType == DSharpPlus.Entities.MessageType.ChannelIconChange)
                {
                    newMessage(messageArray[i].Author.Username.ToUpper() + " changed the channel icon.");
                }
                else if (messageArray[i].MessageType == DSharpPlus.Entities.MessageType.ChannelNameChange)
                {
                    newMessage(messageArray[i].Author.Username.ToUpper() + " changed the channel name.");
                }
                else if (messageArray[i].MessageType == DSharpPlus.Entities.MessageType.ChannelPinnedMessage)
                {
                    newMessage(messageArray[i].Author.Username.ToUpper() + " pinned a message.");
                }
                else if (messageArray[i].MessageType == DSharpPlus.Entities.MessageType.RecipientAdd)
                {
                    newMessage(messageArray[i].Author.Username.ToUpper() + " added a user.");
                }
                else if (messageArray[i].MessageType == DSharpPlus.Entities.MessageType.RecipientRemove)
                {
                    newMessage(messageArray[i].Author.Username.ToUpper() + " removed a user.");
                }
                else if (messageArray[i].MessageType == DSharpPlus.Entities.MessageType.Default)
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

                    newMessage(messageArray[i].Author.Username.ToUpper() + ":" + messageArray[i].Content + extraInfo);
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

        public static void typingIndicator(string username)
        {
            for (int i = 0; i < typingIndicatorAmount; i++)
            {
                LCDWrapper.LogiLcdMonoSetText(3, username + " is typing...");
                LCDWrapper.LogiLcdUpdate();
                Thread.Sleep(500);
                LCDWrapper.LogiLcdMonoSetText(3, line3);
                LCDWrapper.LogiLcdUpdate();
                Thread.Sleep(500);
            }
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

            getPrevMessages();
        };

        discord.TypingStarted += async t =>
        {
            if (t.Channel == selectedChannel)
            {
                Console.WriteLine("User typing!");

                Thread typingIndicatorThread = new Thread(() => typingIndicator(t.User.Username.ToUpper()));
                typingIndicatorThread.Start();
            }
        };

            discord.DmChannelCreated += async c =>
        {
            dmChannels.Add(c.Channel);
            if (c.Channel.Recipients.Count() > 1)
            {
                Console.WriteLine("Channel added: " + c.Channel.Recipients[0].Username + " + " + c.Channel.Recipients.Count + " More");
            }
            else if (c.Channel.Recipients.Count() == 1)
            {
                Console.WriteLine("Channel added: " + c.Channel.Recipients[0].Username);
            }
        };

        discord.DmChannelDeleted += async c =>
        {
            for (int i = 0; i < dmChannels.Count(); i++)
            {
                if (dmChannels[i] == c.Channel)
                {
                    if (dmChannels[i].Recipients.Count() > 1)
                    {
                        Console.WriteLine("Channel removed: " + dmChannels[i].Recipients[0].Username + " + " + dmChannels[i].Recipients.Count + " More");
                    } else if (dmChannels[i].Recipients.Count() == 1) {
                        Console.WriteLine("Channel removed: " + dmChannels[i].Recipients[0].Username);
                    }
                    dmChannels.RemoveAt(i);
                    break;
                }
            }
        };

        discord.MessageCreated += async e => {
            if (window == 0)
            {
                if (e.Channel == selectedChannel)
                {
                    if (e.Message.MessageType == DSharpPlus.Entities.MessageType.Call)
                    {
                        newMessage(e.Author.Username.ToUpper() + " started a call.");
                    }
                    else if (e.Message.MessageType == DSharpPlus.Entities.MessageType.ChannelIconChange)
                    {
                        newMessage(e.Author.Username.ToUpper() + " changed the channel icon.");
                    }
                    else if (e.Message.MessageType == DSharpPlus.Entities.MessageType.ChannelNameChange)
                    {
                        newMessage(e.Author.Username.ToUpper() + " changed the channel name.");
                    }
                    else if (e.Message.MessageType == DSharpPlus.Entities.MessageType.ChannelPinnedMessage)
                    {
                        newMessage(e.Author.Username.ToUpper() + " pinned a message.");
                    }
                    else if (e.Message.MessageType == DSharpPlus.Entities.MessageType.RecipientAdd)
                    {
                        newMessage(e.Author.Username.ToUpper() + " added a user.");
                    }
                    else if (e.Message.MessageType == DSharpPlus.Entities.MessageType.RecipientRemove)
                    {
                        newMessage(e.Author.Username.ToUpper() + " removed a user.");
                    } else if (e.Message.MessageType == DSharpPlus.Entities.MessageType.Default)
                    {
                        string extraInfo = "";
                        int attachmentCount = e.Message.Attachments.Count();

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

                        newMessage(e.Author.Username.ToUpper() + ":" + e.Message.Content + extraInfo);
                    }
                }
            }
        };

        await discord.ConnectAsync();
        await Task.Delay(-1);
    }

    public static void newMessage(string msg)
    {
        var messageParts = (msg).SplitInParts(27);
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

            Console.WriteLine(stringArray[i]);

            LCDWrapper.LogiLcdUpdate();
        }
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
