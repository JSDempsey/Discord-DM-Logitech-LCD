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
        static ulong channelToSelect = 0;
        static string cfgToken;

        static bool runOnStartup = false;

        static string line0 = "";
        static string line1 = "";
        static string line2 = "";
        static string line3 = "";

        static FileStream fs;

        static int notificationAmount = 4;

        static string dir;

        static void Main(string[] args)
        {
            string dirPath = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            dir = Path.GetDirectoryName(dirPath);
            dir = dir.Remove(0, 6);

            fs = new FileStream(dir + "\\config.cfg", FileMode.OpenOrCreate, FileAccess.ReadWrite);

            string configString;

            using (var streamReader = new StreamReader(fs, Encoding.UTF8))
            {
                configString = streamReader.ReadToEnd();
            }

            fs.Close();

            string[] configParts = configString.Split(new String[] { "\r\n" }, StringSplitOptions.None);

            if (configParts.Length == 1)
            {
                Console.WriteLine("ERROR: CONFIG FILE EMPTY");
                Console.WriteLine("Populating new file...");
                using (StreamWriter sw = new StreamWriter(dir + "\\config.cfg"))
                {
                    sw.WriteLine("Token=");
                    sw.WriteLine("LastChannel=");
                    sw.WriteLine("RunOnStartup=" + runOnStartup.ToString());
                    sw.Close();
                }
                Console.WriteLine("config.cfg created. Please close this program, enter your token in the config and start it again.");
                Console.ReadLine();
                Environment.Exit(0);
            }

            for (int i = 0; i < configParts.Length; i++)
            {
                if (configParts[i] != "")
                {
                    string[] configOptionParts = configParts[i].Split('=');
                    if (configOptionParts[0] == "Token")
                    {
                        cfgToken = configOptionParts[1];
                        Console.WriteLine("Connecting with " + cfgToken);
                        if (cfgToken == "")
                        {
                            Console.WriteLine("Please enter your token and restart the program!");
                            Console.ReadLine();
                            Environment.Exit(0);
                        }
                    } else if (configOptionParts[0] == "LastChannel")
                    {
                        if (configOptionParts[1] != "")
                        {
                            Console.WriteLine("Previous channel: " + configOptionParts[1]);
                            channelToSelect = ulong.Parse(configOptionParts[1]);
                        }
                    } else if (configOptionParts[0] == "RunOnStartup")
                    {
                        if (configOptionParts[1].ToLower() == "false")
                        {
                            runOnStartup = false;
                            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                            if (key.GetValue("DiscordDMLogitechLCD") != null)
                            {
                                key.DeleteValue("DiscordDMLogitechLCD", false);
                            }
                        } else if (configOptionParts[1].ToLower() == "true")
                        {
                            runOnStartup = true;
                            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                            if (key.GetValue("DiscordDMLogitechLCD") == null || key.GetValue("DiscordDMLogitechLCD").ToString() != dir + "\\DiscordDMLogitechLCD.exe")
                            {
                                key.SetValue("DiscordDMLogitechLCD", dir + "\\DiscordDMLogitechLCD.exe");
                            }
                        }
                    }
                }
            }

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
                using (StreamWriter sw = new StreamWriter(dir + "\\config.cfg"))
                {
                    sw.WriteLine("Token=" + cfgToken);
                    sw.WriteLine("LastChannel=" + selectedChannel.Id);
                    sw.WriteLine("RunOnStartup=" + runOnStartup.ToString());
                    sw.Close();
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

    public static void notification(string message, int line)
    {
        for (int i = 0; i < notificationAmount; i++)
        {
            LCDWrapper.LogiLcdMonoSetText(line, message);
            LCDWrapper.LogiLcdUpdate();
            Thread.Sleep(500);
            if (line == 0)
            {
                LCDWrapper.LogiLcdMonoSetText(line, line0);
            }
            else if (line == 1)
            {
                LCDWrapper.LogiLcdMonoSetText(line, line1);
            }
            else if (line == 2)
            {
                LCDWrapper.LogiLcdMonoSetText(line, line2);
            }
            else if (line == 3)
            {
                LCDWrapper.LogiLcdMonoSetText(line, line3);
            }
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
                if (channelToSelect != 0)
                {
                    if (channel.Id == channelToSelect)
                    {
                        selectedChannel = channel;
                    }
                }
            }
            if (channelToSelect == 0)
            {
                selectedChannel = dmChannels[0];
            }
            DSharpPlus.Entities.DiscordUser[] tempUsers = selectedChannel.Recipients.ToArray();
            if (tempUsers.Length == 1)
            {
                LCDWrapper.LogiLcdMonoSetText(0, "Channel: " + tempUsers[0].Username);
            }
            else if (tempUsers.Length > 1)
            {
                LCDWrapper.LogiLcdMonoSetText(0, "Channel: " + tempUsers[0].Username + " + " + tempUsers.Length + " More");
            }
            LCDWrapper.LogiLcdUpdate();
            Console.WriteLine("Selected channel: " + selectedChannel.Recipients[0].Username);

            getPrevMessages();
        };

        discord.TypingStarted += async t =>
        {
            if (t.Channel == selectedChannel)
            {
                Console.WriteLine("User typing!");

                Thread typingIndicatorThread = new Thread(() => notification(t.User.Username.ToUpper() + " is typing...", 3));
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
                } else if (e.Channel != selectedChannel)
                {
                    for (int i = 0; i < dmChannels.Count(); i++)
                    {
                        if (e.Channel == dmChannels[i])
                        {
                            Thread newUnselectedChannelMessageThread = new Thread(() => notification("Message from " + e.Author.Username.ToUpper(), 0));
                            newUnselectedChannelMessageThread.Start();
                        }
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
